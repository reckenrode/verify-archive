// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Cli

open System.CommandLine
open System.CommandLine.Builder
open System.CommandLine.Invocation
open System.IO

let private rootCommand =
    let root = RootCommand ()
    root.AddOption <| Option<FileInfo> (
        [| "-i"; "--input" |],
        description = "the archive to check",
        IsRequired = true
    )
    root.AddOption <| Option<DirectoryInfo> (
        [| "-r"; "--root" |],
        getDefaultValue = (fun () -> DirectoryInfo "/Volumes"),
        description = "the root path from which to check"
    )
    root.Handler <- CommandHandler.Create Commands.verify
    root

let builder = CommandLineBuilder rootCommand
