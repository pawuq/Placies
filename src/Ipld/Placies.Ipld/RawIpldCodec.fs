namespace Placies.Ipld

open System
open System.Buffers
open Placies.Multiformats

type RawIpldCodec() =
    interface IIpldCodec with
        member this.CodecInfo = MultiCodecInfos.Raw
        member _.TryDecode(buffer) =
            let bytes = Array.zeroCreate<byte> (int buffer.Length)
            buffer.CopyTo(bytes)
            DataModelNode.Bytes bytes |> Ok

        member _.TryEncode(bufferWriter, dataModelNode) =
            match dataModelNode with
            | DataModelNode.Bytes bytes ->
                bufferWriter.Write(bytes)
                Ok ()
            | _ ->
                Error (exn ("Invalid raw data model", exn "Data model is not Bytes"))
