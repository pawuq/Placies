namespace Placies.Cli.IpldSubCommand.CarSubCommand

open System
open System.IO
open System.IO.Pipelines
open System.Threading
open Argu
open FsToolkit.ErrorHandling
open Placies.Ipld.DagJson
open Placies.Ipld
open Placies.Ipld.Car.CarV1
open Placies.Ipld.DagCbor
open Placies.Multiformats

[<RequireQualifiedAccess>]
type CarInspectArgs =
    | Input of path: string
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Input _ -> "Path of a file with input data, or when is -, read from stdin"

[<RequireQualifiedAccess>]
type CarArgs =
    | [<CliPrefix(CliPrefix.None); SubCommand>] Inspect of ParseResults<CarInspectArgs>
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Inspect _ -> "Inspect CAR file"

[<RequireQualifiedAccess>]
module CarArgs =

    let handle
            (multiBaseProvider: IMultiBaseProvider) (ipldCodecProvider: IIpldCodecProvider)
            (carArgsParseResults: ParseResults<CarArgs>)
            =
        taskResult {
            match carArgsParseResults.GetSubCommand() with
            | CarArgs.Inspect carInspectArgsParseResults ->
                let dagCborIpldCodec = DagCborIpldCodec()
                let inputArg = carInspectArgsParseResults.GetResult(CarInspectArgs.Input)
                use inputStream =
                    if inputArg = "-" then
                        Console.OpenStandardInput()
                    else
                        File.OpenRead(inputArg)

                let pipeReader = PipeReader.Create(inputStream)

                let! car = CarV1.readFromPipeReader pipeReader dagCborIpldCodec CancellationToken.None |> TaskResult.mapError string
                do! pipeReader.CompleteAsync()

                let! carDataModel = car |> CarV1.toDataModel ipldCodecProvider |> TaskResult.mapError string

                use stdout = Console.OpenStandardOutput()
                do! (DagJsonIpldCodec(multiBaseProvider) :> IIpldCodec).TryEncodeAsync(stdout, carDataModel) |> TaskResult.mapError string
        }
