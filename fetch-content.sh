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

	DOWNLOADERS=("wget" "curl")
	DOWNLOAD_COMMANDS=("wget URL" "curl -o \"Red-Alert-2-Multiplayer.exe\" URL")

	for INDEX in "${!DOWNLOADERS[@]}"; do
		DOWNLOADER=${DOWNLOADERS[$INDEX]}
		EXISTS=1
		(hash ${DOWNLOADER} 2> /dev/null) || EXISTS=0

		if [ $EXISTS -eq 1 ]; then
			COMMAND=${DOWNLOAD_COMMANDS[$INDEX]}
			break;
		fi
	done

	if [ -z "$COMMAND" ]; then
		echo "No supported download method found!"
		exit 1
	fi

	COMMAND="${COMMAND/URL/http://xwis.net/downloads/Red-Alert-2-Multiplayer.exe}"
	eval "${COMMAND}"

	7z e Red-Alert-2-Multiplayer.exe
	rm *.exe *.dll *.DLL *.wav *.mmp *.CFG *.WAR *.cache
	popd
fi
