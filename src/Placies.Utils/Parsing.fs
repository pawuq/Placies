namespace Placies.Utils.Parsing

[<RequireQualifiedAccess>]
type ParseError<'e> =
    | Incomplete
    | Error of 'e
