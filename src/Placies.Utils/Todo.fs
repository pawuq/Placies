namespace global

open System

[<Obsolete("TODO")>]
type Todo = interface end

[<AutoOpen>]
module Todo =
    [<Obsolete("TODO", false)>]
    let inline todo<'a> : 'a =
        raise (NotImplementedException("TODO"))
