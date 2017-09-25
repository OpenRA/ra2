#!/bin/bash
set -e

command -v curl >/dev/null 2>&1 || { echo >&2 "Windows packaging requires curl."; exit 1; }
command -v makensis >/dev/null 2>&1 || { echo >&2 "Windows packaging requires makensis."; exit 1; }

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

if [ "${INCLUDE_DEFAULT_MODS}" = "True" ]; then
	echo "Cannot generate installers while INCLUDE_DEFAULT_MODS is enabled."
	echo "Make sure that this setting is disabled in both your mod.config and user.config."
	exit 1
fi

TAG="$1"
if [ $# -eq "1" ]; then
	OUTPUTDIR=$(python -c "import os; print(os.path.realpath('.'))")
else
	OUTPUTDIR=$(python -c "import os; print(os.path.realpath('$2'))")
fi

BUILTDIR="${PACKAGING_DIR}/build"

# Set the working dir to the location of this script
cd "${PACKAGING_DIR}"

LAUNCHER_LIBS="-r:System.dll -r:System.Drawing.dll -r:System.Windows.Forms.dll -r:${BUILTDIR}/OpenRA.Game.exe"

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

make version VERSION="${TAG}"

pushd ${ENGINE_DIRECTORY} > /dev/null
SRC_DIR="$(pwd)"
make windows-dependencies
make core SDK="-sdk:4.5"
make install-engine gameinstalldir="" DESTDIR="${BUILTDIR}"
make install-common-mod-files gameinstalldir="" DESTDIR="${BUILTDIR}"
popd > /dev/null
popd > /dev/null

# Add mod files
cp -r "${TEMPLATE_ROOT}/mods/"* "${BUILTDIR}/mods"
cp "mod.ico" "${BUILTDIR}/${MOD_ID}.ico"
cp "${SRC_DIR}/OpenRA.Game.exe.config" "${BUILTDIR}"

echo "Compiling Windows launcher"
sed "s|DISPLAY_NAME|${PACKAGING_DISPLAY_NAME}|" "${SRC_DIR}/packaging/windows/WindowsLauncher.cs.in" | sed "s|MOD_ID|${MOD_ID}|" | sed "s|FAQ_URL|${PACKAGING_FAQ_URL}|" > "${BUILTDIR}/WindowsLauncher.cs"
mcs -sdk:4.5 "${BUILTDIR}/WindowsLauncher.cs" -warn:4 -codepage:utf8 -warnaserror -out:"${BUILTDIR}/${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe" -t:winexe ${LAUNCHER_LIBS} -win32icon:"${BUILTDIR}/${MOD_ID}.ico"
rm "${BUILTDIR}/WindowsLauncher.cs"
mono "${SRC_DIR}/fixheader.exe" "${BUILTDIR}/${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe" > /dev/null

echo "Building Windows setup.exe"
pushd "${PACKAGING_DIR}" > /dev/null
makensis -V2 -DSRCDIR="${BUILTDIR}" -DDEPSDIR="${SRC_DIR}/thirdparty/download/windows" -DTAG="${TAG}" -DMOD_ID="${MOD_ID}" -DPACKAGING_WINDOWS_INSTALL_DIR_NAME="${PACKAGING_WINDOWS_INSTALL_DIR_NAME}" -DPACKAGING_WINDOWS_LAUNCHER_NAME="${PACKAGING_WINDOWS_LAUNCHER_NAME}" -DPACKAGING_DISPLAY_NAME="${PACKAGING_DISPLAY_NAME}" -DPACKAGING_WEBSITE_URL="${PACKAGING_WEBSITE_URL}" -DPACKAGING_AUTHORS="${PACKAGING_AUTHORS}" -DPACKAGING_WINDOWS_REGISTRY_KEY="${PACKAGING_WINDOWS_REGISTRY_KEY}" buildpackage.nsi
if [ $? -eq 0 ]; then
	mv OpenRA.Setup.exe "${OUTPUTDIR}/${PACKAGING_INSTALLER_NAME}-$TAG.exe"
fi
popd > /dev/null

echo "Packaging zip archive"
pushd "${BUILTDIR}" > /dev/null
find "${SRC_DIR}/thirdparty/download/windows/" -name '*.dll' -exec cp '{}' '.' ';'
zip "${PACKAGING_INSTALLER_NAME}-${TAG}-winportable" -r -9 * --quiet --symlinks
mv "${PACKAGING_INSTALLER_NAME}-${TAG}-winportable.zip" "${OUTPUTDIR}"
popd > /dev/null

# Cleanup
rm -rf "${BUILTDIR}"
