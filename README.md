VerifyArchive compares Backblaze backup archives to the local filesystem and reports any
discrepancies it finds.

# Installation

No binaries are provided currently.  You’ll need to download the latest release and build it from
source.  You’ll also need the [.NET 5.0 SDK][1].  Once you’ve download and installed the SDK, run
`dotnet build -c Release` to build the release and `dotnet publish -c Release -o <destination>` to
install it somewhere on your computer.

Alternatively, if you use [Nix][2] with a version of `nix` that supports flakes, this repository
provides a flake.  You can run it by typing `nix run github:reckenrode/VerifyArchive`.  See my
[nixos-configs][3] for an example of adding it to a [home-manager][4] environment.

## Which branch to use

I make heavy use of topic branches when doing local development, but I push my changes to main.
While I try to make sure that all of my tests pass, if you don’t want to get possibly pre-release
code, you should track the release branch.  The release branch tracks main, but it is updated only
when a release is tagged.  This mainly applies if you are using the flake since that builds from
the git repository instead of one of the tagged releases.

# Running verify-archive

`verify-archive` takes two parameters: the archive to check and (optionally) the root directory
to start checking.  It currently defaults to `/Volumes`.  Ideally it would adapt based on your
platform, but I don’t use Backblaze with Windows, so I don’t know how it organizes the archive.  The
`--root` parameter at least allows you to change it as necessary.

# Developing verify-archive

Follow the installation instructs to build from source.  If you have [direnv][5] installed with Nix
support, it will set up a local environment for you.  Note that you need to have direnv set up with
flake support.

If the dependencies are updated (i.e., `packages.lock.json` changes), you will need to run
`update-depends.sh` to update `src-deps.json`.  This is used by the Nix build process to download
the correct dependencies from nuget.org as part of the Nix build process when installing from a
flake.  They can’t be fetched as part of `dotnet build` because Nix does not allow builders to
download from the network.

[1]: https://dotnet.microsoft.com/download/dotnet/5.0
[2]: https://nixos.org
[3]: https://github.com/reckenrode/nixos-configs
[4]: https://github.com/nix-community/home-manager
[5]: https://direnv.net
