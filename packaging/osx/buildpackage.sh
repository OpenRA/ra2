#!/bin/bash
# OpenRA packaging script for macOS
set -e

command -v make >/dev/null 2>&1 || { echo >&2 "The Red Alert 2 mod requires make."; exit 1; }
command -v python >/dev/null 2>&1 || { echo >&2 "The Red Alert 2 mod requires python."; exit 1; }
command -v curl >/dev/null 2>&1 || command -v wget > /dev/null 2>&1 || { echo >&2 "The Red Alert 2 mod requires curl or wget."; exit 1; }

require_variables() {
	missing=""
	for i in "$@"; do
		eval check="\$$i"
		[ -z "${check}" ] && missing="${missing}   ${i}\n"
	done
	if [ ! -z "${missing}" ]; then
		echo "Required mod.config variables are missing:\n${missing}Repair your mod.config (or user.config) and try again."
		exit 1
	fi
}

if [ $# -eq "0" ]; then
	echo "Usage: `basename $0` version [outputdir]"
	exit 1
fi

PACKAGING_DIR=$(python -c "import os; print(os.path.dirname(os.path.realpath('$0')))")
TEMPLATE_ROOT="${PACKAGING_DIR}/../../"

# shellcheck source=mod.config
. "${TEMPLATE_ROOT}/mod.config"

if [ -f "${TEMPLATE_ROOT}/user.config" ]; then
	# shellcheck source=user.config
	. "${TEMPLATE_ROOT}/user.config"
fi

require_variables "MOD_ID" "ENGINE_DIRECTORY" "PACKAGING_DISPLAY_NAME" "PACKAGING_INSTALLER_NAME" \
	"PACKAGING_OSX_LAUNCHER_TAG" "PACKAGING_OSX_LAUNCHER_SOURCE" "PACKAGING_OSX_LAUNCHER_TEMP_ARCHIVE_NAME" \
	"PACKAGING_FAQ_URL" "PACKAGING_OVERWRITE_MOD_VERSION"

TAG="$1"
if [ $# -eq "1" ]; then
	OUTPUTDIR=$(python -c "import os; print(os.path.realpath('.'))")
else
	OUTPUTDIR=$(python -c "import os; print(os.path.realpath('$2'))")
fi

BUILTDIR="${PACKAGING_DIR}/build"
PACKAGING_OSX_APP_NAME="OpenRA - ${PACKAGING_DISPLAY_NAME}.app"

# Set the working dir to the location of this script
cd "${PACKAGING_DIR}"

modify_plist() {
	sed "s|$1|$2|g" "$3" > "$3.tmp" && mv "$3.tmp" "$3"
}

echo "Building launcher"

if command -v curl >/dev/null 2>&1; then
	curl -s -L -o "${PACKAGING_OSX_LAUNCHER_TEMP_ARCHIVE_NAME}" -O "${PACKAGING_OSX_LAUNCHER_SOURCE}" || exit 3
else
	wget -cq "${PACKAGING_OSX_LAUNCHER_SOURCE}" -O "${PACKAGING_OSX_LAUNCHER_TEMP_ARCHIVE_NAME}" || exit 3
fi

unzip -qq -d "${BUILTDIR}" "${PACKAGING_OSX_LAUNCHER_TEMP_ARCHIVE_NAME}"
rm "${PACKAGING_OSX_LAUNCHER_TEMP_ARCHIVE_NAME}"

modify_plist "{DEV_VERSION}" "${TAG}" "${BUILTDIR}/OpenRA.app/Contents/Info.plist"
modify_plist "{FAQ_URL}" "${PACKAGING_FAQ_URL}" "${BUILTDIR}/OpenRA.app/Contents/Info.plist"
echo "Building core files"

pushd ${TEMPLATE_ROOT} > /dev/null

if [ ! -f "${ENGINE_DIRECTORY}/Makefile" ]; then
	echo "Required engine files not found."
	echo "Run \`make\` in the mod directory to fetch and build the required files, then try again.";
	exit 1
fi

if [ ! -d "${OUTPUTDIR}" ]; then
	echo "Output directory '${OUTPUTDIR}' does not exist.";
	exit 1
fi

MOD_VERSION=$(grep 'Version:' mods/${MOD_ID}/mod.yaml | awk '{print $2}')

if [ "${PACKAGING_OVERWRITE_MOD_VERSION}" == "True" ]; then
    make version VERSION="${TAG}"
else
	echo "Mod version ${MOD_VERSION} will remain unchanged.";
fi

pushd ${ENGINE_DIRECTORY} > /dev/null
make osx-dependencies
make core SDK="-sdk:4.5"
make install-engine gameinstalldir="/Contents/Resources/" DESTDIR="${BUILTDIR}/OpenRA.app"
make install-common-mod-files gameinstalldir="/Contents/Resources/" DESTDIR="${BUILTDIR}/OpenRA.app"

for f in ${PACKAGING_COPY_ENGINE_FILES}; do
  mkdir -p "${BUILTDIR}/OpenRA.app/Contents/Resources/$(dirname "${f}")"
  cp -r "${f}" "${BUILTDIR}/OpenRA.app/Contents/Resources/${f}"
done

popd > /dev/null
popd > /dev/null

# Add mod files
cp -Lr "${TEMPLATE_ROOT}/mods/"* "${BUILTDIR}/OpenRA.app/Contents/Resources/mods"
cp "mod.icns" "${BUILTDIR}/OpenRA.app/Contents/Resources/${MOD_ID}.icns"

pushd "${BUILTDIR}" > /dev/null
mv "OpenRA.app" "${PACKAGING_OSX_APP_NAME}"

# Copy macOS specific files
modify_plist "{MOD_ID}" "${MOD_ID}" "${PACKAGING_OSX_APP_NAME}/Contents/Info.plist"
modify_plist "{MOD_NAME}" "${PACKAGING_DISPLAY_NAME}" "${PACKAGING_OSX_APP_NAME}/Contents/Info.plist"
modify_plist "{JOIN_SERVER_URL_SCHEME}" "openra-${MOD_ID}-${TAG}" "${PACKAGING_OSX_APP_NAME}/Contents/Info.plist"

echo "Packaging zip archive"

zip "${PACKAGING_INSTALLER_NAME}-${TAG}-macOS.zip" -r -9 "${PACKAGING_OSX_APP_NAME}" --quiet
mv "${PACKAGING_INSTALLER_NAME}-${TAG}-macOS.zip" "${OUTPUTDIR}"
popd > /dev/null

# Clean up
rm -rf "${BUILTDIR}"
