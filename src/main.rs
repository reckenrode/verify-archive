// SPDX-License-Identifier: GPL-3.0-only

use clap::Clap;

mod digest;
mod fs;
mod opts;
mod zip;

fn main() {
    let opts = opts::Opts::parse();
}
