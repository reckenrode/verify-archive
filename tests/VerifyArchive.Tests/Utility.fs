// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Tests.Utility

open System
open System.IO
open System.IO.Compression

type TemporaryDirectory () =
    let mutable isDisposed = false

    member val Path =
        Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ())
        |> Directory.CreateDirectory with get

    interface IDisposable with
        member this.Dispose () =
            if not isDisposed
            then
                this.Path.Delete (recursive = true)
                GC.SuppressFinalize this

    override this.Finalize () =
        (this :> IDisposable).Dispose ()

let setUpPath files path =
    let dir = Path.Combine (path, Path.GetRandomFileName ()) |> Directory.CreateDirectory
    files |> List.iter (fun (file: string, contents) ->
        let dir =
            Path.Combine (dir.FullName, Path.GetDirectoryName file)
            |> Directory.CreateDirectory
        let outputPath = Path.Combine (dir.FullName, Path.GetFileName file)
        File.WriteAllText (outputPath, contents))
    dir

let zipTestFiles files path =
    let zipPath = setUpPath files path
    let outputFilename = Path.Combine (path, Path.GetRandomFileName ())
    ZipFile.CreateFromDirectory (zipPath.FullName, outputFilename)
    outputFilename
