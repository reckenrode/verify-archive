// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.FileSystem

open FSharp.Control.Tasks.Affine
open System.IO
open System.Threading.Tasks

open VerifyArchive.Archive
open VerifyArchive.Error

let CHUNK_SIZE = 2 <<< 18

let compare filesystemRoot (archive: Archive) = task {
    let tryOpenFile file =
        try
            File.OpenRead (Path.Combine (filesystemRoot, file)) |> Some
        with
        | :? IOException -> None

    let tasks =
        archive
        |> Archive.entries
        |> Seq.chunkBySize CHUNK_SIZE
        |> Seq.map (fun chunk ->
            chunk
            |> Array.map (fun entry -> task {
                let filename = entry |> Entry.name
                match tryOpenFile filename with
                | None -> return Some (filename, Missing)
                | Some file ->
                    use file = file
                    use zipFile = entry |> Entry.openStream
                    match! file |> StreamComparison.compare zipFile with
                    | Ok () -> return None
                    | Error error -> return Some (filename, error)
            })
        )
    let! errors =
        tasks
        |> Seq.map (fun tasks -> task {
            let! results = Task.WhenAll tasks
            return results |> Seq.choose id
        })
        |> Task.WhenAll
    let errors = errors |> Seq.concat

    if errors |> Seq.isEmpty
    then return Ok ()
    else return Error (errors |> List.ofSeq |> List.sortBy (fun (filename, _) -> filename))
}
