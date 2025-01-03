namespace Placies.Ipld

open System
open System.IO
open FsToolkit.ErrorHandling
open Placies.Multiformats

type RawIpldCodec() =
    interface IIpldCodec with
        member this.CodecInfo = MultiCodecInfos.Raw
        member this.TryDecodeAsync(pipeReader) = taskResult {
            use memoryStream = new MemoryStream()
            do! pipeReader.CopyToAsync(memoryStream)
            let bytes = memoryStream.ToArray()
            return DataModelNode.Bytes bytes
        }
        member this.TryEncodeAsync(pipeWriter, dataModelNode) = taskResult {
            match dataModelNode with
            | DataModelNode.Bytes bytes ->
                let! flushResult = pipeWriter.WriteAsync(bytes)
                if flushResult.IsCanceled then
                    raise (OperationCanceledException())
                // TODO?: Handle flushResult.IsCompleted
            | _ ->
                return! Error (exn ("Invalid raw data model", exn "Data model is not Bytes"))
        }
