// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Commands

open FSharp.Control
open System.CommandLine
open System.IO

open VerifyArchive.Archive

type Options = {
    input: FileInfo
    root: DirectoryInfo
}

let private openArchive (archive: FileInfo) =
    try
        archive.FullName
        |> File.OpenRead
        |> Zip.openRead
        |> Result.mapError (fun _ -> $"ERROR: {archive} is not a zip file")
    with
    | :? IOException -> Error $"ERROR: could not open {archive}"

let private renderError = function
    | { filename = filename; ``type`` = Mismatch } ->
        (Path.GetDirectoryName filename, $"- “{Path.GetFileName filename}” does not match")
    | { filename = filename; ``type`` = Missing } ->
        (Path.GetDirectoryName filename, $"- “{Path.GetFileName filename}” is missing")

let private showDifferences diffs (console: IConsole) =
    let diffs =
        diffs
        |> List.groupBy fst
        |> List.sortBy fst
        |> List.map (fun (path, errors) ->
            let errors = errors |> List.map snd |> List.sort |> String.concat "\n"
            $"{path}\n{errors}\n")
        |> String.concat "\n"
    console.Error.Write $"{diffs}"
    -1

let private showError msg (console: IConsole) =
    console.Error.Write $"{msg}\n"
    -1

let private showSuccess (console: IConsole) =
    console.Out.Write "All files matched!\n"
    0

let verify (options: Options) (console: IConsole) =
    async {
        match options.input |> openArchive with
        | Error msg -> return console |> showError msg
        | Ok zip ->
            let! diffs =
                zip
                |> Archive.differences options.root.FullName
                |> AsyncSeq.map renderError
                |> AsyncSeq.toListAsync

            if diffs |> List.isEmpty
            then return console |> showSuccess
            else return console |> showDifferences diffs
    }
    |> Async.StartAsTask
