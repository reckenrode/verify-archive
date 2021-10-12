pkgs:

pkgs.mkShell {
  nativeBuildInputs = with pkgs; [
    rust-bin.stable.latest.default
    libiconv
  ];
}
