#!/bin/bash

# Die on any error for Travis CI to automatically retry:
set -e

function download_url() {
  URL="$1"
  LOCAL_FILE="${1##*/}"
  if [ -x "$(type -p wget)" ]; then
    wget -O "${LOCAL_FILE}" "${URL}"
  elif [ -x "$(type -p curl)" ]; then
    curl -f -o "${LOCAL_FILE}" "${URL}"
  else
    echo "No supported download method found." 1>&2
    return 1
  fi
}

if [ "$(uname)" == "Darwin" ]; then
  DIR="$HOME/Library/Application Support/OpenRA/Content/ra2"
else
  DIR="$HOME/.openra/Content/ra2"
fi

#if the directory already exists then exit
[ ! -d "${DIR}" ] && mkdir -p "${DIR}" || {
  cd "${DIR}"
  #only exit if there's existing content
  if ls *.mix &> /dev/null; then
    exit 0
  fi
}
echo "Downloading RA2 mod content"
cd "${DIR}"
#download the file else exit non-zero
download_url "http://xwis.net/downloads/Red-Alert-2-Multiplayer.exe"
7z e Red-Alert-2-Multiplayer.exe
rm *.exe *.dll *.DLL *.wav *.mmp *.CFG *.WAR *.cache
