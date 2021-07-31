verify-archive compares Backblaze backup archives to the local filesystem and reports any
discrepancies it finds.

This program has gone through a few iterations in different languages.  This repository originally
belonged to the Ruby version.  That version is still available on the `ruby` branch.  An F# version
is also available on the `fsharp` branch.  The main branch is written in Rust.  It’s the fastest of
the three, so it’s the one I use.

# Building

Clone the repository and do `cargo build --release`.  The binary will be found at
`target/release/verify-archive` once the build completes.  If you don’t have a Rust environment,
install `cargo` with your package manager or use [rustup][1].  I don’t have a minimum-supported Rust
version, so assume I’m using the latest.  There is also [Nix][2] flake in the repository that will
set up a development environment.

# Running

`verify-archive` takes two parameters: the archive to check and (optionally) the root directory
to start checking.  It currently defaults to `/Volumes`.  Ideally it would adapt based on your
platform, but I don’t use Backblaze with Windows, so I don’t know how it organizes the archive.  The
`--root` parameter at least allows you to change it as necessary.

[1]: https://rustup.rs
[2]: https://nixos.org
