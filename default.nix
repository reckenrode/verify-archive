{ lib
, linkFarm
, stdenv
, fetchFromGitHub
, fetchurl
, makeWrapper
, dotnetCorePackages
, nugetUrl ? "https://www.nuget.org/api/v2/package"
}:

let
  loadJSON = (path: lib.trivial.pipe path [ builtins.readFile builtins.fromJSON ]);
  src-deps = loadJSON ./src-deps.json;
  test-deps = loadJSON ./test-deps.json;
  nugets = nugetSource {
    name = "verify-archive";
    sdk = dotnetCorePackages.sdk_5_0;
    source = src-deps;
  };
  nugetSource = { name, sdk, source }:
    let
      mkNuGetUrl = { package, version, hash }: {
        name = "${package}.${version}.nupkg";
        url = "${nugetUrl}/${package}/${version}";
        inherit hash;
      };
      nugets = lib.trivial.pipe source [
      	(lib.mapAttrsToList (package: v: mkNuGetUrl { inherit package; inherit (v) version hash; }))
        (map (attrs: { inherit (attrs) name; path = fetchurl attrs; }))
      ];
    in linkFarm "${name}-nugets" nugets;
in stdenv.mkDerivation rec {
  pname = "verify-archive";
  version = "0.1.1";

  src = fetchFromGitHub {
    owner = "reckenrode";
    repo = "VerifyArchive";
    rev = "v${version}";
    hash = "sha256-5HBHispTeQTg1QRi7kyWDMKqt2r8bpQOGhaGl5PS+7g=";
  };

  nativeBuildInputs = [
    dotnetCorePackages.sdk_5_0
    makeWrapper
  ];

  buildPhase = ''
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
    export HOME="$(mktemp -d)"
    dotnet build --nologo --source ${nugets} -c Release src/VerifyArchive/VerifyArchive.fsproj
  '';

  installPhase = ''
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
    dotnet publish --nologo --no-build -c Release -o "$out" src/VerifyArchive/VerifyArchive.fsproj
    makeWrapper ${dotnetCorePackages.net_5_0}/bin/dotnet "$out/bin/${pname}" \
      --add-flags $out/VerifyArchive.dll --argv0 verify-archive 
  '';

  meta = with lib; {
    description = "Finds discrepancies between Backblaze backups and the local filesystem";
    longDescription = ''
      VerifyArchive compares Backblaze backup archives to the local filesystem and reports any
      discrepancies it finds.
    '';
    homepage = "https://github.com/reckenrode/VerifyArchive";
    license = licenses.gpl3Only;
    platforms = dotnetCorePackages.net_5_0.meta.platforms;
  };
}
