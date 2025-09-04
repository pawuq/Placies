module Placies.Ipld.Tests.CarV1Tests

open System.IO
open System.Threading
open System.Threading.Tasks
open Xunit
open Swensen.Unquote

open Placies.Utils
open Placies.Multiformats
open Placies.Ipld
open Placies.Ipld.DagJson
open Placies.Ipld.DagCbor
open Placies.Ipld.Car.CarV1


[<Fact>]
let ``carv1-basic`` () : Task =
    let multiBaseRegistry = MultiBaseRegistry.CreateDefault()
    let ipldCodecProvider = IpldCodecRegistry.CreateDefault(multiBaseRegistry)
    let dagCborIpldCodec = DagCborIpldCodec() :> IIpldCodec
    let dagJsonIpldCodec = DagJsonIpldCodec(multiBaseRegistry) :> IIpldCodec
    task {
        use carStream = File.OpenRead("./car-fixtures/carv1-basic.car")
        let! car = CarV1.readFromStream carStream dagCborIpldCodec CancellationToken.None
        let car = car |> ResultExn.getOk
        let carDataModel = car |> CarV1.toDataModel ipldCodecProvider |> Task.runSynchronously |> Result.getOk

        let jsonStream = File.OpenRead("./car-fixtures/carv1-basic.json")
        let! json = dagJsonIpldCodec.TryDecodeAsync(jsonStream)
        let jsonDataModel = json |> ResultExn.getOk

        test <@ carDataModel = jsonDataModel @>
    }
