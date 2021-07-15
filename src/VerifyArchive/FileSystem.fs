// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.FileSystem

open FSharp.Control.Tasks.Affine
open System.IO
open System.IO.Compression
open System.Threading.Tasks

open VerifyArchive.Error

let CHUNK_SIZE = 256

let compare filesystemRoot (archive: ZipArchive) = task {
    let tryOpenFile file =
        try
            File.OpenRead (Path.Combine (filesystemRoot, file)) |> Some
        with
        | :? IOException -> None

    let mutable errors = []
    for chunk in archive.Entries |> Seq.chunkBySize CHUNK_SIZE do
        let! results =
            chunk
            |> Seq.map (fun entry -> task {
                let filename = entry.FullName
                match tryOpenFile filename with
                | None -> return Some (filename, Missing)
                | Some file ->
                    use file = file
                    use zipFile = entry.Open ()
                    match! file |> StreamComparison.compare zipFile with
                    | Ok () -> return None
                    | Error error -> return Some (filename, error)
            })
            |> Task.WhenAll
        results
        |> Seq.choose id
        |> Seq.iter (fun error -> errors <- error :: errors)

    if errors |> List.isEmpty
    then return Ok ()
    else return Error (errors |> List.sortBy (fun (filename, _) -> filename))
}
