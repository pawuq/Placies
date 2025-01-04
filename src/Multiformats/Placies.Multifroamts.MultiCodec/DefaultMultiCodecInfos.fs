namespace Placies.Multiformats


[<RequireQualifiedAccess>]
module MultiCodecInfos =

    let Identity = { Name = "identity"; Code = 0x0 }
    let Cidv1 = { Name = "cidv1"; Code = 0x1 }

    let Sha2_256 = { Name = "sha2-256"; Code = 0x12 }

    let Varsig = { Name = "varsig"; Code = 0x34 }
    let Dns = { Name = "dns"; Code = 0x35 }

    let Raw = { Name = "raw"; Code = 0x55 }

    let DagPb = { Name = "dag-pb"; Code = 0x70 }
    let DagCbor = { Name = "dag-cbor"; Code = 0x71 }
    let Libp2pKey = { Name = "libp2p-key"; Code = 0x72 }

    let Ipfs = { Name = "ipfs"; Code = 0xe3 }
    let Ipns = { Name = "ipns"; Code = 0xe5 }

    let Ed25519Pub = { Name = "ed25519-pub"; Code = 0xed }

    let DagJson = { Name = "dag-json"; Code = 0x0129 }

    let RsaPub = { Name = "rsa-pub"; Code = 0x1205 }


[<AutoOpen>]
module DefaultMultiCodecRegistryExtensions =
    type MultiCodecRegistry with

        static member DefaultMultiCodecInfos = seq {
            MultiCodecInfos.Identity
            MultiCodecInfos.Cidv1
            MultiCodecInfos.Sha2_256
            MultiCodecInfos.Varsig
            MultiCodecInfos.Dns
            MultiCodecInfos.Raw
            MultiCodecInfos.DagPb
            MultiCodecInfos.DagCbor
            MultiCodecInfos.Libp2pKey
            MultiCodecInfos.Ipfs
            MultiCodecInfos.Ipns
            MultiCodecInfos.Ed25519Pub
            MultiCodecInfos.DagJson
            MultiCodecInfos.RsaPub
        }

        member this.RegisterDefaults(): unit =
            for multiCodecInfo in MultiCodecRegistry.DefaultMultiCodecInfos do
                this.Register(multiCodecInfo) |> ignore

        static member CreateDefault(): MultiCodecRegistry =
            let registry = MultiCodecRegistry()
            registry.RegisterDefaults()
            registry
