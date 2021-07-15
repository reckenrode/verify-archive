// SPDX-License-Identifier: GPL-3.0-only

module VerifyArchive.Error

type Error =
    | Mismatch of sourceHash: string * backupHash: string
    | Missing

type ArchiveError =
    | InvalidArchive
