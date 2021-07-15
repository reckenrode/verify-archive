// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Commands

open FSharp.Control.Tasks.Affine
open System.CommandLine
open System.IO

open VerifyArchive.Archive
open VerifyArchive.Error

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

let private formatErrors errors =
    errors
    |> Seq.map (function
        | Basename filename, Mismatch (sourceHash, backupHash) ->
            $"- “{filename}” mismatch, {sourceHash.[0..7]} ≠ {backupHash.[0..7]}"
        | Basename filename, _ ->
            $"- “{filename}” is missing")
    |> String.concat "\n"

let private formatFailure (path, errors) =
    $"{path}\n{formatErrors errors}\n"

let private showComparisonFailures (failures: list<string * Error>) (console: IConsole) =
    failures
    |> Seq.groupBy (fst >> Path.GetDirectoryName)
    |> Seq.map formatFailure
    |> String.concat "\n"
    |> console.Error.Write
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
        match! zip |> FileSystem.compare options.root.FullName with
        | Ok () -> return console |> showSuccess
        | Error errors -> return console |> showComparisonFailures errors
}
