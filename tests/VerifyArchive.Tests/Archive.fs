// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Tests.Archive

open Expecto

open System.IO

open VerifyArchive.Archive
open VerifyArchive.Error
open VerifyArchive.Tests.Utility

[<Tests>]
let tests = testList "VerifyArchive.Archive" [
    testList "opening zip archives" [
        test "when the path is a zip file, it opens" {
            let expected = Ok ()

            use workingDirectory = TemporaryDirectory ()
            let workingPath = workingDirectory.Path.FullName

            let zip = workingPath |> zipTestFiles [
                "test", "some data"
            ]
            let result = zip |> File.OpenRead |> Zip.openRead
            let result = result |> Result.map (fun file -> file.Close ())

            Expect.equal result expected "the zip was opened"
        }

        test "when the path is not a zip file, it returns an error" {
            let expected = InvalidArchive

            use stream = MemoryStream ()
            use writer = StreamWriter stream

            fprintfn writer "This is obviously not a zip file"
            writer.Flush ()

            let result = Zip.openRead stream

            let error = Expect.wantError result "an error was returned"
            Expect.equal error expected "the error matches"
        }
    ]

    testList "reading zip archives" [
        test "when the file is empty, then there are no entries" {
            let expected = true

            use workingDirectory = TemporaryDirectory ()
            let workingPath = workingDirectory.Path.FullName

            let zip = workingPath |> zipTestFiles [] |> File.OpenRead |> Zip.openRead
            use zip = Expect.wantOk zip "the zip was read"

            let entries = zip |> Archive.entries
            let hasEntries = entries |> Seq.isEmpty

            Expect.equal hasEntries expected "the zip has no entries"
        }
    ]

    testTask "when the zip has entries, then it has the same number of entries" {
        let expected = [
            "test 1", "some data"
            "test 2", "more data"
            Path.Combine ("test 3", "test 4"), "other data"
        ]

        use workingDirectory = TemporaryDirectory ()
        let workingPath = workingDirectory.Path.FullName

        let zip = workingPath |> zipTestFiles expected
        let zip = File.OpenRead zip |> Zip.openRead
        use zip = Expect.wantOk zip "the zip was read"

        let entries = zip |> Archive.entries

        Expect.equal (Seq.length entries) (List.length expected) "the lengths match"
    }

    testTask "when the zip has entries, then it has matching data for the entries" {
        let expected = [
            "test 1", "some data"
            "test 2", "more data"
            Path.Combine ("test 3", "test 4"), "other data"
        ]

        use workingDirectory = TemporaryDirectory ()
        let workingPath = workingDirectory.Path.FullName

        let zip = workingPath |> zipTestFiles expected
        let zip = File.OpenRead zip |> Zip.openRead
        use zip = Expect.wantOk zip "the zip was read"

        let entries = zip |> Archive.entries

        for entry, (name, data) in (entries, expected) ||> Seq.zip do
            Expect.equal (Entry.name entry) name "the names match"
            use stream = entry |> Entry.openStream |> StreamReader
            let entryData = stream.ReadToEnd ()
            Expect.equal entryData data "the data match"
    }
]
