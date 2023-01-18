{ lib
, stdenv
, fetchFromGitHub
, rustPlatform
}:

rustPlatform.buildRustPackage rec {
  pname = "verify-archive";
  version = "0.6.0";

  src = fetchFromGitHub {
    owner = "reckenrode";
    repo = "verify-archive";
    rev = "v${version}";
    hash = "sha256-6Ctucvw/t75ugK0+K04/kBTecYY2lafWZfNTsnitRjU=";
  };

  cargoHash = "sha256-UYOxL0udArggNGPe60spR4CfsXrFEtOfpmq8MDw4gmk=";

  meta = let inherit (lib) licenses platforms; in {
    description = "Compare Backblaze backup archives to the local filesystem for discrepancies";
    homepage = "https://github.com/reckenrode/verify-archive";
    license = licenses.gpl3Only;
    platforms = platforms.unix ++ platforms.windows;
  };
}
