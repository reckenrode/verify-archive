{ lib
, stdenv
, fetchFromGitHub
, rustPlatform
}:

rustPlatform.buildRustPackage rec {
  pname = "verify-archive";
  version = "0.6.1";

  src = fetchFromGitHub {
    owner = "reckenrode";
    repo = "verify-archive";
    rev = "v${version}";
    hash = "sha256-XyKibEaDnA4UsGHz2Vq3Xvf6zi9MbXiHDjTkDAKCCjo=";
  };

  cargoHash = "sha256-yXXLMfTcC+IPwHgxLIU5uLdhTol+Lf1HhV7+9vBpnCY=";

  meta = let inherit (lib) licenses platforms; in {
    description = "Compare Backblaze backup archives to the local filesystem for discrepancies";
    homepage = "https://github.com/reckenrode/verify-archive";
    license = licenses.gpl3Only;
    platforms = platforms.unix ++ platforms.windows;
  };
}
