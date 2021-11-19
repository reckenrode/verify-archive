{
  description = ''
    VerifyArchive compares Backblaze backup archives to the local filesystem and reports any
    discrepancies it finds.
  '';

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixos-unstable"; # Until Rust 1.56 is available in stable
    utils.url = "github:gytis-ivaskevicius/flake-utils-plus/v1.3.0";
  };

  outputs = inputs@{ self, nixpkgs, utils, ... }:
    utils.lib.mkFlake {
      inherit self inputs;
      outputsBuilder = channels: rec {
        packages.verify-archive = channels.nixpkgs.callPackage ./default.nix {};
	defaultPackage = packages.verify-archive;
      };
    };
}
