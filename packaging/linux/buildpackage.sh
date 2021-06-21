#!/bin/bash
# OpenRA packaging script for Linux (AppImage)
set -e

command -v make >/dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK Linux packaging requires make."; exit 1; }
command -v python3 >/dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK Linux packaging requires python 3."; exit 1; }
command -v tar >/dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK Linux packaging requires tar."; exit 1; }
command -v curl >/dev/null 2>&1 || command -v wget > /dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK Linux packaging requires curl or wget."; exit 1; }

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
	"PACKAGING_APPIMAGE_DEPENDENCIES_TAG" "PACKAGING_APPIMAGE_DEPENDENCIES_SOURCE" "PACKAGING_APPIMAGE_DEPENDENCIES_TEMP_ARCHIVE_NAME" \
	"PACKAGING_FAQ_URL" "PACKAGING_OVERWRITE_MOD_VERSION"

TAG="$1"
if [ $# -eq "1" ]; then
	OUTPUTDIR=$(python3 -c "import os; print(os.path.realpath('.'))")
else
	OUTPUTDIR=$(python3 -c "import os; print(os.path.realpath('$2'))")
fi

APPDIR="${PACKAGING_DIR}/${PACKAGING_INSTALLER_NAME}.appdir"

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

echo "Building core files"
install_assemblies_mono "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}" "${APPDIR}/usr/lib/openra" "linux-x64" "True" "${PACKAGING_COPY_CNC_DLL}" "${PACKAGING_COPY_D2K_DLL}"
install_data "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}" "${APPDIR}/usr/lib/openra"

for f in ${PACKAGING_COPY_ENGINE_FILES}; do
	mkdir -p "${APPDIR}/usr/lib/openra/$(dirname "${f}")"
	cp -r "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/${f}" "${APPDIR}/usr/lib/openra/${f}"
done

echo "Building mod files"
pushd "${TEMPLATE_ROOT}" > /dev/null
make all
popd > /dev/null

cp -Lr "${TEMPLATE_ROOT}/mods/"* "${APPDIR}/usr/lib/openra/mods"

for f in ${PACKAGING_COPY_MOD_BINARIES}; do
	mkdir -p "${APPDIR}/usr/lib/openra/$(dirname "${f}")"
	cp "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/bin/${f}" "${APPDIR}/usr/lib/openra/${f}"
done

set_engine_version "${ENGINE_VERSION}" "${APPDIR}/usr/lib/openra"
if [ "${PACKAGING_OVERWRITE_MOD_VERSION}" == "True" ]; then
	set_mod_version "${TAG}" "${APPDIR}/usr/lib/openra/mods/${MOD_ID}/mod.yaml"
else
	MOD_VERSION=$(grep 'Version:' "${APPDIR}/usr/lib/openra/mods/${MOD_ID}/mod.yaml" | awk '{print $2}')
	echo "Mod version ${MOD_VERSION} will remain unchanged.";
fi

# Add native libraries
echo "Downloading dependencies"
if command -v curl >/dev/null 2>&1; then
	curl -s -L -o "${PACKAGING_APPIMAGE_DEPENDENCIES_TEMP_ARCHIVE_NAME}" -O "${PACKAGING_APPIMAGE_DEPENDENCIES_SOURCE}" || exit 3
	curl -s -L -O https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage || exit 3
else
	wget -cq "${PACKAGING_APPIMAGE_DEPENDENCIES_SOURCE}" -O "${PACKAGING_APPIMAGE_DEPENDENCIES_TEMP_ARCHIVE_NAME}" || exit 3
	wget -cq https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage || exit 3
fi

echo "Building AppImage"

tar xf "${PACKAGING_APPIMAGE_DEPENDENCIES_TEMP_ARCHIVE_NAME}" -C "${APPDIR}"
chmod 0755 "${APPDIR}/usr/bin/mono"
chmod 0644 "${APPDIR}/etc/mono/config"
chmod 0644 "${APPDIR}/etc/mono/4.5/machine.config"
chmod 0644 "${APPDIR}/usr/lib/mono/4.5/Facades/"*.dll
chmod 0644 "${APPDIR}/usr/lib/mono/4.5/"*.dll "${APPDIR}/usr/lib/mono/4.5/"*.exe
chmod 0755 "${APPDIR}/usr/lib/"*.so

rm -rf "${PACKAGING_APPIMAGE_DEPENDENCIES_SOURCE}"

# Add launcher and icons
sed "s/{MODID}/${MOD_ID}/g" "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/linux/AppRun.in" | sed "s/{MODNAME}/${PACKAGING_DISPLAY_NAME}/g" > "${APPDIR}/AppRun"
chmod 0755 "${APPDIR}/AppRun"

if [ -n "${PACKAGING_DISCORD_APPID}" ]; then
	sed "s/{DISCORDAPPID}/${PACKAGING_DISCORD_APPID}/g" "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/linux/openra.desktop.discord.in" > temp.desktop.in
	sed "s/{DISCORDAPPID}/${PACKAGING_DISCORD_APPID}/g" "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/linux/openra-mimeinfo.xml.discord.in" > temp.xml.in
else
	cp "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/linux/openra.desktop.in" temp.desktop.in
	cp "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/linux/openra-mimeinfo.xml.in" temp.xml.in
fi

mkdir -p "${APPDIR}/usr/share/applications"
chmod 0755 temp.desktop.in
sed "s/{MODID}/${MOD_ID}/g" temp.desktop.in | sed "s/{MODNAME}/${PACKAGING_DISPLAY_NAME}/g" | sed "s/{TAG}/${TAG}/g" > "${APPDIR}/usr/share/applications/openra-${MOD_ID}.desktop"
cp "${APPDIR}/usr/share/applications/openra-${MOD_ID}.desktop" "${APPDIR}/openra-${MOD_ID}.desktop"
rm temp.desktop.in

mkdir -p "${APPDIR}/usr/share/mime/packages"
chmod 0644 temp.xml.in
sed "s/{MODID}/${MOD_ID}/g" temp.xml.in | sed "s/{TAG}/${TAG}/g" > "${APPDIR}/usr/share/mime/packages/openra-${MOD_ID}.xml"
rm temp.xml.in

if [ -f "${ARTWORK_DIR}/icon_scalable.svg" ]; then
	install -Dm644 "${ARTWORK_DIR}/icon_scalable.svg" "${APPDIR}/usr/share/icons/hicolor/scalable/apps/openra-${MOD_ID}.svg"
fi

for i in 16x16 32x32 48x48 64x64 128x128 256x256 512x512 1024x1024; do
	if [ -f "${ARTWORK_DIR}/icon_${i}.png" ]; then
		install -Dm644 "${ARTWORK_DIR}/icon_${i}.png" "${APPDIR}/usr/share/icons/hicolor/${i}/apps/openra-${MOD_ID}.png"
		install -m644 "${ARTWORK_DIR}/icon_${i}.png" "${APPDIR}/openra-${MOD_ID}.png"
	fi
done

install -d "${APPDIR}/usr/bin"

sed "s/{MODID}/${MOD_ID}/g" "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/linux/openra.appimage.in" | sed "s/{TAG}/${TAG}/g" | sed "s/{MODNAME}/${PACKAGING_DISPLAY_NAME}/g" | sed "s/{MODINSTALLERNAME}/${PACKAGING_INSTALLER_NAME}/g" | sed "s|{MODFAQURL}|${PACKAGING_FAQ_URL}|g" > "${APPDIR}/usr/bin/openra-${MOD_ID}"
chmod 0755 "${APPDIR}/usr/bin/openra-${MOD_ID}"

sed "s/{MODID}/${MOD_ID}/g" "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/linux/openra-server.appimage.in" > "${APPDIR}/usr/bin/openra-${MOD_ID}-server"
chmod 0755 "${APPDIR}/usr/bin/openra-${MOD_ID}-server"

sed "s/{MODID}/${MOD_ID}/g" "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/linux/openra-utility.appimage.in" > "${APPDIR}/usr/bin/openra-${MOD_ID}-utility"
chmod 0755 "${APPDIR}/usr/bin/openra-${MOD_ID}-utility"

install -m 0755 "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/linux/gtk-dialog.py" "${APPDIR}/usr/bin/gtk-dialog.py"
install -m 0755 "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/linux/restore-environment.sh" "${APPDIR}/usr/bin/restore-environment.sh"

chmod a+x appimagetool-x86_64.AppImage
ARCH=x86_64 ./appimagetool-x86_64.AppImage "${APPDIR}" "${OUTPUTDIR}/${PACKAGING_INSTALLER_NAME}-${TAG}-x86_64.AppImage"

# Clean up
rm -rf appimagetool-x86_64.AppImage "${PACKAGING_APPIMAGE_DEPENDENCIES_TEMP_ARCHIVE_NAME}" "${APPDIR}"
