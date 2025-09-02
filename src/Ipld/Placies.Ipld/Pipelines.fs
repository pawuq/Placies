namespace Placies.Ipld

open System.IO.Pipelines
open System.Threading.Tasks


[<AutoOpen>]
module PipelinesExtensions =

    [<RequireQualifiedAccess>]
    module PipeReader =

        /// <summary>
        /// Calls <c>CompleteAsync()</c> when <c>action</c> is finished.
        /// </summary>
        let inline usingAsync (pipeReader: PipeReader) ([<InlineIfLambda>] action: PipeReader -> Task<'a>) : Task<'a> = task {
            try
                let! a = action pipeReader
                do! pipeReader.CompleteAsync()
                return a
            with ex ->
                do! pipeReader.CompleteAsync(ex)
                return reraiseAnywhere ex
        }

    [<RequireQualifiedAccess>]
    module PipeWriter =

        /// <summary>
        /// Calls <c>CompleteAsync()</c> when <c>action</c> is finished.
        /// </summary>
        let inline usingAsync (pipeWriter: PipeWriter) ([<InlineIfLambda>] action: PipeWriter -> Task<'a>) : Task<'a> = task {
            try
                let! a = action pipeWriter
                do! pipeWriter.CompleteAsync()
                return a
            with ex ->
                do! pipeWriter.CompleteAsync(ex)
                return reraiseAnywhere ex
        }
