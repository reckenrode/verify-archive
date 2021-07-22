// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Tests.StreamComparison

open Expecto
open System.IO

open VerifyArchive.Stream
open VerifyArchive.Error

[<Tests>]
let tests = testList "VerifyArchive.Comparison" [
    let emptyStream () =
        let stream = MemoryStream ()
        stream

    let mkStream data =
        let stream = MemoryStream ()
        let writer = StreamWriter stream
        fprintf writer $"{data}"
        writer.Flush ()
        stream.Seek (0L, SeekOrigin.Begin) |> ignore
        stream

    testList "empty stream" [
        testTask "when the other is empty, it matches" {
            let expected = Ok ()
            use stream = emptyStream ()
            use otherStream = MemoryStream ()
            let! result = stream |> compare otherStream
            Expect.equal result expected "streams match"
        }

        testTask "when the other has data, it does not match" {
            let expected =
                Mismatch (
                    sourceHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                    backupHash = "916f0027a575074ce72a331777c3478d6513f786a591bd892da1a577bf2335f9"
                ) |> Error
            use stream = emptyStream ()
            use otherStream = mkStream "test data"
            let! result = stream |> compare otherStream
            Expect.equal result expected "streams do not match"
        }
    ]

    testList "non-empty stream" [
        testTask "when the other is empty, it does not match" {
            let expected =
                Mismatch (
                    sourceHash = "916f0027a575074ce72a331777c3478d6513f786a591bd892da1a577bf2335f9",
                    backupHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
                ) |> Error
            use stream = mkStream "test data"
            use otherStream = emptyStream ()
            let! result = stream |> compare otherStream
            Expect.equal result expected "streams do not match"
        }

        testTask "when the other has matching data, it matches" {
            let expected = Ok ()
            use stream = mkStream "test data"
            use otherStream = mkStream "test data"
            let! result = stream |> compare otherStream
            Expect.equal result expected "streams match"
        }

        testTask "when the other has different data, it does not match" {
            let expected =
                Mismatch (
                    sourceHash = "916f0027a575074ce72a331777c3478d6513f786a591bd892da1a577bf2335f9",
                    backupHash = "1653e0881271f9033226dd6fbdc71bbf4284c10dba7ddaeaf9e1b0e70718b83c"
                ) |> Error
            use stream = mkStream "test data"
            use otherStream = mkStream "some other test data"
            let! result = stream |> compare otherStream
            Expect.equal result expected "streams do not match"
        }
    ]
]
