// SPDX-License-Identifier: GPL-3.0-only

use std::io;

use clap::Clap;

use crate::commands::verify;

mod commands;
mod digest;
mod fs;
mod opts;
mod zip;

fn main() -> io::Result<()> {
    let opts = opts::Opts::parse();
    verify(opts)
}
