{
  description = ''
    VerifyArchive compares Backblaze backup archives to the local filesystem and reports any
    discrepancies it finds.
  '';

  inputs.nixpkgs.url = "github:nixos/nixpkgs/nixos-22.11";

  outputs = { self, nixpkgs }:
    let
      inherit (nixpkgs) lib;

      forAllSystems = lib.genAttrs lib.systems.flakeExposed;
    in {
      devShells = forAllSystems (system:
        let
          pkgs = nixpkgs.legacyPackages.${system};
        in {
          default = pkgs.mkShell {
            packages = lib.attrValues {
              inherit (pkgs) cargo clippy rustfmt rustc rust-analyzer;
            };
          };
        });
    };
}
