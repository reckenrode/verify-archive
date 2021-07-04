{
  description = ''
    VerifyArchive compares Backblaze backup archives to the local filesystem and reports any
    discrepancies it finds.
  '';

  inputs.nixpkgs.url = "github:nixos/nixpkgs/nixos-21.05";
  inputs.flake-utils.url = "github:numtide/flake-utils";

  outputs = { nixpkgs, flake-utils, ... }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = nixpkgs.legacyPackages.${system};
      in rec {
        # packages.verify-archive = import ./default.nix {};
        # defaultPackage = packages.verify-archive;

        # apps.verify-archive = {
        #   type = "app";
        #   program = "${packages.verify-archive}/bin/verify-archive";
        # };
        # apps.defaultApp = apps.verify-archive;

        devShell = import ./shell.nix { inherit pkgs; };
      }
    );
}
