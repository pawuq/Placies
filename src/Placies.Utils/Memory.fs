namespace Placies.Utils

open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module ReadOnlyMemoryExtensions =
    type Memory<'T> with
        member this.AsReadOnly(): ReadOnlyMemory<'T> =
            Memory.op_Implicit(this)

[<AutoOpen>]
module ReadOnlySpanExtensions =
    type Span<'T> with
        member this.AsReadOnly(): ReadOnlySpan<'T> =
            Span.op_Implicit(this)

type ArrayExtensions private () =
    [<Extension>]
    static member AsReadOnlyMemory(this: 'T array): ReadOnlyMemory<'T> =
        this.AsMemory().AsReadOnly()

[<RequireQualifiedAccess>]
module Array =
    let inline asMemory (array: 'a array) : Memory<'a> = array.AsMemory()
    let inline asReadOnlyMemory (array: 'a array) : ReadOnlyMemory<'a> = array.AsMemory().AsReadOnly()
