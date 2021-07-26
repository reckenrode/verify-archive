// SPDX-License-Identifier: GPL-3.0-only

open System.CommandLine.Parsing

[<EntryPoint>]
let main argv =
    let parser = VerifyArchive.Cli.builder.Build ()
    parser.Invoke argv
