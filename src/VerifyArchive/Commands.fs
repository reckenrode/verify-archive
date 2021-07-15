// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Commands

open FSharp.Control.Tasks.Affine
open System.CommandLine
open System.IO
open System.IO.Compression

open VerifyArchive.Error

type Options = {
    input: FileInfo
    root: DirectoryInfo
}

let private openArchive (archive: FileInfo) =
    try
        archive.FullName |> ZipFile.OpenRead |> Ok
    with
    | :? IOException -> Error $"ERROR: could not open {archive}"
    | :? System.NotSupportedException | :? InvalidDataException ->
        Error $"ERROR: {archive} is not a zip file"

let private formatFailure = function
    | filename, Mismatch (sourceHash, backupHash) ->
        $"{filename} mismatch, {sourceHash.[0..7]} ≠ {backupHash.[0..7]}\n"
    | filename, _ ->
        $"{filename} missing\n"

let private showComparisonFailures failures (console: IConsole) =
    failures |> List.iter (formatFailure >> console.Error.Write)
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
