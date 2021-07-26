// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Tests.FileSystem

open Expecto
open FSharp.Control
open FSharpx.Prelude
open System.IO

open VerifyArchive.Archive
open VerifyArchive.FileSystem
open VerifyArchive.Tests.Utility

[<Tests>]
let tests = testList "VerifyArchive.ZipArchive" [
    testAsync "empty archive, returns success" {
        let expected = []

        use workingDirectory = TemporaryDirectory ()

        let emptyZip =
            workingDirectory.Path.FullName
            |> zipTestFiles []
            |> File.OpenRead
            |> Zip.openRead
        use emptyZip = Expect.wantOk emptyZip "the zip was opened"

        let! result = emptyZip |> differences "/" |> AsyncSeq.toListAsync

        Expect.equal result expected "everything matches"
    }

    testList "archive with files" [
        testAsync "when the file system has the same files, then they match" {
            let inputFiles = [
                "file 1", "this is a test"
                "file 2", "this is another test"
            ]
            let expected = []

            use workingDirectory = TemporaryDirectory ()
            let workingPath = workingDirectory.Path.FullName

            let filesPath = workingPath |> setUpPath inputFiles
            let zip =
                workingPath
                |> zipTestFiles inputFiles
                |> File.OpenRead
                |> Zip.openRead
            use zip = Expect.wantOk zip "the zip was opened"

            let! result = zip |> differences filesPath.FullName |> AsyncSeq.toListAsync

            Expect.equal result expected "everything matches"
        }

        testAsync "when the file system has the same files and extra, then they still match" {
            let expected = []

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

            let! result = zip |> differences filesPath.FullName |> AsyncSeq.toListAsync

            Expect.equal result expected "everything matches"
        }

        testAsync "when the file system has different files, then they do not match" {
            let expected = [
                { filename = "file 2"; ``type`` = Mismatch }
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

            let! result = zip |> differences filesPath.FullName |> AsyncSeq.toListAsync

            Expect.equal result expected "file 2 does not match"
        }

        testAsync "when the file system is missing files, then they do not match" {
            let expected = [
                { filename = "file 2"; ``type`` = Missing }
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

            let! result = zip |> differences filesPath.FullName |> AsyncSeq.toListAsync

            Expect.equal result expected "file 2 does not match"
        }

        testAsync "when the files are in directories and are the same, then they match" {
            let inputFiles = [
                Path.Combine ("directory 1", "file 1"), "this is a test"
                Path.Combine ("directory 2", "directory 3", "file 2"), "this is another test"
            ]
            let expected = []

            use workingDirectory = TemporaryDirectory ()
            let workingPath = workingDirectory.Path.FullName

            let filesPath = workingPath |> setUpPath inputFiles
            let zip =
                workingPath
                |> zipTestFiles inputFiles
                |> File.OpenRead
                |> Zip.openRead
            use zip = Expect.wantOk zip "the zip was opened"

            let! result = zip |> differences filesPath.FullName |> AsyncSeq.toListAsync

            Expect.equal result expected "everything matches"
        }

        testAsync "when the files are in directories and are not the same, then they don’t match" {
            let expected = [
                { filename = Path.Combine ("directory 2", "file 2"); ``type`` = Mismatch }
                { filename = Path.Combine ("directory 3", "directory 4", "file 3"); ``type`` = Missing }
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

            let! result = zip |> differences filesPath.FullName |> AsyncSeq.toListAsync

            Expect.equal result expected "file 2 does not match"
        }

        testAsync "when the file’s name contains “\\” and the archive’s “_-backslash-_”, \
                  and they are the same, then they match" {
            let expected = []

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

            let! result = zip |> differences files.FullName |> AsyncSeq.toListAsync

            Expect.equal result expected "the files match"
        }
    ]
]
