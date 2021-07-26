// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Stream

#nowarn "0025"

open FSharp.Control
open System.IO
open System.Security.Cryptography
open System.Text

let private hashToString hash =
    hash
    |> Array.fold (fun (sb: StringBuilder) b -> sb.Append $"{b:x2}") (StringBuilder ())
    |> string

let matches (backupStream: Stream) (sourceStream: Stream) = async {
    use sourceHasher = SHA256.Create ()
    use backupHasher = SHA256.Create ()
    let hashers =
        [|
            sourceHasher.ComputeHashAsync sourceStream |> Async.AwaitTask;
            backupHasher.ComputeHashAsync backupStream|> Async.AwaitTask
        |]
        |> Async.Parallel
    let! hashers = hashers
    let [| sourceHash; backupHash |] = hashers
    return sourceHash = backupHash
}
