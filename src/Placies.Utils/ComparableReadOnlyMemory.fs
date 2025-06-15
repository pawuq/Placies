namespace Placies.Utils

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<Struct>]
[<CustomEquality; CustomComparison>]
type ComparableReadOnlyMemory<'T when 'T :> IComparable<'T>> = {
    Value: ReadOnlyMemory<'T>
} with
    static member Create(value: ReadOnlyMemory<'T>) =
        { Value = value }
    interface IComparable<ComparableReadOnlyMemory<'T>> with
        member this.CompareTo(other) =
            MemoryExtensions.SequenceCompareTo(this.Value.Span, other.Value.Span)
    interface IComparable with
        member this.CompareTo(other) = (this :> IComparable<_>).CompareTo(other :?> ComparableReadOnlyMemory<'T>)
    override this.Equals(obj: obj): bool =
        match obj with
        | :? ComparableReadOnlyMemory<'T> as obj ->
            MemoryExtensions.SequenceEqual(this.Value.Span, obj.Value.Span)
        | _ -> false
    override this.GetHashCode(): int =
        let hashCode = HashCode()
        let span = this.Value.Span
        if typeof<'T> = typeof<byte> then
            let spanByte = ReadOnlySpan<byte>(Unsafe.AsPointer(&Unsafe.As<'T, byte>(&MemoryMarshal.GetReference(span))), span.Length)
            hashCode.AddBytes(spanByte)
        else
            for i = 0 to span.Length - 1 do
                hashCode.Add(span.[i])
        hashCode.ToHashCode()
