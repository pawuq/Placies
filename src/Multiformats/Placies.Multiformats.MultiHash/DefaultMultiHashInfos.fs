namespace Placies.Multiformats

open System
open System.Security.Cryptography


type IdentityHashAlgorithm() =
    inherit HashAlgorithm()
    let mutable memory: Memory<byte> = Unchecked.defaultof<_>
    override this.HashCore(array, ibStart, cbSize) =
        memory <- array.AsMemory(ibStart, cbSize)
    override this.HashFinal() =
        memory.ToArray()
    override this.Initialize() =
        ()


[<RequireQualifiedAccess>]
module MultiHashInfos =

    let Identity = { CodecInfo = MultiCodecInfos.Identity; HashAlgorithm = fun () -> new IdentityHashAlgorithm() }

    let Sha2_256 = { CodecInfo = MultiCodecInfos.Sha2_256; HashAlgorithm = fun () -> SHA256.Create() }


[<AutoOpen>]
module DefaultMultiHashRegistryExtensions =
    type MultiHashRegistry with

        static member DefaultMultiHashInfos = seq {
            MultiHashInfos.Identity
            MultiHashInfos.Sha2_256
        }

        member this.RegisterDefaults(): unit =
            for multiHashInfo in MultiHashRegistry.DefaultMultiHashInfos do
                this.Register(multiHashInfo) |> ignore

        static member CreateDefault(): MultiHashRegistry =
            let registry = MultiHashRegistry()
            registry.RegisterDefaults()
            registry

[<AutoOpen>]
module SharedMultiHashRegistryExtensions =
    let private sharedInstance = MultiHashRegistry.CreateDefault()
    type MultiHashRegistry with
        static member Shared = sharedInstance
