#!/bin/bash
set -e
command -v makensis >/dev/null 2>&1 || { echo >&2 "Windows packaging requires makensis."; exit 1; }

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
	"PACKAGING_WINDOWS_LAUNCHER_NAME" "PACKAGING_WINDOWS_REGISTRY_KEY" "PACKAGING_WINDOWS_INSTALL_DIR_NAME" \
	"PACKAGING_WINDOWS_LICENSE_FILE" "PACKAGING_FAQ_URL" "PACKAGING_WEBSITE_URL" "PACKAGING_AUTHORS" "PACKAGING_OVERWRITE_MOD_VERSION"

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

popd > /dev/null

function build_platform()
{
	if [ "$1" = "x86" ]; then
		IS_WIN32="WIN32=true"
	else
		IS_WIN32="WIN32=false"
	fi

	pushd ${TEMPLATE_ROOT} > /dev/null

	echo "Building core files ($1)"
	pushd ${ENGINE_DIRECTORY} > /dev/null

	SRC_DIR="$(pwd)"

	make clean
	make windows-dependencies "${IS_WIN32}"
	make core "${IS_WIN32}"
	make version VERSION="${ENGINE_VERSION}"
	make install-engine gameinstalldir="" DESTDIR="${BUILTDIR}"
	make install-common-mod-files gameinstalldir="" DESTDIR="${BUILTDIR}"

	for f in ${PACKAGING_COPY_ENGINE_FILES}; do
		mkdir -p "${BUILTDIR}/$(dirname "${f}")"
		cp -r "${f}" "${BUILTDIR}/${f}"
	done

	popd > /dev/null

	echo "Building mod files ($1)"
	make core

	cp -Lr mods/* "${BUILTDIR}/mods"

	popd > /dev/null

	cp "mod.ico" "${BUILTDIR}/${MOD_ID}.ico"
	cp "${SRC_DIR}/OpenRA.Game.exe.config" "${BUILTDIR}"

	# We need to set the loadFromRemoteSources flag for the launcher, but only for the "portable" zip package.
	# Windows automatically un-trusts executables that are extracted from a downloaded zip file
	cp "${SRC_DIR}/OpenRA.Game.exe.config" "${BUILTDIR}/${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe.config"

 	echo "Compiling Windows launcher ($1)"
	sed "s|DISPLAY_NAME|${PACKAGING_DISPLAY_NAME}|" "${SRC_DIR}/packaging/windows/WindowsLauncher.cs.in" | sed "s|MOD_ID|${MOD_ID}|" | sed "s|FAQ_URL|${PACKAGING_FAQ_URL}|" > "${BUILTDIR}/WindowsLauncher.cs"
	csc "${BUILTDIR}/WindowsLauncher.cs" -nologo -warn:4 -warnaserror -platform:"$1" -out:"${BUILTDIR}/${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe" -t:winexe ${LAUNCHER_LIBS} -win32icon:"${BUILTDIR}/${MOD_ID}.ico"
	rm "${BUILTDIR}/WindowsLauncher.cs"
	mono "${SRC_DIR}/OpenRA.PostProcess.exe" "${BUILTDIR}/${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe" -LAA > /dev/null

 	echo "Building Windows setup.exe ($1)"
	pushd "${PACKAGING_DIR}" > /dev/null
	makensis -V2 -DSRCDIR="${BUILTDIR}" -DDEPSDIR="${SRC_DIR}/thirdparty/download/windows" -DTAG="${TAG}" -DMOD_ID="${MOD_ID}" -DPACKAGING_WINDOWS_INSTALL_DIR_NAME="${PACKAGING_WINDOWS_INSTALL_DIR_NAME}" -DPACKAGING_WINDOWS_LAUNCHER_NAME="${PACKAGING_WINDOWS_LAUNCHER_NAME}" -DPACKAGING_DISPLAY_NAME="${PACKAGING_DISPLAY_NAME}" -DPACKAGING_WEBSITE_URL="${PACKAGING_WEBSITE_URL}" -DPACKAGING_AUTHORS="${PACKAGING_AUTHORS}" -DPACKAGING_WINDOWS_REGISTRY_KEY="${PACKAGING_WINDOWS_REGISTRY_KEY}" -DPACKAGING_WINDOWS_LICENSE_FILE="${TEMPLATE_ROOT}/${PACKAGING_WINDOWS_LICENSE_FILE}" buildpackage.nsi
	if [ $? -eq 0 ]; then
		mv OpenRA.Setup.exe "${OUTPUTDIR}/${PACKAGING_INSTALLER_NAME}-${TAG}-${1}.exe"
	fi
	popd > /dev/null

	echo "Packaging zip archive ($1)"
	pushd "${BUILTDIR}" > /dev/null
	find "${SRC_DIR}/thirdparty/download/windows/" -name '*.dll' -exec cp '{}' '.' ';'
	zip "${PACKAGING_INSTALLER_NAME}-${TAG}-${1}-winportable.zip" -r -9 * --quiet
	mv "${PACKAGING_INSTALLER_NAME}-${TAG}-${1}-winportable.zip" "${OUTPUTDIR}"
	popd > /dev/null

	# Cleanup
	rm -rf "${BUILTDIR}"
}

build_platform "x86"
build_platform "x64"
