// SPDX-License-Identifier: GPL-3.0-only

use std::path::PathBuf;

use clap::Clap;

#[derive(Clap)]
#[clap(about, author, version)]
pub struct Opts {
    /// the archive to check
    #[clap(short, long)]
    pub input: PathBuf,
    /// the root path from which to check
    #[clap(short, long, default_value = "/Volumes")]
    pub root: PathBuf,
}
