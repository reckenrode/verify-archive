// SPDX-License-Identifier: GPL-3.0-only

use std::io;

use clap::Clap;

use crate::commands::verify;

mod commands;
mod digest;
mod fs;
mod opts;
mod zip;

#[async_std::main]
async fn main() -> io::Result<()> {
    let opts = opts::Opts::parse();
    verify(opts).await
}
