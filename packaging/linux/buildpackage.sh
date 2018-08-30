#!/bin/bash
# OpenRA packaging script for Linux (AppImage)
set -e

command -v make >/dev/null 2>&1 || { echo >&2 "The OpenRA mod template requires make."; exit 1; }
command -v python >/dev/null 2>&1 || { echo >&2 "The OpenRA mod template requires python."; exit 1; }
command -v tar >/dev/null 2>&1 || { echo >&2 "The OpenRA mod template requires tar."; exit 1; }
command -v curl >/dev/null 2>&1 || command -v wget > /dev/null 2>&1 || { echo >&2 "The OpenRA mod template requires curl or wget."; exit 1; }

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
	"PACKAGING_APPIMAGE_DEPENDENCIES_TAG" "PACKAGING_APPIMAGE_DEPENDENCIES_SOURCE" "PACKAGING_APPIMAGE_DEPENDENCIES_TEMP_ARCHIVE_NAME" \
	"PACKAGING_FAQ_URL" "PACKAGING_OVERWRITE_MOD_VERSION"

TAG="$1"
if [ $# -eq "1" ]; then
	OUTPUTDIR=$(python -c "import os; print(os.path.realpath('.'))")
else
	OUTPUTDIR=$(python -c "import os; print(os.path.realpath('$2'))")
fi

BUILTDIR="${PACKAGING_DIR}/${PACKAGING_INSTALLER_NAME}.appdir"

# Set the working dir to the location of this script
cd "${PACKAGING_DIR}"

pushd "${TEMPLATE_ROOT}" > /dev/null

if [ ! -f "${ENGINE_DIRECTORY}/Makefile" ]; then
	echo "Required engine files not found."
	echo "Run \`make\` in the mod directory to fetch and build the required files, then try again.";
	exit 1
fi

if [ ! -d "${OUTPUTDIR}" ]; then
	echo "Output directory '${OUTPUTDIR}' does not exist.";
	exit 1
fi

echo "Building core files"

MOD_VERSION=$(grep 'Version:' mods/${MOD_ID}/mod.yaml | awk '{print $2}')

if [ "${PACKAGING_OVERWRITE_MOD_VERSION}" == "True" ]; then
    make version VERSION="${TAG}"
else
	echo "Mod version ${MOD_VERSION} will remain unchanged.";
fi

pushd "${ENGINE_DIRECTORY}" > /dev/null
make linux-dependencies
make core SDK="-sdk:4.5"
make install-engine prefix="usr" DESTDIR="${BUILTDIR}/"
make install-common-mod-files prefix="usr" DESTDIR="${BUILTDIR}/"

for f in ${PACKAGING_COPY_ENGINE_FILES}; do
  mkdir -p "${BUILTDIR}/usr/lib/openra/$(dirname "${f}")"
  cp -r "${f}" "${BUILTDIR}/usr/lib/openra/${f}"
done

popd > /dev/null
popd > /dev/null

# Add native libraries
echo "Downloading dependencies"
if command -v curl >/dev/null 2>&1; then
	curl -s -L -o "${PACKAGING_APPIMAGE_DEPENDENCIES_TEMP_ARCHIVE_NAME}" -O "${PACKAGING_APPIMAGE_DEPENDENCIES_SOURCE}" || exit 3
	curl -s -L -O https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage || exit 3
else
	wget -cq "${PACKAGING_APPIMAGE_DEPENDENCIES_SOURCE}" -O "${PACKAGING_APPIMAGE_DEPENDENCIES_TEMP_ARCHIVE_NAME}" || exit 3
	wget -cq https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage || exit 3
fi
tar xf "${PACKAGING_APPIMAGE_DEPENDENCIES_TEMP_ARCHIVE_NAME}"
chmod a+x appimagetool-x86_64.AppImage

echo "Building AppImage"

# Add mod files
cp -Lr "${TEMPLATE_ROOT}/mods/"* "${BUILTDIR}/usr/lib/openra/mods"

install -Dm 0755 libSDL2.so "${BUILTDIR}/usr/lib/openra/"
install -Dm 0644 include/SDL2-CS.dll.config "${BUILTDIR}/usr/lib/openra/"
install -Dm 0755 libopenal.so "${BUILTDIR}/usr/lib/openra/"
install -Dm 0644 include/OpenAL-CS.dll.config "${BUILTDIR}/usr/lib/openra/"
install -Dm 0755 liblua.so "${BUILTDIR}/usr/lib/openra/"
install -Dm 0644 include/Eluant.dll.config "${BUILTDIR}/usr/lib/openra/"

# Add launcher and icons
sed "s/{MODID}/${MOD_ID}/g" include/AppRun.in | sed "s/{MODNAME}/${PACKAGING_DISPLAY_NAME}/g" > AppRun.temp
install -m 0755 AppRun.temp "${BUILTDIR}/AppRun"

sed "s/{MODID}/${MOD_ID}/g" include/mod.desktop.in | sed "s/{MODNAME}/${PACKAGING_DISPLAY_NAME}/g" | sed "s/{TAG}/${TAG}/g" > temp.desktop
install -Dm 0755 temp.desktop "${BUILTDIR}/usr/share/applications/openra-${MOD_ID}.desktop"
install -m 0755 temp.desktop "${BUILTDIR}/openra-${MOD_ID}.desktop"

sed "s/{MODID}/${MOD_ID}/g" include/mod-mimeinfo.xml.in | sed "s/{TAG}/${TAG}/g" > temp.xml
install -Dm 0755 temp.xml "${BUILTDIR}/usr/share/mime/packages/openra-${MOD_ID}.xml"

if [ -f "${PACKAGING_DIR}/mod_scalable.svg" ]; then
  install -Dm644 "${PACKAGING_DIR}/mod_scalable.svg" "${BUILTDIR}/usr/share/icons/hicolor/scalable/apps/openra-${MOD_ID}.svg"
fi

for i in 16x16 32x32 48x48 64x64 128x128 256x256 512x512 1024x1024; do
  if [ -f "${PACKAGING_DIR}/mod_${i}.png" ]; then
    install -Dm644 "${PACKAGING_DIR}/mod_${i}.png" "${BUILTDIR}/usr/share/icons/hicolor/${i}/apps/openra-${MOD_ID}.png"
    install -m644 "${PACKAGING_DIR}/mod_${i}.png" "${BUILTDIR}/openra-${MOD_ID}.png"
  fi
done

install -d "${BUILTDIR}/usr/bin"

sed "s/{MODID}/${MOD_ID}/g" include/mod.in | sed "s/{TAG}/${TAG}/g" | sed "s/{MODNAME}/${PACKAGING_DISPLAY_NAME}/g" | sed "s/{MODINSTALLERNAME}/${PACKAGING_INSTALLER_NAME}/g" | sed "s|{MODFAQURL}|${PACKAGING_FAQ_URL}|g" > openra-mod.temp
install -m 0755 openra-mod.temp "${BUILTDIR}/usr/bin/openra-${MOD_ID}"

sed "s/{MODID}/${MOD_ID}/g" include/mod-server.in  > openra-mod-server.temp
install -m 0755 openra-mod-server.temp "${BUILTDIR}/usr/bin/openra-${MOD_ID}-server"

install -m 0755 include/gtk-dialog.py "${BUILTDIR}/usr/bin/gtk-dialog.py"

# travis-ci doesn't support mounting FUSE filesystems so extract and run the contents manually
./appimagetool-x86_64.AppImage --appimage-extract
ARCH=x86_64 ./squashfs-root/AppRun "${BUILTDIR}" "${OUTPUTDIR}/${PACKAGING_INSTALLER_NAME}-${TAG}.AppImage"

# Clean up
rm -rf openra-mod.temp openra-mod-server.temp temp.desktop temp.xml AppRun.temp libSDL2.so libopenal.so liblua.so appimagetool-x86_64.AppImage squashfs-root "${PACKAGING_APPIMAGE_DEPENDENCIES_TEMP_ARCHIVE_NAME}" "${BUILTDIR}"
