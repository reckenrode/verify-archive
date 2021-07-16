// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Cli

open System.CommandLine
open System.CommandLine.Builder
open System.CommandLine.Invocation
open System.IO
open System.Reflection

let private rootCommand =
    let assembly = Assembly.GetEntryAssembly ()
    let attribute = assembly.GetCustomAttribute typeof<AssemblyDescriptionAttribute>
    let description = (attribute :?> AssemblyDescriptionAttribute).Description
    let root =
        RootCommand (
            description,
            Name = "verify-archive",
            Handler = CommandHandler.Create Commands.verify
        )
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
    root

let builder = (CommandLineBuilder rootCommand).UseDefaults ()
