namespace Placies.Multiformats

open System
open System.Text
open System.Numerics
open System.Collections.Generic
open Placies.Utils


[<RequireQualifiedAccess>]
module MultiBaseInfos =

    let encodeAnyBase (baseAlphabet: string) (input: ReadOnlySpan<byte>) : string =
        let mutable number = BigInteger(input)
        let l = baseAlphabet.Length
        let result = StringBuilder()
        while number > BigInteger.Zero do
            let idx = number % BigInteger(l) |> int
            if idx >= l then failwith ""
            result.Append(baseAlphabet.[idx]) |> ignore
            number <- number / BigInteger(l)
        result.ToString()

    let decodeAnyBase (baseAlphabet: string) (input: ReadOnlySpan<char>) : byte array =
        let mutable result = BigInteger(0)
        let b = baseAlphabet.Length
        let mutable pow = 0
        for i in input.Length-1 .. -1 .. 0 do
            let c = input.[i]
            let idx = baseAlphabet.IndexOf(c)
            if idx = -1 then failwith ""
            result <- result + BigInteger.Pow(b, pow) * BigInteger(idx)
            pow <- pow + 1
        result.ToByteArray(isUnsigned=true, isBigEndian=true)

    let base10Alphabet = "0123456789"
    let base36Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz"


    let Base10 = {
        Name = "base10"; PrefixCharacter = '9'
        BaseCoder = BaseEncoder.create (fun bytes -> encodeAnyBase base10Alphabet bytes.Span) (fun text -> decodeAnyBase base10Alphabet text.Span)
    }
    let Base16 = {
        Name = "base16"; PrefixCharacter = 'f'
        BaseCoder = BaseEncoder.create (fun bytes -> SimpleBase.Base16.LowerCase.Encode(bytes.Span)) (fun text -> SimpleBase.Base16.LowerCase.Decode(text.Span))
    }
    let Base32 = {
        Name = "base32"; PrefixCharacter = 'b'
        BaseCoder = BaseEncoder.create (fun bytes -> SimpleBase.Base32.Rfc4648.Encode(bytes.Span, false).ToLowerInvariant()) (fun text -> SimpleBase.Base32.Rfc4648.Decode(text.Span))
    }
    let Base36 = {
        Name = "base36"; PrefixCharacter = 'k'
        BaseCoder = BaseEncoder.create (fun bytes -> encodeAnyBase base36Alphabet bytes.Span) (fun text -> decodeAnyBase base36Alphabet text.Span)
    }
    let Base58Btc = {
        Name = "base58btc"; PrefixCharacter = 'z'
        BaseCoder = BaseEncoder.create (fun bytes -> SimpleBase.Base58.Bitcoin.Encode(bytes.Span)) (fun text -> SimpleBase.Base58.Bitcoin.Decode(text.Span))
    }
    let Base64 = {
        Name = "base64"; PrefixCharacter = 'm'
        BaseCoder = BaseEncoder.create (fun bytes -> Convert.ToBase64StringNoPad(bytes.Span)) (fun text -> Convert.FromBase64StringNoPad(text.Span))
    }
    let Base64Pad = {
        Name = "base64pad"; PrefixCharacter = 'M'
        BaseCoder = BaseEncoder.create (fun bytes -> Convert.ToBase64String(bytes.Span)) (fun text -> Convert.FromBase64Chars(text.Span))
    }
    let Base64Url = {
        Name = "base64url"; PrefixCharacter = 'u'
        BaseCoder = BaseEncoder.create (fun bytes -> Convert.ToBase64StringUrl(bytes.Span)) (fun text -> Convert.FromBase64StringUrl(text.Span))
    }


[<AutoOpen>]
module DefaultMultiBaseRegistryExtensions =
    type MultiBaseRegistry with

        static member DefaultMultiBaseInfos = seq {
            MultiBaseInfos.Base10
            MultiBaseInfos.Base16
            MultiBaseInfos.Base32
            MultiBaseInfos.Base36
            MultiBaseInfos.Base58Btc
            MultiBaseInfos.Base64
            MultiBaseInfos.Base64Url
        }

        member this.RegisterDefaults(): unit =
            for multiBaseInfo in MultiBaseRegistry.DefaultMultiBaseInfos do
                this.Register(multiBaseInfo) |> ignore

        static member CreateDefault(): MultiBaseRegistry =
            let registry = MultiBaseRegistry()
            registry.RegisterDefaults()
            registry

[<AutoOpen>]
module SharedMultiBaseRegistryExtensions =
    let private sharedInstance = MultiBaseRegistry.CreateDefault()
    type MultiBaseRegistry with
        static member Shared = sharedInstance
