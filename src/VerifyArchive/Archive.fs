// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Archive

open FSharp.Control
open FSharpx.Option
open ICSharpCode.SharpZipLib.Zip
open System
open System.IO
open System.Text.RegularExpressions

open VerifyArchive.Error

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
    Entries: unit -> AsyncSeq<Entry>
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

        let processFile entry =
            let filename = entry |> Entry.name
            maybe {
                let! file =
                    tryOpenFile filename
                    |> Option.orElseWith (fun () -> tryOpenFile <| BACKSLASH_REGEX.Replace (filename, "\\"))
                return async {
                    use file = file
                    let! hash = Blake3.computeHashAsync file
                    return (filename, Some hash)
                }
            }
            |> Option.defaultValue (async { return (filename, None) })

        let processEntry entry = async {
            use file = entry |> Entry.openStream
            return! Blake3.computeHashAsync file
        }

        let processChecksums zipHash = function
            | (_, Some fileHash) when fileHash = zipHash -> None
            | (filename, Some _) -> Some { filename = filename; ``type`` = Mismatch }
            | (filename, None) -> Some { filename = filename; ``type`` = Missing }

        let fileCheckums =
            archive
            |> entries
            |> AsyncSeq.mapAsync processFile

        let zipChecksums =
            archive
            |> entries
            |> AsyncSeq.mapAsync processEntry

        AsyncSeq.zipWithParallel processChecksums zipChecksums fileCheckums
        |> AsyncSeq.choose id

module Zip =
    let private liftEntryEnumerator (it: ZipFile) () = asyncSeq {
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
