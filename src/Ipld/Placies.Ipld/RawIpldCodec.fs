namespace Placies.Ipld

open System.IO
open FsToolkit.ErrorHandling
open Placies.Multiformats

type RawIpldCodec() =
    interface IIpldCodec with
        member this.CodecInfo = MultiCodecInfos.Raw
        member this.TryDecodeAsync(stream) = taskResult {
            use memoryStream = new MemoryStream()
            do! stream.CopyToAsync(memoryStream)
            let bytes = memoryStream.ToArray()
            return DataModelNode.Bytes bytes
        }
        member this.TryEncodeAsync(writeToStream, dataModelNode) = taskResult {
            match dataModelNode with
            | DataModelNode.Bytes bytes ->
                do! writeToStream.WriteAsync(bytes)
            | _ ->
                return! Error (exn ("Invalid raw data model", exn "Not Bytes"))
        }
