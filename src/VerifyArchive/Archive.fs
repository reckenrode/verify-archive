// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Archive

open ICSharpCode.SharpZipLib.Zip
open System
open System.IO

open VerifyArchive.Error

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

module Archive =
    let entries archive =
        archive.Entries ()

module Entry =
    let name entry =
        entry.Name

    let openStream entry =
        entry.Open ()

let private liftEntryEnumerator (it: ZipFile) () = seq {
    for entry in it |> Seq.cast<ZipEntry> do
        yield { Name = entry.Name; Open = (fun () -> it.GetInputStream entry) }
}

module Zip =
    let openRead (stream: Stream) =
        try
            let zip = ZipFile stream
            {
                Entries = liftEntryEnumerator zip
                Close = zip.Close
            }
            |> Ok
        with exn -> Error InvalidArchive
