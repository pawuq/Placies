namespace Placies.Ipld

open System.IO
open System.IO.Pipelines
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Placies
open Placies.Utils
open Placies.Multiformats

type IIpldCodec =
    abstract CodecInfo: MultiCodecInfo
    abstract TryEncodeAsync: pipeWriter: PipeWriter * dataModelNode: DataModelNode -> TaskResult<unit, exn>
    abstract TryDecodeAsync: pipeReader: PipeReader -> TaskResult<DataModelNode, exn>

[<AutoOpen>]
module CodecExtensions =

    let private writeStreamToPipeWriter (writeToStream: Stream) (action: PipeWriter -> Task<'a>) = task {
        let pipeWriter = PipeWriter.Create(writeToStream)
        try
            let! a = action pipeWriter
            do! pipeWriter.CompleteAsync()
            return a
        with ex ->
            do! pipeWriter.CompleteAsync(ex)
            return raise ex
    }
    let private readPipeReaderFromStream (stream: Stream) (action: PipeReader -> Task<'a>) = task {
        let pipeReader = PipeReader.Create(stream)
        try
            let! a = action pipeReader
            do! pipeReader.CompleteAsync()
            return a
        with ex ->
            do! pipeReader.CompleteAsync(ex)
            return raise ex
    }

    type IIpldCodec with

        member this.TryEncodeAsync(writeToStream: Stream, dataModelNode: DataModelNode): TaskResult<unit, exn> = task {
            return! writeStreamToPipeWriter writeToStream ^fun pipeWriter ->
                this.TryEncodeAsync(pipeWriter, dataModelNode)
        }
        member this.TryDecodeAsync(stream: Stream): TaskResult<DataModelNode, exn> = task {
            return! readPipeReaderFromStream stream ^fun pipeReader ->
                this.TryDecodeAsync(pipeReader)
        }

        member this.TryEncodeWithCidAsync(pipeWriter: PipeWriter, dataModelNode: DataModelNode, cidVersion: int, cidMultihashInfo: MultiHashInfo): TaskResult<Cid, exn> = taskResult {
            use memoryStream = new MemoryStream()

            let memoryStreamPipeWriter = PipeWriter.Create(memoryStream, StreamPipeWriterOptions(leaveOpen=true))
            do! this.TryEncodeAsync(memoryStreamPipeWriter, dataModelNode)
            do! memoryStreamPipeWriter.CompleteAsync()
            memoryStream.Seek(0, SeekOrigin.Begin) |> ignore
            memoryStream.CopyTo(pipeWriter.AsStream())

            memoryStream.Seek(0, SeekOrigin.Begin) |> ignore
            let multihash = MultiHash.computeFromStream memoryStream cidMultihashInfo
            let cid = Cid.create cidVersion this.CodecInfo.Code multihash
            return cid
        }
        member this.TryEncodeWithCidAsync(writeToStream: Stream, dataModelNode: DataModelNode, cidVersion: int, cidMultihashInfo: MultiHashInfo): TaskResult<Cid, exn> = task {
            return! writeStreamToPipeWriter writeToStream ^fun pipeWriter ->
                this.TryEncodeWithCidAsync(pipeWriter, dataModelNode, cidVersion, cidMultihashInfo)
        }
