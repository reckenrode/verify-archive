// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Tests.FileSystem

open Expecto
open System
open System.IO
open System.IO.Compression

open VerifyArchive.Error
open VerifyArchive.FileSystem

type private TemporaryDirectory () =
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

[<Tests>]
let tests = testList "VerifyArchive.ZipArchive" [
    testTask "empty archive, returns success" {
        let expected = Ok ()

        use workingDirectory = TemporaryDirectory ()

        use emptyZip =
            let zipPath = Path.Combine (workingDirectory.Path.FullName, Path.GetRandomFileName ())
            (ZipFile.Open (zipPath, ZipArchiveMode.Create)).Dispose ()
            ZipFile.OpenRead zipPath

        let! result = emptyZip |> compare "/"

        Expect.equal result expected "everything matches"
    }

    testList "archive with files" [
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
            ZipFile.OpenRead outputFilename

        testTask "when the file system has the same files, then they match" {
            let inputFiles = [
                "file 1", "this is a test"
                "file 2", "this is another test"
            ]
            let expected = Ok ()

            use workingDirectory = TemporaryDirectory ()
            let workingPath = workingDirectory.Path.FullName

            let filesPath = workingPath |> setUpPath inputFiles
            use zip = workingPath |> zipTestFiles inputFiles

            let! result = zip |> compare filesPath.FullName

            Expect.equal result expected "everything matches"
        }

        testTask "when the file system has the same files and extra, then they still match" {
            let expected = Ok ()

            use workingDirectory = TemporaryDirectory ()
            let workingPath = workingDirectory.Path.FullName

            let filesPath = workingPath |> setUpPath [
                "file 1", "this is a test"
                "file 2", "this is another test"
                "file 3", "this is a third file"
            ]
            use zip = workingPath |> zipTestFiles [
                "file 1", "this is a test"
                "file 2", "this is another test"
            ]

            let! result = zip |> compare filesPath.FullName

            Expect.equal result expected "everything matches"
        }

        testTask "when the file system has different files, then they do not match" {
            let expected = Error [
                "file 2", Mismatch (
                    sourceHash = "f69bff44070ba35d7169196ba0095425979d96346a31486b507b4a3f77bd356d",
                    backupHash = "f2415a036b22d43d4d383fc9e0263d9a98556318f1e315370e6b9770862992a4"
                )
            ]

            use workingDirectory = TemporaryDirectory ()
            let workingPath = workingDirectory.Path.FullName

            let filesPath = workingPath |> setUpPath [
                "file 1", "this is a test"
                "file 2", "this is another test"
            ]
            let zip = workingPath |> zipTestFiles [
                "file 1", "this is a test"
                "file 2", "this is a wrong value"
            ]

            let! result = zip |> compare filesPath.FullName

            Expect.equal result expected "file 2 does not match"
        }

        testTask "when the file system is missing files, then they do not match" {
            let expected = Error [
                "file 2", Missing
            ]

            use workingDirectory = TemporaryDirectory ()
            let workingPath = workingDirectory.Path.FullName

            let filesPath = workingPath |> setUpPath [
                "file 1", "this is a test"
            ]
            let zip = workingPath |> zipTestFiles [
                "file 1", "this is a test"
                "file 2", "this is a wrong value"
            ]

            let! result = zip |> compare filesPath.FullName

            Expect.equal result expected "file 2 does not match"
        }

        testTask "when the files are in directories and are the same, then they match" {
            let inputFiles = [
                Path.Combine ("directory 1", "file 1"), "this is a test"
                Path.Combine ("directory 2", "directory 3", "file 2"), "this is another test"
            ]
            let expected = Ok ()

            use workingDirectory = TemporaryDirectory ()
            let workingPath = workingDirectory.Path.FullName

            let filesPath = workingPath |> setUpPath inputFiles
            use zip = workingPath |> zipTestFiles inputFiles

            let! result = zip |> compare filesPath.FullName

            Expect.equal result expected "everything matches"
        }

        testTask "when the files are in directories and are not the same, then they don’t match" {
            let expected = Error [
                Path.Combine ("directory 2", "file 2"), Mismatch (
                    sourceHash = "f69bff44070ba35d7169196ba0095425979d96346a31486b507b4a3f77bd356d",
                    backupHash = "f2415a036b22d43d4d383fc9e0263d9a98556318f1e315370e6b9770862992a4"
                )
                Path.Combine ("directory 3", "directory 4", "file 3"), Missing
            ]

            use workingDirectory = TemporaryDirectory ()
            let workingPath = workingDirectory.Path.FullName

            let filesPath = workingPath |> setUpPath [
                Path.Combine ("directory 1", "file 1"), "this is a test"
                Path.Combine ("directory 2", "file 2"), "this is another test"
            ]
            let zip = workingPath |> zipTestFiles [
                Path.Combine ("directory 1", "file 1"), "this is a test"
                Path.Combine ("directory 2", "file 2"), "this is a wrong value"
                Path.Combine ("directory 3", "directory 4", "file 3"), "some missing text"
            ]

            let! result = zip |> compare filesPath.FullName

            Expect.equal result expected "file 2 does not match"
        }
    ]
]
