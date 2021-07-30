// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Blake3.Tests

open Expecto

open System
open System.IO
open System.Text

open VerifyArchive

[<Tests>]
let tests = testList "VerifyArchive.Blake3" [
    testAsync "when `computeHashAsync` receives data, it computes the b3sum" {
        let expected = "6a953581d60dbebc9749b56d2383277fb02b58d260b4ccf6f119108fa0f1d4ef"
        let test_data = Encoding.UTF8.GetBytes "test data"

        use stream = MemoryStream ()
        stream.Write (ReadOnlySpan test_data)
        stream.Seek (0L, SeekOrigin.Begin) |> ignore

        let! result = Blake3.computeHashAsync stream

        Expect.equal (string result) expected "the hashes match"
    }
]
