// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.FileSystem

open FSharp.Control
open FSharpx.Option
open System.IO
open System.Text.RegularExpressions

open VerifyArchive.Archive

let BACKSLASH_REGEX = Regex ("_-backslash-_", RegexOptions.Compiled)

type DifferenceType =
    | Mismatch
    | Missing

type Difference = {
    filename: string
    ``type``: DifferenceType
}

let private processEntry tryOpenFile entry =
    let filename = entry|> Entry.name
    maybe {
        let! file =
            tryOpenFile filename
            |> Option.orElseWith (fun () -> tryOpenFile <| BACKSLASH_REGEX.Replace (filename, "\\"))
        return async {
            use file = file
            use zipFile = entry |> Entry.openStream
            let! result = file |> Stream.matches zipFile
            if result
            then return None
            else return Some { filename = filename; ``type`` = Mismatch }
        }
    }
    |> Option.defaultValue (async { return Some { filename = filename; ``type`` = Missing } })

let differences filesystemRoot (archive: Archive) = asyncSeq {
    let tryOpenFile file =
        try
            File.OpenRead (Path.Combine (filesystemRoot, file)) |> Some
        with
        | :? IOException -> None

    let processEntry = processEntry tryOpenFile

    for entry in archive |> Archive.entries |> Seq.map processEntry do
        match! entry with
        | Some error -> yield error
        | None -> ()
}
