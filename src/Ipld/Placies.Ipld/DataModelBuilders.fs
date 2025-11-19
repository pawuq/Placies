namespace Placies.Ipld

open Placies


type DataModelListBuilder() =

    member inline _.Yield(elem: DataModelNode): DataModelNode list =
        [ elem ]

    member inline _.YieldFrom(elems: DataModelNode list): DataModelNode list =
        elems

    member inline _.Zero(): DataModelNode list =
        []

    member inline _.Delay(f: unit -> 'a): 'a =
        f ()

    member inline _.Combine(elems1: DataModelNode list, elems2: DataModelNode list): DataModelNode list =
        elems1 @ elems2

    member inline _.For(sequence: 'a seq, body: 'a -> DataModelNode list): DataModelNode list =
        sequence |> Seq.collect body |> Seq.toList

    member inline _.Run(elems: DataModelNode list): DataModelNode =
        DataModelNode.List elems

[<AutoOpen>]
module DataModeListBuilder =
    let dataModelList = DataModelListBuilder()

[<AutoOpen>]
module DataModelListBuilderExtensions =
    [<AutoOpen>]
    module LowPriority =
        type DataModelListBuilder with

            member inline this.Yield(value: bool): DataModelNode list =
                this.Yield(DataModelNode.Boolean value)

            member inline this.Yield(value: int): DataModelNode list =
                this.Yield(DataModelNode.Integer value)

            member inline this.Yield(value: float): DataModelNode list =
                this.Yield(DataModelNode.Float value)

            member inline this.Yield(value: string): DataModelNode list =
                this.Yield(DataModelNode.String value)

            member inline this.Yield(value: Cid): DataModelNode list =
                this.Yield(DataModelNode.Link value)

// ----

type DataModelMapBuilder() =

    member inline _.Yield(_: string * DataModelNode as (key, value)): (string * DataModelNode) list =
        [ key, value ]

    member inline _.YieldFrom(props: (string * DataModelNode) seq): (string * DataModelNode) list =
        props |> Seq.toList

    member inline _.Zero(): (string * DataModelNode) list =
        []

    member inline _.Delay(f: unit -> 'a): 'a =
        f ()

    member inline _.Combine(props1: (string * DataModelNode) list, props2: (string * DataModelNode) list): (string * DataModelNode) list =
        props1 @ props2

    member inline _.For(sequence: 'a seq, body: 'a -> (string * DataModelNode) list): (string * DataModelNode) list =
        sequence |> Seq.collect body |> Seq.toList

    member inline _.Run(props: (string * DataModelNode) list): DataModelNode =
        DataModelNode.Map (Map.ofList props)

[<AutoOpen>]
module DataModeMapBuilder =
    let dataModelMap = DataModelMapBuilder()

[<AutoOpen>]
module DataModelMapBuilderExtensions =

    [<AutoOpen>]
    module LowPriority =
        type DataModelMapBuilder with

            member this.Yield(_: string * bool as (key, value)): (string * DataModelNode) list =
                this.Yield((key, DataModelNode.Boolean value))

            member this.Yield(_: string * int as (key, value)): (string * DataModelNode) list =
                this.Yield((key, DataModelNode.Integer value))

            member this.Yield(_: string * float as (key, value)): (string * DataModelNode) list =
                this.Yield((key, DataModelNode.Float value))

            member this.Yield(_: string * string as (key, value)): (string * DataModelNode) list =
                this.Yield((key, DataModelNode.String value))

            member this.Yield(_: string * Cid as (key, value)): (string * DataModelNode) list =
                this.Yield((key, DataModelNode.Link value))
