namespace Placies.Ipld.Car.CarV1

open System
open System.Buffers
open System.Collections.Generic
open System.IO
open System.IO.Pipelines
open System.Threading
open FSharp.Control
open FsToolkit.ErrorHandling
open Placies
open Placies.Ipld
open Placies.Multiformats
open Placies.Utils
open Placies.Utils.Parsing


type CarV1Header = {
    Version: int
    Roots: IReadOnlyList<Cid>
}

type CarDataSection = {
    Cid: Cid
    BlockBytes: ReadOnlyMemory<byte>

    Offset: int
    Length: int
    BlockOffset: int
} with
    member this.BlockLength = this.BlockBytes.Length

type CarV1 = {
    Header: CarV1Header
    DataSections: IReadOnlyList<CarDataSection>
}

[<RequireQualifiedAccess>]
module CarV1 =

    let parseHeader (dataModelNode: DataModelNode) : Validation<CarV1Header, _> = validation {
        let! version = validation {
            let! version = dataModelNode |> DataModelNode.tryAsMapAndFindField (DataModelNode.String "version") |> Result.requireSome "No 'version' field"
            let! version = version |> DataModelNode.tryAsInteger |> Result.requireSome "'version' field is not Integer"
            return int32<int64> version
        }
        do! version |> Result.requireEqualTo 1 "Version must be 1"
        let! roots = validation {
            let! roots = dataModelNode |> DataModelNode.tryAsMapAndFindField (DataModelNode.String "roots") |> Result.requireSome "No 'roots' field"
            let! roots = roots |> DataModelNode.tryAsList |> Result.requireSome "'roots' field is not List"
            let! roots =
                roots
                |> List.map (DataModelNode.tryAsLink >> Result.requireSome "root is not Link" >> Validation.ofResult)
                |> List.sequenceValidationA
            return roots
        }
        do!
            let rootsLength = roots |> List.length
            (rootsLength >= 1) |> Result.requireTrueWith (fun () -> $"'roots' length must be >= 1, but it is %i{rootsLength}")
        return {
            Version = version
            Roots = roots
        }
    }

    let rec private readVarInt (pipeReader: PipeReader) ct : TaskResult<uint64, exn> = task {
        let! readResult = pipeReader.ReadAsync(ct) // TODO?: Respect readResult.IsCanceled
        let tempBufferLength = min VarInt.maxSize (int readResult.Buffer.Length)
        let tempBuffer = Unsafe.stackallockSpan<byte> tempBufferLength
        readResult.Buffer.Slice(0, tempBufferLength).CopyTo(tempBuffer)
        match VarInt.parseSizeOfSpan (tempBuffer.AsReadOnly()) with
        | Error ParseError.Incomplete ->
            if readResult.IsCompleted then
                return Error ^ exn("Unexpected end of data")
            else
                pipeReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End)
                return! readVarInt pipeReader ct
        | Error (ParseError.Error err) ->
            return Error err
        | Ok varintSize ->
            pipeReader.AdvanceTo(readResult.Buffer.GetPosition(varintSize))
            let res, _ = VarIntParser.TryParseFromSpanAsUInt64(tempBuffer)
            return res
    }

    let readHeader (pipeReader: PipeReader) (dagCborIpldCodec: IIpldCodec) ct : TaskResult<CarV1Header * int, _> = taskResult {
        do! dagCborIpldCodec.CodecInfo.Code |> Result.requireEqualTo MultiCodecInfos.DagCbor.Code (exn $"DagCbor ipld codec must be %A{MultiCodecInfos.DagCbor}, but it is %A{dagCborIpldCodec.CodecInfo}")
        let! headerSectionLength = readVarInt pipeReader ct |> TaskResult.mapError (fun err -> exn("Failed parse header varint", err))
        let headerSectionLength = int headerSectionLength
        let! readResult = pipeReader.ReadAtLeastAsync(headerSectionLength, ct) // TODO?: Respect readResult.IsCanceled
        if readResult.Buffer.Length < headerSectionLength then
            return! Error ^ exn "Unexpected end of data"
        else
            let headerBytes = readResult.Buffer.Slice(0, headerSectionLength)
            let! headerDmn = dagCborIpldCodec.TryDecode(headerBytes) |> Result.mapError (fun err -> exn("Failed parse header data model", err))
            pipeReader.AdvanceTo(headerBytes.End)
            let! header = parseHeader headerDmn |> Result.mapError (fun errs -> exn($"Failed parse header:\n%A{errs}"))
            let headerLength = VarInt.getSizeOfInt32 headerSectionLength + headerSectionLength
            return header, headerLength
    }

    let readDataSection (pipeReader: PipeReader) (offset: int) ct : TaskResult<CarDataSection, exn> = task {
        match! readVarInt pipeReader ct with
        | Error err ->
            return Error ^ exn("Failed parse header varint", err)
        | Ok dataSectionLength ->
            let dataSectionLength = int dataSectionLength
            let! readResult = pipeReader.ReadAtLeastAsync(dataSectionLength, ct) // TODO?: Respect readResult.IsCanceled
            if readResult.Buffer.Length < dataSectionLength then
                return Error ^ exn "Unexpected end of data"
            else
                let mutable sequenceReader = SequenceReader(readResult.Buffer)
                match CidParser.TryParseFromSequenceReader(&sequenceReader) with
                | Error err -> return Error ^ exn("Failed parse CID", err)
                | Ok cid ->
                    let blockBytesLength = dataSectionLength - int sequenceReader.Consumed
                    let blockBytes = Array.zeroCreate<byte> blockBytesLength
                    sequenceReader.TryCopyTo(blockBytes.AsSpan()) |> fun success -> assert success
                    sequenceReader.Advance(blockBytesLength)
                    pipeReader.AdvanceTo(readResult.Buffer.GetPosition(dataSectionLength))
                    let dataSectionLength = VarInt.getSizeOfInt32 dataSectionLength + dataSectionLength
                    let dataSection = {
                        Cid = cid
                        BlockBytes = blockBytes.AsMemory().AsReadOnly()
                        Offset = offset
                        Length = dataSectionLength
                        BlockOffset = offset + (dataSectionLength - blockBytesLength)
                    }
                    return Ok dataSection
    }

    let readDataSections (pipeReader: PipeReader) (headerLength: int) ct : IAsyncEnumerable<Result<CarDataSection, exn>> = taskSeq {
        let mutable doLoop = true
        let mutable offset = headerLength
        while doLoop do
            let! readResult = pipeReader.ReadAsync(ct)
            let isEof = readResult.Buffer.IsEmpty && readResult.IsCompleted
            pipeReader.AdvanceTo(readResult.Buffer.Start)
            if isEof then
                doLoop <- false
            else
                match! readDataSection pipeReader offset ct with
                | Ok dataSection ->
                    offset <- offset + dataSection.Length
                    yield Ok dataSection
                | Error err ->
                    yield Error err
                    doLoop <- false
    }

    let readFromPipeReader (pipeReader: PipeReader) (dagCborIpldCodec: IIpldCodec) (ct: CancellationToken) : TaskResult<CarV1, exn> = taskResult {
        let! header, headerLength = readHeader pipeReader dagCborIpldCodec ct
        let dataSections = readDataSections pipeReader headerLength ct
        let! dataSections = dataSections |> TaskSeq.toListAsync
        let! dataSections = dataSections |> List.sequenceResultM
        return {
            Header = header
            DataSections = dataSections |> List.toArray
        }
    }
    let readFromStream (stream: Stream) (dagCborIpldCodec: IIpldCodec) (ct: CancellationToken) : TaskResult<CarV1, exn> = task {
        let pipeReader = PipeReader.Create(stream)
        let! res = readFromPipeReader pipeReader dagCborIpldCodec ct
        do! pipeReader.CompleteAsync()
        return res
    }

    let toDataModel (ipldCodecProvider: IIpldCodecProvider) (car: CarV1) : TaskResult<DataModelNode, exn> = taskResult {
        let! blocks =
            car.DataSections
            |> Seq.map ^fun dataSection -> taskResult {
                let cid = dataSection.Cid
                let! ipldCodec = ipldCodecProvider.TryGetByCid(cid) |> Result.requireSome (exn $"Not found IPLD codec for cid %A{cid}")
                let! content = ipldCodec.TryDecode(ReadOnlySequence(dataSection.BlockBytes))
                return dataModelMap {
                    "cid", cid
                    "content", content
                    "offset", dataSection.Offset
                    "length", dataSection.Length
                    "blockOffset", dataSection.BlockOffset
                    "blockLength", dataSection.BlockLength
                }
            }
            |> Seq.toList
            |> List.sequenceTaskResultM
        return dataModelMap {
            "header", dataModelMap {
                "version", car.Header.Version
                "roots", dataModelList {
                    for cid in car.Header.Roots -> cid
                }
            }
            "blocks", DataModelNode.List blocks
        }
    }
