namespace Placies.Ipld

open Placies.Ipld.DagCbor
open Placies.Ipld.DagJson
open Placies.Ipld.DagPb
open Placies.Multiformats

[<AutoOpen>]
module DefaultIpldCodecRegistryExtensions =
    type IpldCodecRegistry with

        static member DefaultIpldCodecs(multiBaseProvider: IMultiBaseProvider): IIpldCodec seq = seq {
            RawIpldCodec()
            DagPbIpldCodec()
            DagCborIpldCodec()
            DagJsonIpldCodec(multiBaseProvider)
        }

        member this.RegisterDefaults(multiBaseProvider: IMultiBaseProvider): unit =
            for codec in IpldCodecRegistry.DefaultIpldCodecs(multiBaseProvider) do
                this.Register(codec) |> ignore

        static member CreateDefault(multiBaseProvider: IMultiBaseProvider): IpldCodecRegistry =
            let registry = IpldCodecRegistry()
            registry.RegisterDefaults(multiBaseProvider)
            registry

[<AutoOpen>]
module SharedIpldCodecRegistryExtensions =
    let private sharedInstance = IpldCodecRegistry.CreateDefault(MultiBaseRegistry.Shared)
    type IpldCodecRegistry with
        static member Shared = sharedInstance
