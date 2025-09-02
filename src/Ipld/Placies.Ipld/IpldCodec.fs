namespace Placies.Ipld

open System
open System.Buffers
open System.IO
open System.IO.Pipelines
open System.Runtime.InteropServices
open System.Threading
open FsToolkit.ErrorHandling
open Placies
open Placies.Utils
open Placies.Multiformats

type IIpldCodec =
    abstract CodecInfo: MultiCodecInfo
    abstract TryEncodeAsync: pipeWriter: PipeWriter * dataModelNode: DataModelNode * [<Optional>] ct: CancellationToken -> TaskResult<unit, exn>
    abstract TryDecodeAsync: pipeReader: PipeReader * [<Optional>] ct: CancellationToken -> TaskResult<DataModelNode, exn>

[<AutoOpen>]
module CodecExtensions =

    type IIpldCodec with

        member this.TryEncodeAsync(writeToStream: Stream, dataModelNode: DataModelNode, [<Optional>] ct: CancellationToken): TaskResult<unit, exn> = task {
            return! PipeWriter.usingAsync (PipeWriter.Create(writeToStream)) ^fun pipeWriter ->
                this.TryEncodeAsync(pipeWriter, dataModelNode, ct)
        }
        member this.TryDecodeAsync(stream: Stream, [<Optional>] ct: CancellationToken): TaskResult<DataModelNode, exn> = task {
            return! PipeReader.usingAsync (PipeReader.Create(stream)) ^fun pipeReader ->
                this.TryDecodeAsync(pipeReader, ct)
        }

        member this.TryEncodeWithCidAsync(pipeWriter: PipeWriter, dataModelNode: DataModelNode, cidVersion: int, cidMultihashInfo: MultiHashInfo, [<Optional>] ct: CancellationToken): TaskResult<Cid, exn> = taskResult {
            use memoryStream = new MemoryStream()

            let memoryStreamPipeWriter = PipeWriter.Create(memoryStream, StreamPipeWriterOptions(leaveOpen=true))
            do! this.TryEncodeAsync(memoryStreamPipeWriter, dataModelNode, ct)
            do! memoryStreamPipeWriter.CompleteAsync()
            memoryStream.Seek(0, SeekOrigin.Begin) |> ignore
            memoryStream.CopyTo(pipeWriter.AsStream())

            memoryStream.Seek(0, SeekOrigin.Begin) |> ignore
            let multihash = MultiHash.computeFromStream memoryStream cidMultihashInfo
            let cid = Cid.create cidVersion this.CodecInfo.Code multihash
            return cid
        }
        member this.TryEncodeWithCidAsync(writeToStream: Stream, dataModelNode: DataModelNode, cidVersion: int, cidMultihashInfo: MultiHashInfo, [<Optional>] ct: CancellationToken): TaskResult<Cid, exn> = task {
            return! PipeWriter.usingAsync (PipeWriter.Create(writeToStream)) ^fun pipeWriter ->
                this.TryEncodeWithCidAsync(pipeWriter, dataModelNode, cidVersion, cidMultihashInfo, ct)
        }

        member this.TryDecodeAsync(buffer: ReadOnlyMemory<byte>, [<Optional>] ct: CancellationToken): TaskResult<DataModelNode, exn> = task {
            return! PipeReader.usingAsync (PipeReader.Create(ReadOnlySequence(buffer))) ^fun pipeReader ->
                this.TryDecodeAsync(pipeReader, ct)
        }
