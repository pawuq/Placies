namespace Placies.Ipld

open System.Collections.Generic
open Placies
open Placies.Utils
open Placies.Utils.Collections
open Placies.Multiformats

type IIpldCodecProvider =
    abstract TryGetByCode: code: int -> IIpldCodec option
    abstract TryGetByName: name: string -> IIpldCodec option

[<AutoOpen>]
module IpldCodecProviderExtensions =
    type IIpldCodecProvider with
        member this.TryGetByMultiCodecInfo(multiCodecInfo: MultiCodecInfo): IIpldCodec option =
            this.TryGetByCode(multiCodecInfo.Code)
        member this.TryGetByCid(cid: Cid): IIpldCodec option =
            this.TryGetByCode(cid.ContentTypeCode)


type IpldCodecRegistry() =
    let registryByCode = Dictionary<int, IIpldCodec>()
    let registryByName = Dictionary<string, IIpldCodec>()

    member _.Register(codec: IIpldCodec): bool =
        Dictionary.tryAdd2
            registryByCode codec.CodecInfo.Code codec
            registryByName codec.CodecInfo.Name codec

    interface IIpldCodecProvider with
        member this.TryGetByCode(code) =
            registryByCode.TryGetValue(code) |> Option.ofTryByref
        member this.TryGetByName(name) =
            registryByName.TryGetValue(name) |> Option.ofTryByref
