// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Tests.StreamComparison

open Expecto
open System.IO

open VerifyArchive.Stream

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
        testAsync "when the other is empty, it matches" {
            let expected = true
            use stream = emptyStream ()
            use otherStream = MemoryStream ()
            let! result = stream |> matches otherStream
            Expect.equal result expected "streams match"
        }

        testAsync "when the other has data, it does not match" {
            let expected = false
            use stream = emptyStream ()
            use otherStream = mkStream "test data"
            let! result = stream |> matches otherStream
            Expect.equal result expected "streams do not match"
        }
    ]

    testList "non-empty stream" [
        testAsync "when the other is empty, it does not match" {
            let expected = false
            use stream = mkStream "test data"
            use otherStream = emptyStream ()
            let! result = stream |> matches otherStream
            Expect.equal result expected "streams do not match"
        }

        testAsync "when the other has matching data, it matches" {
            let expected = true
            use stream = mkStream "test data"
            use otherStream = mkStream "test data"
            let! result = stream |> matches otherStream
            Expect.equal result expected "streams match"
        }

        testAsync "when the other has different data, it does not match" {
            let expected = false
            use stream = mkStream "test data"
            use otherStream = mkStream "some other test data"
            let! result = stream |> matches otherStream
            Expect.equal result expected "streams do not match"
        }
    ]
]
