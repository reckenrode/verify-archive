{ lib
, stdenv
, fetchFromGitHub
, rustPlatform
}:

rustPlatform.buildRustPackage rec {
  pname = "verify-archive";
  version = "0.5.1";

  src = fetchFromGitHub {
    owner = "reckenrode";
    repo = "verify-archive";
    rev = "v${version}";
    hash = "sha256-k/yB8BbeCb3AXEzcFICEndB4thYhiwZAx8fju4aFhrU=";
  };

  cargoHash = "sha256-CTVwhB4UXWY7YK61cu1VKJezlC4d7o1WgWpL1Xx31nI=";

  meta = let inherit (lib) licenses platforms; in {
    description = "Compare Backblaze backup archives to the local filesystem for discrepancies";
    homepage = "https://github.com/reckenrode/verify-archive";
    license = licenses.gpl3Only;
    platforms = platforms.unix ++ platforms.windows;
  };
}
