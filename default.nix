{ lib
, stdenv
, rust-bin
, makeRustPlatform
}:

let
  rustPlatform = makeRustPlatform {
      cargo = rust-bin.stable.latest.cargo;
      rustc = rust-bin.stable.latest.minimal;
  };
  info = lib.importTOML ./Cargo.toml;
in
rustPlatform.buildRustPackage {
  pname = "verify-archive";
  version = info.package.version;

  src = ./.;

  cargoHash = "sha256-UPzGjn00vq9tmNIKcd4529lGKCel5XTr7wt0sltdyBY=";

  meta = let inherit (lib) licenses platforms; in {
    description = info.package.description;
    homepage = info.package.repository;
    license = licenses.gpl3Only;
    platforms = platforms.unix ++ platforms.windows;
  };
}
