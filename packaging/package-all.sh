#!/bin/bash
set -e

if [ $# -eq "0" ]; then
	echo "Usage: `basename $0` version [outputdir]"
	exit 1
fi

TAG="$1"
if [ $# -eq "1" ]; then
	OUTPUTDIR=$(pwd)
else
	OUTPUTDIR=$2
fi

command -v python >/dev/null 2>&1 || { echo >&2 "The Red Alert 2 mod requires python."; exit 1; }
command -v make >/dev/null 2>&1 || { echo >&2 "The Red Alert 2 mod requires make."; exit 1; }
command -v curl >/dev/null 2>&1 || { echo >&2 "The Red Alert 2 mod requires curl."; exit 1; }
command -v makensis >/dev/null 2>&1 || { echo >&2 "The Red Alert 2 mod requires makensis."; exit 1; }

PACKAGING_DIR=$(python -c "import os; print(os.path.dirname(os.path.realpath('$0')))")

echo "Building Windows package"
${PACKAGING_DIR}/windows/buildpackage.sh "${TAG}" "${OUTPUTDIR}"
if [ $? -ne 0 ]; then
	echo "Windows package build failed."
fi

echo "Building macOS package"
${PACKAGING_DIR}/osx/buildpackage.sh "${TAG}" "${OUTPUTDIR}"
if [ $? -ne 0 ]; then
	echo "macOS package build failed."
fi

echo "Package build done."
