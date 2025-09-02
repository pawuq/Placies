namespace Placies.Utils

#nowarn "9" // Uses of this construct may result in the generation of unverifiable .NET IL code.

open System
open Microsoft.FSharp.NativeInterop

[<RequireQualifiedAccess>]
module Unsafe =

    // https://github.com/fsharp/fslang-suggestions/issues/720
    let inline stackallockSpan<'a when 'a : unmanaged> (size: int) : Span<'a> =
        let p = NativePtr.stackalloc<'a> size |> NativePtr.toVoidPtr
        Span<'a>(p, size)
