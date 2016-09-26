#!/bin/bash

# Die on any error for Travis CI to automatically retry:
set -e

if [ "$(uname)" == "Darwin" ]; then
	DIR="$HOME/Library/Application Support/OpenRA/Content/ra2/"
else
	DIR="$HOME/.openra/Content/ra2/"
fi

if [ ! -e "$DIR" ]; then
	echo "Downloading RA2 mod content"

	mkdir -p "$DIR"
	pushd "$DIR"

	wget http://xwis.net/downloads/Red-Alert-2-Multiplayer.exe
	7z e Red-Alert-2-Multiplayer.exe
	rm *.exe *.dll *.DLL *.wav *.mmp *.CFG *.WAR *.cache
	popd
fi
