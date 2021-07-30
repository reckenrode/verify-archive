// SPDX-License-Identifier: GPL-3.0-only

use clap::Clap;
use tokio::io;

use crate::commands::verify;

mod commands;
mod digest;
mod fs;
mod opts;
mod zip;

#[tokio::main(flavor = "multi_thread")]
async fn main() -> io::Result<()> {
    let opts = opts::Opts::parse();
    verify(opts).await
}
