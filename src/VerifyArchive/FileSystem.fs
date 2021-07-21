// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.FileSystem

open FSharp.Control.Tasks.Affine
open FSharpx.Option
open FSharpx.Prelude
open System.IO
open System.Threading.Tasks
open System.Text.RegularExpressions

open VerifyArchive.Archive
open VerifyArchive.Error

let CHUNK_SIZE = 64

let BACKSLASH_REGEX = Regex ("_-backslash-_", RegexOptions.Compiled)

let private processEntry tryOpenFile entry =
    let filename = entry|> Entry.name
    maybe {
        let! file =
            tryOpenFile filename
            |> Option.orElseWith (fun () -> tryOpenFile <| BACKSLASH_REGEX.Replace (filename, "\\"))
        return task {
            use file = file
            use zipFile = entry |> Entry.openStream
            match! file |> StreamComparison.compare zipFile with
            | Ok () -> return None
            | Error error -> return Some (filename, error)
        }
    }
    |> Option.defaultValue (Some (filename, Missing) |> Task.FromResult)

let compare filesystemRoot (archive: Archive) = task {
    let tryOpenFile file =
        try
            File.OpenRead (Path.Combine (filesystemRoot, file)) |> Some
        with
        | :? IOException -> None

    let tasks =
        archive
        |> Archive.entries
        |> Seq.map (processEntry tryOpenFile)

    let mutable errors = []
    for chunk in tasks |> Seq.chunkBySize CHUNK_SIZE do
        let! results = Task.WhenAll chunk
        let chunkErrors =
            results
            |> Seq.choose id
            |> Seq.toList
        if not (List.isEmpty chunkErrors) then errors <- chunkErrors :: errors

    if errors |> List.isEmpty
    then return Ok ()
    else
        return errors
        |> List.collect id
        |> List.sortBy fst
        |> Error
}
