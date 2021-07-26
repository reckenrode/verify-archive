// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Stream

#nowarn "0025"

open FSharp.Control
open FSharp.Control.Tasks.NonAffine.Unsafe
open System.IO
open System
open System.Text

let private hashToString hash =
    hash
    |> Array.fold (fun (sb: StringBuilder) b -> sb.Append $"{b:x2}") (StringBuilder ())
    |> string

let private computeHashAsync (stream: Stream) = uvtask {
    let backingArray = GC.AllocateUninitializedArray (512 * 1024)
    let buffer = Memory backingArray
    use hasher = Blake3.Hasher.New ()

    let! asyncBytesRead = stream.ReadAsync buffer
    let mutable bytesRead = asyncBytesRead
    while bytesRead > 0 do
        let span = ReadOnlySpan (backingArray, 0, bytesRead)
        if bytesRead < (128 * 1024)
        then hasher.Update span
        else hasher.UpdateWithJoin span
        let! asyncBytesRead = stream.ReadAsync buffer
        bytesRead <- asyncBytesRead

    return hasher.Finalize ()
}

let matches (backupStream: Stream) (sourceStream: Stream) = async {
    let hashes =
        [|
            (computeHashAsync sourceStream).AsTask () |> Async.AwaitTask;
            (computeHashAsync backupStream).AsTask () |> Async.AwaitTask
        |]
        |> Async.Parallel
    let! hashes = hashes
    let [| sourceHash; backupHash |] = hashes
    return sourceHash = backupHash
}
