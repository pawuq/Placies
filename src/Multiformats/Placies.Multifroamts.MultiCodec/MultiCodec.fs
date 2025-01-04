namespace Placies.Multiformats

open System.Collections.Generic
open Placies.Utils
open Placies.Utils.Collections


type MultiCodecInfo = {
    Name: string
    Code: int
}

type IMultiCodecProvider =
    abstract TryGetByCode: code: int -> MultiCodecInfo option
    abstract TryGetByName: name: string -> MultiCodecInfo option

type MultiCodecRegistry() =
    let registryOfName = Dictionary<string, MultiCodecInfo>()
    let registryOfCode = Dictionary<int, MultiCodecInfo>()

    member _.Register(codecInfo: MultiCodecInfo): bool =
        Dictionary.tryAdd2
            registryOfCode codecInfo.Code codecInfo
            registryOfName codecInfo.Name codecInfo

    interface IMultiCodecProvider with
        member _.TryGetByCode(code: int): MultiCodecInfo option =
            registryOfCode.TryGetValue(code) |> Option.ofTryByref
        member _.TryGetByName(name: string): MultiCodecInfo option =
            registryOfName.TryGetValue(name) |> Option.ofTryByref
