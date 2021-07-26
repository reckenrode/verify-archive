// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Commands

open FSharp.Control
open FSharp.Control.Tasks.Affine
open System.CommandLine
open System.IO

open VerifyArchive.Archive
open VerifyArchive.FileSystem

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

let (|Basename|) : string -> string = Path.GetFileName

let private formatErrors error = async {
    return match error with
            | { filename = filename; ``type`` = Mismatch } ->
                (filename, $"- “{Path.GetFileName filename}” does not match")
            | { filename = filename; ``type`` = Missing } ->
                (filename, $"- “{Path.GetFileName filename}” is missing")
}

let private renderErrors (directory, errors) = async {
    let! errors =
        errors
        |> AsyncSeq.mapAsyncParallel formatErrors
        |> AsyncSeq.toListAsync
    return (directory, errors |> List.map snd |> String.concat "\n")
}

let private showDifferences diffs (console: IConsole) =
    let diffs =
        diffs
        |> List.map (fun (path, errors) -> $"{path}\n{errors}\n")
        |> String.concat "\n"
    console.Error.Write $"{diffs}"
    -1

let private showError msg (console: IConsole) =
    console.Error.Write $"{msg}\n"
    -1

let private showSuccess (console: IConsole) =
    console.Out.Write "All files matched!\n"
    0

let verify (options: Options) (console: IConsole) = task {
    match options.input |> openArchive with
    | Error msg -> return console |> showError msg
    | Ok zip ->
        let! diffs =
            zip
            |> FileSystem.differences options.root.FullName
            |> AsyncSeq.groupBy (fun result -> Path.GetDirectoryName result.filename)
            |> AsyncSeq.mapAsyncParallel renderErrors
            |> AsyncSeq.toListAsync

        if diffs |> List.isEmpty
        then return console |> showSuccess
        else return console |> showDifferences diffs
}
