// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Stream

#nowarn "0025"

open FSharp.Control.Tasks.Affine
open System.IO
open System.Security.Cryptography
open System.Text
open System.Threading.Tasks

open VerifyArchive.Error

let private hashToString hash =
    hash
    |> Array.fold (fun (sb: StringBuilder) b -> sb.Append $"{b:x2}") (StringBuilder ())
    |> string

let compare (backupStream: Stream) (sourceStream: Stream) = task {
    use sourceHasher = SHA256.Create ()
    use backupHasher = SHA256.Create ()
    let sourceHash = sourceHasher.ComputeHashAsync sourceStream
    let backupHash = backupHasher.ComputeHashAsync backupStream
    let! [|sourceHash; backupHash|] = Task.WhenAll (sourceHash, backupHash)
    return
        if sourceHash = backupHash
        then Ok ()
        else
            Mismatch (sourceHash = hashToString sourceHash, backupHash = hashToString backupHash)
            |> Error
}
