// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Tests.FileSystem

open Expecto
open FSharpx.Prelude
open System.IO

open VerifyArchive.Archive
open VerifyArchive.Error
open VerifyArchive.FileSystem
open VerifyArchive.Tests.Utility

[<Tests>]
let tests = testList "VerifyArchive.ZipArchive" [
    testTask "empty archive, returns success" {
        let expected = Ok ()

        use workingDirectory = TemporaryDirectory ()

        let emptyZip =
            workingDirectory.Path.FullName
            |> zipTestFiles []
            |> File.OpenRead
            |> Zip.openRead
        use emptyZip = Expect.wantOk emptyZip "the zip was opened"

        let! result = emptyZip |> compare "/"

        Expect.equal result expected "everything matches"
    }

    testList "archive with files" [
        testTask "when the file system has the same files, then they match" {
            let inputFiles = [
                "file 1", "this is a test"
                "file 2", "this is another test"
            ]
            let expected = Ok ()

            use workingDirectory = TemporaryDirectory ()
            let workingPath = workingDirectory.Path.FullName

            let filesPath = workingPath |> setUpPath inputFiles
            let zip =
                workingPath
                |> zipTestFiles inputFiles
                |> File.OpenRead
                |> Zip.openRead
            use zip = Expect.wantOk zip "the zip was opened"

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
            let zip =
                workingPath
                |> zipTestFiles [
                    "file 1", "this is a test"
                    "file 2", "this is another test"
                ]
                |> File.OpenRead
                |> Zip.openRead
            use zip = Expect.wantOk zip "the zip was opened"

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
            let zip =
                workingPath
                |> zipTestFiles [
                    "file 1", "this is a test"
                    "file 2", "this is a wrong value"
                ]
                |> File.OpenRead
                |> Zip.openRead
            use zip = Expect.wantOk zip "the zip was opened"

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
            let zip =
                workingPath
                |> zipTestFiles [
                    "file 1", "this is a test"
                    "file 2", "this is a wrong value"
                ]
                |> File.OpenRead
                |> Zip.openRead
            use zip = Expect.wantOk zip "the zip was opened"

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
            let zip =
                workingPath
                |> zipTestFiles inputFiles
                |> File.OpenRead
                |> Zip.openRead
            use zip = Expect.wantOk zip "the zip was opened"

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
            let zip =
                workingPath
                |> zipTestFiles [
                    Path.Combine ("directory 1", "file 1"), "this is a test"
                    Path.Combine ("directory 2", "file 2"), "this is a wrong value"
                    Path.Combine ("directory 3", "directory 4", "file 3"), "some missing text"
                ]
                |> File.OpenRead
                |> Zip.openRead
            use zip = Expect.wantOk zip "the zip was opened"

            let! result = zip |> compare filesPath.FullName

            Expect.equal result expected "file 2 does not match"
        }

        testTask "when the file’s name contains “\\” and the archive’s “_-backslash-_”, \
                  and they are the same, then they match" {
            let expected = Ok ()

            use workingDirectory = TemporaryDirectory ()
            let workingPath = workingDirectory.Path.FullName

            let files = workingPath |> setUpPath [
                "some file\\slash", "the contents"
            ]
            use zip =
                workingPath
                |> zipTestFiles [
                "some file_-backslash-_slash", "the contents"
                ]
                |> File.OpenRead
                |> Zip.openRead
                |> (flip Expect.wantOk "the zip was opened")

            let! result = zip |> compare files.FullName

            Expect.equal result expected "the files match"
        }
    ]
]
