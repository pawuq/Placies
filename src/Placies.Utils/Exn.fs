namespace Placies.Utils

namespace global

[<AutoOpen>]
module GlobalExnUtils =

    // From https://gist.github.com/JamesFaix/af0bab95aca867ae2d1b7c2c2de2a658
    /// <summary>
    /// Rethrows an exception, while preserving stacktrace.
    /// Can be used in computation expressions, unlike 'reraise' keyword.
    /// </summary>
    let inline reraiseAnywhere<'a> (e: exn) : 'a =
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e).Throw()
        Unchecked.defaultof<'a>
