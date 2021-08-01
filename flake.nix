{
  description = ''
    VerifyArchive compares Backblaze backup archives to the local filesystem and reports any
    discrepancies it finds.
  '';

  inputs.nixpkgs.url = "github:nixos/nixpkgs/nixos-21.05";

  inputs.rust-overlay.url = "github:oxalica/rust-overlay";
  inputs.rust-overlay.inputs.nixpkgs.follows = "nixpkgs";
  inputs.rust-overlay.inputs.flake-utils.follows = "flake-utils";

  inputs.flake-utils.url = "github:numtide/flake-utils";

  outputs = { nixpkgs, flake-utils, rust-overlay, ... }:
    let
      systems = builtins.attrNames nixpkgs.legacyPackages;
    in
    flake-utils.lib.eachSystem systems (system:
      let
	overlays = [ (import rust-overlay) ];
        pkgs = import nixpkgs { inherit system overlays; };
      in rec {
#        packages.verify-archive = pkgs.callPackage ./default.nix {};
#        defaultPackage = packages.verify-archive;
#
#        apps.verify-archive = {
#          type = "app";
#          program = "${packages.verify-archive}/bin/verify-archive";
#        };
#        apps.defaultApp = apps.verify-archive;

        devShell = import ./shell.nix { inherit pkgs; };
      }
    );
}
