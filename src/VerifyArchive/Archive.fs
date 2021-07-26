// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Archive

open FSharp.Control
open FSharpx.Option
open ICSharpCode.SharpZipLib.Zip
open System
open System.IO
open System.Text.RegularExpressions

open VerifyArchive.Error
open VerifyArchive.Stream

type DifferenceType =
    | Mismatch
    | Missing

type Difference = {
    filename: string
    ``type``: DifferenceType
}

type Entry = {
    Name: string
    Open: unit -> Stream
}

type Archive = {
    Entries: unit -> seq<Entry>
    Close: unit -> unit
}
with
    interface IDisposable with
        member this.Dispose () = this.Close ()

module Entry =
    let name entry =
        entry.Name

    let openStream entry =
        entry.Open ()

module Archive =
    let private BACKSLASH_REGEX = Regex ("_-backslash-_", RegexOptions.Compiled)

    let entries archive =
        archive.Entries ()

    let differences filesystemRoot (archive: Archive) =
        let tryOpenFile file =
            try
                File.OpenRead (Path.Combine (filesystemRoot, file)) |> Some
            with
            | :? IOException -> None

        let processEntry entry =
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

        asyncSeq {
            for entry in archive |> entries |> Seq.map processEntry do
                match! entry with
                | Some error -> yield error
                | None -> ()
        }

module Zip =
    let private liftEntryEnumerator (it: ZipFile) () = seq {
        for entry in it |> Seq.cast<ZipEntry> do
            yield { Name = entry.Name; Open = (fun () -> it.GetInputStream entry) }
    }

    let openRead (stream: Stream) =
        try
            let zip = ZipFile stream
            {
                Entries = liftEntryEnumerator zip
                Close = zip.Close
            }
            |> Ok
        with exn -> Error InvalidArchive
