{ lib
, stdenv
, fetchFromGitHub
, rustPlatform
}:

rustPlatform.buildRustPackage rec {
  pname = "verify-archive";
  version = "0.5.0";

  src = fetchFromGitHub {
    owner = "reckenrode";
    repo = "verify-archive";
    rev = "v${version}";
    hash = "sha256-sEuxHrTof7JM3psnA6wRsQO+Sk3ZHvuFVPX0neFEeTc=";
  };

  cargoHash = "sha256-6yfrTFLDBiep5T2NvmUHBhbC7PlquW0Z2Bp4e83XgIE=";

  meta = let inherit (lib) licenses platforms; in {
    description = "Compare Backblaze backup archives to the local filesystem for discrepancies";
    homepage = "https://github.com/reckenrode/verify-archive";
    license = licenses.gpl3Only;
    platforms = platforms.unix ++ platforms.windows;
  };
}
