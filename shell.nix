{ pkgs }:

pkgs.mkShell {
    nativeBuildInputs = with pkgs; [
        dotnet-sdk_5
    ];
}
