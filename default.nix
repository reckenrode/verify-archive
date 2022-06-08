{ lib
, stdenv
, fetchFromGitHub
, rustPlatform
}:

rustPlatform.buildRustPackage rec {
  pname = "verify-archive";
  version = "0.5.2";

  src = fetchFromGitHub {
    owner = "reckenrode";
    repo = "verify-archive";
    rev = "v${version}";
    hash = "sha256-gbDs/ukJyz4TPZ4RMW966s38HeTnTUDpgu9rhQ5jSOo=";
  };

  cargoHash = "sha256-rscFATm0Ela4M/F/TRqOKL6t6xOMAhS7vvM0eFtRyZE=";

  meta = let inherit (lib) licenses platforms; in {
    description = "Compare Backblaze backup archives to the local filesystem for discrepancies";
    homepage = "https://github.com/reckenrode/verify-archive";
    license = licenses.gpl3Only;
    platforms = platforms.unix ++ platforms.windows;
  };
}
