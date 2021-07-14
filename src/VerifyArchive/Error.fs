// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Error

open System.IO

type Error =
    | Mismatch of path: string * sourceHash: string * backupHash: string
