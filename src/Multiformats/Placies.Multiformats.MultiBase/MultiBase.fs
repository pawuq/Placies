namespace Placies.Multiformats

open System
open System.Collections.Generic
open FsToolkit.ErrorHandling
open Placies.Utils
open Placies.Utils.Collections


type IBaseCoder =
    abstract Encode: bytes: ReadOnlyMemory<byte> -> string
    abstract Decode: str: ReadOnlyMemory<char> -> byte array

[<RequireQualifiedAccess>]
module BaseEncoder =
    let create (encode: ReadOnlyMemory<byte> -> string) (decode: ReadOnlyMemory<char> -> byte array) : IBaseCoder =
        { new IBaseCoder with
            member _.Encode(bytes) = encode bytes
            member _.Decode(str) = decode str
        }

type MultiBaseInfo = {
    Name: string
    PrefixCharacter: char
    BaseCoder: IBaseCoder
}

type IMultiBaseProvider =
    abstract TryGetByName: name: string -> MultiBaseInfo option
    abstract TryGetByPrefix: prefix: char -> MultiBaseInfo option

[<RequireQualifiedAccess>]
module MultiBase =

    let encode (multibaseInfo: MultiBaseInfo) (bytes: ReadOnlyMemory<byte>) : string =
        string multibaseInfo.PrefixCharacter + multibaseInfo.BaseCoder.Encode(bytes)

    let tryDecode (provider: IMultiBaseProvider) (multibaseText: ReadOnlyMemory<char>) : Result<byte array, string> = result {
        do! multibaseText.IsEmpty |> Result.requireFalse "No multibase prefix"
        let prefix = multibaseText.Span.[0]
        let text = multibaseText.Slice(1)
        let! multibaseInfo = provider.TryGetByPrefix(prefix) |> Result.requireSome $"Not found multibase for prefix '%c{prefix}'"
        return multibaseInfo.BaseCoder.Decode(text)
    }

    let decode provider multibaseText =
        tryDecode provider multibaseText |> Result.getOk

type MultiBaseRegistry() =
    let registryByPrefix = Dictionary<char, MultiBaseInfo>()
    let registryByName = Dictionary<string, MultiBaseInfo>()

    member _.Register(info: MultiBaseInfo): bool =
        Dictionary.tryAdd2
            registryByName info.Name info
            registryByPrefix info.PrefixCharacter info

    interface IMultiBaseProvider with
        member this.TryGetByName(name) =
            registryByName.TryGetValue(name) |> Option.ofTryByref
        member this.TryGetByPrefix(prefix) =
            registryByPrefix.TryGetValue(prefix) |> Option.ofTryByref
