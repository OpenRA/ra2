#!/bin/bash
set -e
command -v makensis >/dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK Windows packaging requires makensis."; exit 1; }
command -v convert >/dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK Windows packaging requires ImageMagick."; exit 1; }
command -v python3 >/dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK Windows packaging requires python 3."; exit 1; }

require_variables() {
	missing=""
	for i in "$@"; do
		eval check="\$$i"
		[ -z "${check}" ] && missing="${missing}   ${i}\n"
	done
	if [ -n "${missing}" ]; then
		printf "Required mod.config variables are missing:\n%sRepair your mod.config (or user.config) and try again.\n" "${missing}"
		exit 1
	fi
}

if [ $# -eq "0" ]; then
	echo "Usage: $(basename "$0") version [outputdir]"
	exit 1
fi

PACKAGING_DIR=$(python3 -c "import os; print(os.path.dirname(os.path.realpath('$0')))")
TEMPLATE_ROOT="${PACKAGING_DIR}/../../"
ARTWORK_DIR="${PACKAGING_DIR}/../artwork/"

# shellcheck source=mod.config
. "${TEMPLATE_ROOT}/mod.config"

if [ -f "${TEMPLATE_ROOT}/user.config" ]; then
	# shellcheck source=user.config
	. "${TEMPLATE_ROOT}/user.config"
fi

require_variables "MOD_ID" "ENGINE_DIRECTORY" "PACKAGING_DISPLAY_NAME" "PACKAGING_INSTALLER_NAME" "PACKAGING_COPY_CNC_DLL" "PACKAGING_COPY_D2K_DLL" \
	"PACKAGING_WINDOWS_LAUNCHER_NAME" "PACKAGING_WINDOWS_REGISTRY_KEY" "PACKAGING_WINDOWS_INSTALL_DIR_NAME" \
	"PACKAGING_WINDOWS_LICENSE_FILE" "PACKAGING_FAQ_URL" "PACKAGING_WEBSITE_URL" "PACKAGING_AUTHORS" "PACKAGING_OVERWRITE_MOD_VERSION"

TAG="$1"
if [ $# -eq "1" ]; then
	OUTPUTDIR=$(python3 -c "import os; print(os.path.realpath('.'))")
else
	OUTPUTDIR=$(python3 -c "import os; print(os.path.realpath('$2'))")
fi

BUILTDIR="${PACKAGING_DIR}/build"

# Set the working dir to the location of this script
cd "${PACKAGING_DIR}"

if [ ! -f "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/Makefile" ]; then
	echo "Required engine files not found."
	echo "Run \`make\` in the mod directory to fetch and build the required files, then try again.";
	exit 1
fi

. "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/functions.sh"

if [ ! -d "${OUTPUTDIR}" ]; then
	echo "Output directory '${OUTPUTDIR}' does not exist.";
	exit 1
fi

function build_platform()
{
	PLATFORM="${1}"
	if [ "${PLATFORM}" = "x86" ]; then
		USE_PROGRAMFILES32="-DUSE_PROGRAMFILES32=true"
	else
		USE_PROGRAMFILES32=""
	fi

	if [ -n "${PACKAGING_DISCORD_APPID}" ]; then
		USE_DISCORDID="-DUSE_DISCORDID=${PACKAGING_DISCORD_APPID}"
	else
		USE_DISCORDID=""
	fi

	echo "Building core files (${PLATFORM})"
	install_assemblies_mono "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}" "${BUILTDIR}" "win-${PLATFORM}" "False" "${PACKAGING_COPY_CNC_DLL}" "${PACKAGING_COPY_D2K_DLL}"
	install_data "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}" "${BUILTDIR}"

	for f in ${PACKAGING_COPY_ENGINE_FILES}; do
		mkdir -p "${BUILTDIR}/$(dirname "${f}")"
		cp -r "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/${f}" "${BUILTDIR}/${f}"
	done

	echo "Building mod files (${PLATFORM})"
	pushd "${TEMPLATE_ROOT}" > /dev/null
	make all
	popd > /dev/null

	cp -Lr "${TEMPLATE_ROOT}/mods/"* "${BUILTDIR}/mods"

	for f in ${PACKAGING_COPY_MOD_BINARIES}; do
		mkdir -p "${BUILTDIR}/$(dirname "${f}")"
		cp "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/bin/${f}" "${BUILTDIR}/${f}"
	done

	set_engine_version "${ENGINE_VERSION}" "${BUILTDIR}"
	if [ "${PACKAGING_OVERWRITE_MOD_VERSION}" == "True" ]; then
		set_mod_version "${TAG}" "${BUILTDIR}/mods/${MOD_ID}/mod.yaml"
	else
		MOD_VERSION=$(grep 'Version:' "mods/${MOD_ID}/mod.yaml" | awk '{print $2}')
		echo "Mod version ${MOD_VERSION} will remain unchanged.";
	fi

	# Create multi-resolution icon
	convert "${ARTWORK_DIR}/icon_16x16.png" "${ARTWORK_DIR}/icon_24x24.png" "${ARTWORK_DIR}/icon_32x32.png" "${ARTWORK_DIR}/icon_48x48.png" "${ARTWORK_DIR}/icon_256x256.png" "${BUILTDIR}/${MOD_ID}.ico"

	echo "Compiling Windows launcher (${PLATFORM})"
	install_windows_launcher "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}" "${BUILTDIR}" "win-${PLATFORM}" "${MOD_ID}" "${PACKAGING_WINDOWS_LAUNCHER_NAME}"  "${PACKAGING_DISPLAY_NAME}" "${BUILTDIR}/${MOD_ID}.ico" "${PACKAGING_FAQ_URL}"

	echo "Building Windows setup.exe (${PLATFORM})"
	pushd "${PACKAGING_DIR}" > /dev/null
	makensis -V2 -DSRCDIR="${BUILTDIR}" -DTAG="${TAG}" -DMOD_ID="${MOD_ID}" -DPACKAGING_WINDOWS_INSTALL_DIR_NAME="${PACKAGING_WINDOWS_INSTALL_DIR_NAME}" -DPACKAGING_WINDOWS_LAUNCHER_NAME="${PACKAGING_WINDOWS_LAUNCHER_NAME}" -DPACKAGING_DISPLAY_NAME="${PACKAGING_DISPLAY_NAME}" -DPACKAGING_WEBSITE_URL="${PACKAGING_WEBSITE_URL}" -DPACKAGING_AUTHORS="${PACKAGING_AUTHORS}" -DPACKAGING_WINDOWS_REGISTRY_KEY="${PACKAGING_WINDOWS_REGISTRY_KEY}" -DPACKAGING_WINDOWS_LICENSE_FILE="${TEMPLATE_ROOT}/${PACKAGING_WINDOWS_LICENSE_FILE}" -DOUTFILE="${OUTPUTDIR}/${PACKAGING_INSTALLER_NAME}-${TAG}-${PLATFORM}.exe" ${USE_PROGRAMFILES32} ${USE_DISCORDID} buildpackage.nsi
	popd > /dev/null

	echo "Packaging zip archive (${PLATFORM})"
	pushd "${BUILTDIR}" > /dev/null
	zip "${OUTPUTDIR}/${PACKAGING_INSTALLER_NAME}-${TAG}-${PLATFORM}-winportable.zip" -r -9 ./* --quiet
	popd > /dev/null

	# Cleanup
	rm -rf "${BUILTDIR}"
}

build_platform "x86"
build_platform "x64"
