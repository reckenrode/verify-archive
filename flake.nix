{
  description = ''
    VerifyArchive compares Backblaze backup archives to the local filesystem and reports any
    discrepancies it finds.
  '';

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixos-22.11";

    rust-overlay.url = "github:oxalica/rust-overlay";
    rust-overlay.inputs.nixpkgs.follows = "nixpkgs";
  };

  outputs = { self, nixpkgs, nixpkgs-unstable, utils, rust-overlay }:
    let
      inherit (nixpkgs) lib;
      forAllSystems = lib.genAttrs lib.systems.flakeExposed;
    in
    {
      devShells = forAllSystems (system:
        let
          pkgs = import nixpkgs {
            inherit system;
            overlays = [ rust-overlay.overlays.default ];
          };
        in {
          default = pkgs.mkShell {
            packages = [ pkgs.rust-bin.stable.latest.default ];
            RUST_ANALYZER_SERVER="${pkgs.rust-analyzer}/bin/rust-analyzer";
          };
        });
    };
}
