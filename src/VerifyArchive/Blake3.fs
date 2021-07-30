// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Blake3

open System
open System.IO

let computeHashAsync (stream: Stream) = async {
    let backingArray = GC.AllocateUninitializedArray (512 * 1024)
    let buffer = Memory backingArray
    use hasher = Blake3.Hasher.New ()

    let! asyncBytesRead = (stream.ReadAsync buffer).AsTask () |> Async.AwaitTask
    let mutable bytesRead = asyncBytesRead
    while bytesRead > 0 do
        let span = ReadOnlySpan (backingArray, 0, bytesRead)
        if bytesRead < (128 * 1024)
        then hasher.Update span
        else hasher.UpdateWithJoin span
        let! asyncBytesRead = (stream.ReadAsync buffer).AsTask () |> Async.AwaitTask
        bytesRead <- asyncBytesRead

    return hasher.Finalize ()
}
