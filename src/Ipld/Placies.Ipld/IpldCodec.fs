namespace Placies.Ipld

open System
open System.Buffers
open System.IO
open System.IO.Pipelines
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Placies
open Placies.Utils
open Placies.Multiformats

type IIpldCodec =
    abstract CodecInfo: MultiCodecInfo
    abstract TryEncode: bufferWriter: IBufferWriter<byte> * dataModelNode: DataModelNode -> Result<unit, exn>
    abstract TryDecode: buffer: ReadOnlySequence<byte> -> Result<DataModelNode, exn>

[<AutoOpen>]
module CodecExtensions =

    type PipeReader with
        member this.ReadAllAsync([<Optional>] cancellationToken: CancellationToken): ValueTask<ReadResult> =
            this.ReadAtLeastAsync(Int32.MaxValue, cancellationToken)

    type IIpldCodec with

        member this.TryEncodeAsync(writeToStream: Stream, dataModelNode: DataModelNode): TaskResult<unit, exn> = task {
            return! PipeWriter.usingAsync (PipeWriter.Create(writeToStream)) ^fun pipeWriter -> taskResult {
                return! this.TryEncode(pipeWriter, dataModelNode)
            }
        }
        member this.TryDecodeAsync(stream: Stream, [<Optional>] ct: CancellationToken): TaskResult<DataModelNode, exn> = task {
            return! PipeReader.usingAsync (PipeReader.Create(stream)) ^fun pipeReader -> task {
                let! readResult = pipeReader.ReadAllAsync(ct)
                let res = this.TryDecode(readResult.Buffer)
                pipeReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End)
                return res
            }
        }

        member this.TryEncodeWithCid(bufferWriter: IBufferWriter<byte>, dataModelNode: DataModelNode, cidVersion: int, cidMultihashInfo: MultiHashInfo): Result<Cid, exn> = result {
            let arrayBufferWriter = ArrayBufferWriter<byte>()
            do! this.TryEncode(arrayBufferWriter, dataModelNode)
            bufferWriter.Write(arrayBufferWriter.WrittenSpan)

            let multihash = MultiHash.computeFromBytes arrayBufferWriter.WrittenSpan cidMultihashInfo
            let cid = Cid.create cidVersion this.CodecInfo.Code multihash
            return cid
        }
        member this.TryEncodeWithCidAsync(writeToStream: Stream, dataModelNode: DataModelNode, cidVersion: int, cidMultihashInfo: MultiHashInfo): TaskResult<Cid, exn> = task {
            return! PipeWriter.usingAsync (PipeWriter.Create(writeToStream)) ^fun pipeWriter -> taskResult {
                return! this.TryEncodeWithCid(pipeWriter, dataModelNode, cidVersion, cidMultihashInfo)
            }
        }
