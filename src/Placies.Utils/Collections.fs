namespace Placies.Utils.Collections

open System
open System.Collections.Generic


[<RequireQualifiedAccess>]
module ArraySegment =

    // [<return: Struct>]
    let (|Nil|Cons|) (source: ArraySegment<'a>) =
        if source.Count = 0 then
            Nil
        else
            Cons (source.[0], source.Slice(1))


[<RequireQualifiedAccess>]
module Array =

    let tryExactlyTwo (source: 'a array) : ('a * 'a) option =
        if source.Length = 2 then
            Some (source.[0], source.[1])
        else
            None

    let exactlyTwo (source: 'a array) : 'a * 'a =
        tryExactlyTwo source |> Option.get


[<RequireQualifiedAccess>]
module Dictionary =

    let tryAdd2
            (source1: IDictionary<'K1, 'V1>) (k1: 'K1) (v1: 'V1)
            (source2: IDictionary<'K2, 'V2>) (k2: 'K2) (v2: 'V2)
            : bool =
        if source1.TryAdd(k1, v1) then
            if source2.TryAdd(k2, v2) then
                true
            else
                source1.Remove(k1) |> ignore
                false
        else
            false
