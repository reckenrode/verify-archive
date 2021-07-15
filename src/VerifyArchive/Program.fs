// SPDX-License-Identifier: GPL-3.0-only

open System.CommandLine.Parsing

[<EntryPoint>]
let main argv =
    let parser = VerifyArchive.Cli.builder.Build ()
    let task = parser.InvokeAsync argv
    let awaiter = task.GetAwaiter ()
    awaiter.GetResult ()
