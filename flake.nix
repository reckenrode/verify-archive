{
  description = ''
    VerifyArchive compares Backblaze backup archives to the local filesystem and reports any
    discrepancies it finds.
  '';

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixos-21.05";
    utils.url = "github:gytis-ivaskevicius/flake-utils-plus/v1.3.0";
  
    rust-overlay.url = "github:oxalica/rust-overlay";
    rust-overlay.inputs.nixpkgs.follows = "nixpkgs";
  
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = inputs@{ self, nixpkgs, utils, rust-overlay, ... }:
    utils.lib.mkFlake {
      inherit self inputs;
      outputsBuilder = channels:
        let
          verify-archive = channels.nixpkgs.callPackage ./default.nix {};
          verify-archive-app = {
            type = "app";
            program = "${verify-archive}/bin/verify-archive";
          };
        in
        {
          packages.verify-archive = verify-archive;
          defaultPackage = verify-archive;

          apps.verify-archive = verify-archive-app;
          apps.defaultApp = verify-archive-app;

          devShell = import ./shell.nix channels.nixpkgs;
        };
      sharedOverlays = [ rust-overlay.overlay ];
    };
}
