{
  description = ''
    VerifyArchive compares Backblaze backup archives to the local filesystem and reports any
    discrepancies it finds.
  '';

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixos-21.11";
    nixpkgs-unstable.url = "github:nixos/nixpkgs/nixos-unstable";
    utils.url = "github:gytis-ivaskevicius/flake-utils-plus/v1.3.1";

    rust-overlay.url = "github:oxalica/rust-overlay";
    rust-overlay.inputs.nixpkgs.follows = "nixpkgs";
  };

  outputs = inputs@{ self, nixpkgs, utils, rust-overlay, ... }:
    utils.lib.mkFlake {
      inherit self inputs;
      outputsBuilder = channels: {
        devShell =
          let
            inherit (channels.nixpkgs) lib mkShell stdenv libiconv rust-bin;
          in
          mkShell {
            buildInputs = [
              rust-bin.stable.latest.default
            ] ++ lib.optionals stdenv.isDarwin [
              libiconv
            ];
	    RUST_ANALYZER_SERVER="${channels.nixpkgs-unstable.rust-analyzer}/bin/rust-analyzer";
          };
      };
    sharedOverlays = [ rust-overlay.overlay ];
  };
}
