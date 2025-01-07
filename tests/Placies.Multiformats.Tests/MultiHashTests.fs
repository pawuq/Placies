module Placies.Multiformats.Tests.MultiHashTests

open System
open Xunit
open Swensen.Unquote
open Placies.Multiformats

[<Fact>]
let ``Minimum length hash`` () : unit =
    let actualBytes = Convert.FromHexString("0001aa").AsSpan()
    let actualRes, _ = MultiHashParser.TryParseFromSpan(actualBytes)
    let expectedMultiHash = MultiHash.create MultiCodecInfos.Identity.Code [| 0xAAuy |]
    test <@ actualRes = Ok expectedMultiHash @>
