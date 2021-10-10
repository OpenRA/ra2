#!/bin/bash
# OpenRA Mod SDK packaging script for macOS
#
# The application bundles will be signed if the following environment variables are defined:
#   MACOS_DEVELOPER_IDENTITY: The alphanumeric identifier listed in the certificate name ("Developer ID Application: <your name> (<identity>)")
#                             or as Team ID in your Apple Developer account Membership Details.
# If the identity is not already in the default keychain, specify the following environment variables to import it:
#   MACOS_DEVELOPER_CERTIFICATE_BASE64: base64 content of the exported .p12 developer ID certificate.
#                                       Generate using `base64 certificate.p12 | pbcopy`
#   MACOS_DEVELOPER_CERTIFICATE_PASSWORD: password to unlock the MACOS_DEVELOPER_CERTIFICATE_BASE64 certificate
#
# The applicaton bundles will be notarized if the following environment variables are defined:
#   MACOS_DEVELOPER_USERNAME: Email address for the developer account
#   MACOS_DEVELOPER_PASSWORD: App-specific password for the developer account
#
set -o errexit -o pipefail || exit $?

if [[ "$OSTYPE" != "darwin"* ]]; then
	echo >&2 "macOS packaging requires a macOS host"
	exit 1
fi

command -v make >/dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK macOS packaging requires make."; exit 1; }
command -v python3 >/dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK macOS packaging requires python 3."; exit 1; }
command -v clang >/dev/null 2>&1 || { echo >&2 "macOS packaging requires clang."; exit 1; }

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

if [ $# -ne "2" ]; then
	echo "Usage: $(basename "$0") tag outputdir"
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
	"PACKAGING_OSX_DMG_MOD_ICON_POSITION" "PACKAGING_OSX_DMG_APPLICATION_ICON_POSITION" "PACKAGING_OSX_DMG_HIDDEN_ICON_POSITION" \
	"PACKAGING_FAQ_URL" "PACKAGING_OVERWRITE_MOD_VERSION"

if [ ! -f "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/Makefile" ]; then
	echo "Required engine files not found."
	echo "Run \`make\` in the mod directory to fetch and build the required files, then try again.";
	exit 1
fi

. "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/functions.sh"
. "${TEMPLATE_ROOT}/packaging/functions.sh"

# Import code signing certificate
if [ -n "${MACOS_DEVELOPER_CERTIFICATE_BASE64}" ] && [ -n "${MACOS_DEVELOPER_CERTIFICATE_PASSWORD}" ] && [ -n "${MACOS_DEVELOPER_IDENTITY}" ]; then
	echo "Importing signing certificate"
	echo "${MACOS_DEVELOPER_CERTIFICATE_BASE64}" | base64 --decode > build.p12
	security create-keychain -p build build.keychain
	security default-keychain -s build.keychain
	security unlock-keychain -p build build.keychain
	security import build.p12 -k build.keychain -P "${MACOS_DEVELOPER_CERTIFICATE_PASSWORD}" -T /usr/bin/codesign >/dev/null 2>&1
	security set-key-partition-list -S apple-tool:,apple: -s -k build build.keychain >/dev/null 2>&1
	rm -fr build.p12
fi

TAG="$1"
if [ $# -eq "1" ]; then
	OUTPUTDIR=$(python3 -c "import os; print(os.path.realpath('.'))")
else
	OUTPUTDIR=$(python3 -c "import os; print(os.path.realpath('$2'))")
fi

if [ ! -d "${OUTPUTDIR}" ]; then
	echo "Output directory '${OUTPUTDIR}' does not exist.";
	exit 1
fi

BUILTDIR="${PACKAGING_DIR}/build"
PACKAGING_OSX_APP_NAME="OpenRA - ${PACKAGING_DISPLAY_NAME}.app"

# Set the working dir to the location of this script
cd "${PACKAGING_DIR}"

modify_plist() {
	sed "s|$1|$2|g" "$3" > "$3.tmp" && mv "$3.tmp" "$3"
}

LAUNCHER_DIR="${BUILTDIR}/${PACKAGING_OSX_APP_NAME}"
LAUNCHER_CONTENTS_DIR="${LAUNCHER_DIR}/Contents"
LAUNCHER_ASSEMBLY_DIR="${LAUNCHER_CONTENTS_DIR}/MacOS"
LAUNCHER_RESOURCES_DIR="${LAUNCHER_CONTENTS_DIR}/Resources"

echo "Building launcher"

mkdir -p "${LAUNCHER_RESOURCES_DIR}"
mkdir -p "${LAUNCHER_ASSEMBLY_DIR}/x86_64"
mkdir -p "${LAUNCHER_ASSEMBLY_DIR}/arm64"
mkdir -p "${LAUNCHER_ASSEMBLY_DIR}/mono"
echo "APPL????" > "${LAUNCHER_CONTENTS_DIR}/PkgInfo"
cp "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/macos/Info.plist.in" "${LAUNCHER_CONTENTS_DIR}/Info.plist"

modify_plist "{DEV_VERSION}" "${TAG}" "${LAUNCHER_CONTENTS_DIR}/Info.plist"
modify_plist "{FAQ_URL}" "${PACKAGING_FAQ_URL}" "${LAUNCHER_CONTENTS_DIR}/Info.plist"
modify_plist "{MOD_ID}" "${MOD_ID}" "${LAUNCHER_CONTENTS_DIR}/Info.plist"
modify_plist "{MINIMUM_SYSTEM_VERSION}" "10.11" "${LAUNCHER_CONTENTS_DIR}/Info.plist"
modify_plist "{MOD_NAME}" "${PACKAGING_DISPLAY_NAME}" "${LAUNCHER_CONTENTS_DIR}/Info.plist"
modify_plist "{JOIN_SERVER_URL_SCHEME}" "openra-${MOD_ID}-${TAG}" "${LAUNCHER_CONTENTS_DIR}/Info.plist"
if [ -n "${DISCORD_APPID}" ]; then
	modify_plist "{DISCORD_URL_SCHEME}" "discord-${DISCORD_APPID}" "${LAUNCHER_CONTENTS_DIR}/Info.plist"
else
	modify_plist "<string>{DISCORD_URL_SCHEME}</string>" "" "${LAUNCHER_CONTENTS_DIR}/Info.plist"
fi

# Compile universal (x86_64 + arm64) Launcher and arch-specific apphosts
clang "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/macos/apphost.c" -o "${LAUNCHER_ASSEMBLY_DIR}/apphost-x86_64" -framework AppKit -target x86_64-apple-macos10.15
clang "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/macos/apphost.c" -o "${LAUNCHER_ASSEMBLY_DIR}/apphost-arm64" -framework AppKit -target arm64-apple-macos10.15
clang "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/macos/apphost-mono.c" -o "${LAUNCHER_ASSEMBLY_DIR}/apphost-mono" -framework AppKit -target x86_64-apple-macos10.11
clang "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/macos/checkmono.c" -o "${LAUNCHER_ASSEMBLY_DIR}/checkmono" -framework AppKit -target x86_64-apple-macos10.11
clang "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/macos/launcher.m" -o "${LAUNCHER_ASSEMBLY_DIR}/Launcher-x86_64" -framework AppKit -target x86_64-apple-macos10.11
clang "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/macos/launcher.m" -o "${LAUNCHER_ASSEMBLY_DIR}/Launcher-arm64" -framework AppKit -target arm64-apple-macos10.15
lipo -create -output "${LAUNCHER_ASSEMBLY_DIR}/Launcher" "${LAUNCHER_ASSEMBLY_DIR}/Launcher-x86_64" "${LAUNCHER_ASSEMBLY_DIR}/Launcher-arm64"
rm "${LAUNCHER_ASSEMBLY_DIR}/Launcher-x86_64" "${LAUNCHER_ASSEMBLY_DIR}/Launcher-arm64"

install_assemblies "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}" "${LAUNCHER_ASSEMBLY_DIR}/x86_64" "osx-x64" "net6" "True" "${PACKAGING_COPY_CNC_DLL}" "${PACKAGING_COPY_D2K_DLL}"
install_assemblies "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}" "${LAUNCHER_ASSEMBLY_DIR}/arm64" "osx-arm64" "net6" "True" "${PACKAGING_COPY_CNC_DLL}" "${PACKAGING_COPY_D2K_DLL}"
install_assemblies "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}" "${LAUNCHER_ASSEMBLY_DIR}/mono" "osx-x64" "mono" "True" "${PACKAGING_COPY_CNC_DLL}" "${PACKAGING_COPY_D2K_DLL}"
install_data "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}" "${LAUNCHER_RESOURCES_DIR}"

for f in ${PACKAGING_COPY_ENGINE_FILES}; do
	mkdir -p "${LAUNCHER_RESOURCES_DIR}/$(dirname "${f}")"
	cp -r "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/${f}" "${LAUNCHER_RESOURCES_DIR}/${f}"
done

echo "Building mod files"
install_mod_assemblies "${TEMPLATE_ROOT}" "${LAUNCHER_ASSEMBLY_DIR}/x86_64" "osx-x64" "net6" "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}"
install_mod_assemblies "${TEMPLATE_ROOT}" "${LAUNCHER_ASSEMBLY_DIR}/arm64" "osx-arm64" "net6" "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}"
install_mod_assemblies "${TEMPLATE_ROOT}" "${LAUNCHER_ASSEMBLY_DIR}/mono" "osx-x64" "mono" "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}"

cp -LR "${TEMPLATE_ROOT}mods/"* "${LAUNCHER_RESOURCES_DIR}/mods"

set_engine_version "${ENGINE_VERSION}" "${LAUNCHER_RESOURCES_DIR}"
if [ "${PACKAGING_OVERWRITE_MOD_VERSION}" == "True" ]; then
	set_mod_version "${TAG}" "${LAUNCHER_RESOURCES_DIR}/mods/${MOD_ID}/mod.yaml"
else
	MOD_VERSION=$(grep 'Version:' "${LAUNCHER_RESOURCES_DIR}/mods/${MOD_ID}/mod.yaml" | awk '{print $2}')
	echo "Mod version ${MOD_VERSION} will remain unchanged.";
fi

# Assemble multi-resolution icon
mkdir "${BUILTDIR}/mod.iconset"
cp "${ARTWORK_DIR}/icon_16x16.png" "${BUILTDIR}/mod.iconset/icon_16x16.png"
cp "${ARTWORK_DIR}/icon_32x32.png" "${BUILTDIR}/mod.iconset/icon_16x16@2.png"
cp "${ARTWORK_DIR}/icon_32x32.png" "${BUILTDIR}/mod.iconset/icon_32x32.png"
cp "${ARTWORK_DIR}/icon_64x64.png" "${BUILTDIR}/mod.iconset/icon_32x32@2x.png"
cp "${ARTWORK_DIR}/icon_128x128.png" "${BUILTDIR}/mod.iconset/icon_128x128.png"
cp "${ARTWORK_DIR}/icon_256x256.png" "${BUILTDIR}/mod.iconset/icon_128x128@2x.png"
cp "${ARTWORK_DIR}/icon_256x256.png" "${BUILTDIR}/mod.iconset/icon_256x256.png"
cp "${ARTWORK_DIR}/icon_512x512.png" "${BUILTDIR}/mod.iconset/icon_256x256@2x.png"
iconutil --convert icns "${BUILTDIR}/mod.iconset" -o "${LAUNCHER_RESOURCES_DIR}/${MOD_ID}.icns"
rm -rf "${BUILTDIR}/mod.iconset"

# Sign binaries with developer certificate
if [ -n "${MACOS_DEVELOPER_IDENTITY}" ]; then
	codesign -s "${MACOS_DEVELOPER_IDENTITY}" --timestamp --options runtime -f --entitlements "${TEMPLATE_ROOT}/${ENGINE_DIRECTORY}/packaging/macos/entitlements.plist" --deep "${LAUNCHER_DIR}"
fi

echo "Packaging disk image"
hdiutil create "build.dmg" -format UDRW -volname "${PACKAGING_DISPLAY_NAME}" -fs HFS+ -srcfolder "${BUILTDIR}"
DMG_DEVICE=$(hdiutil attach -readwrite -noverify -noautoopen "${PACKAGING_DIR}/build.dmg" | egrep '^/dev/' | sed 1q | awk '{print $1}')
sleep 2

# Background image is created from source svg in artsrc repository
mkdir "/Volumes/${PACKAGING_DISPLAY_NAME}/.background/"
tiffutil -cathidpicheck "${ARTWORK_DIR}/macos-background.png" "${ARTWORK_DIR}/macos-background-2x.png" -out "/Volumes/${PACKAGING_DISPLAY_NAME}/.background/background.tiff"

cp "${LAUNCHER_DIR}/Contents/Resources/${MOD_ID}.icns" "/Volumes/${PACKAGING_DISPLAY_NAME}/.VolumeIcon.icns"

echo '
   tell application "Finder"
     tell disk "'${PACKAGING_DISPLAY_NAME}'"
           open
           set current view of container window to icon view
           set toolbar visible of container window to false
           set statusbar visible of container window to false
           set the bounds of container window to {400, 100, 1000, 550}
           set theViewOptions to the icon view options of container window
           set arrangement of theViewOptions to not arranged
           set icon size of theViewOptions to 72
           set background picture of theViewOptions to file ".background:background.tiff"
           make new alias file at container window to POSIX file "/Applications" with properties {name:"Applications"}
           set position of item "'${PACKAGING_OSX_APP_NAME}'" of container window to {'${PACKAGING_OSX_DMG_MOD_ICON_POSITION}'}
           set position of item "Applications" of container window to {'${PACKAGING_OSX_DMG_APPLICATION_ICON_POSITION}'}
           set position of item ".background" of container window to {'${PACKAGING_OSX_DMG_HIDDEN_ICON_POSITION}'}
           set position of item ".fseventsd" of container window to {'${PACKAGING_OSX_DMG_HIDDEN_ICON_POSITION}'}
           set position of item ".VolumeIcon.icns" of container window to {'${PACKAGING_OSX_DMG_HIDDEN_ICON_POSITION}'}
           update without registering applications
           delay 5
           close
     end tell
   end tell
' | osascript

# HACK: Copy the volume icon again - something in the previous step seems to delete it...?
cp "${LAUNCHER_DIR}/Contents/Resources/${MOD_ID}.icns" "/Volumes/${PACKAGING_DISPLAY_NAME}/.VolumeIcon.icns"
SetFile -c icnC "/Volumes/${PACKAGING_DISPLAY_NAME}/.VolumeIcon.icns"
SetFile -a C "/Volumes/${PACKAGING_DISPLAY_NAME}"

chmod -Rf go-w "/Volumes/${PACKAGING_DISPLAY_NAME}"
sync
sync

hdiutil detach "${DMG_DEVICE}"
rm -rf "${BUILTDIR}"

if [ -n "${MACOS_DEVELOPER_CERTIFICATE_BASE64}" ] && [ -n "${MACOS_DEVELOPER_CERTIFICATE_PASSWORD}" ] && [ -n "${MACOS_DEVELOPER_IDENTITY}" ]; then
	security delete-keychain build.keychain
fi

if [ -n "${MACOS_DEVELOPER_USERNAME}" ] && [ -n "${MACOS_DEVELOPER_PASSWORD}" ] && [ -n "${MACOS_DEVELOPER_IDENTITY}" ]; then
	echo "Submitting build for notarization"

	# Reset xcode search path to fix xcrun not finding altool
	sudo xcode-select -r

	# Create a temporary read-only dmg for submission (notarization service rejects read/write images)
	hdiutil convert "build.dmg" -format ULFO -ov -o "build-notarization.dmg"

	xcrun notarytool submit "build-notarization.dmg" --wait --apple-id "${MACOS_DEVELOPER_USERNAME}" --password "${MACOS_DEVELOPER_PASSWORD}" --team-id "${MACOS_DEVELOPER_IDENTITY}"

	rm "build-notarization.dmg"

	echo "Stapling tickets"
	DMG_DEVICE=$(hdiutil attach -readwrite -noverify -noautoopen "build.dmg" | egrep '^/dev/' | sed 1q | awk '{print $1}')
	sleep 2

	xcrun stapler staple "/Volumes/${PACKAGING_DISPLAY_NAME}/${PACKAGING_OSX_APP_NAME}"

	sync
	sync

	hdiutil detach "${DMG_DEVICE}"
fi

hdiutil convert "build.dmg" -format ULFO -ov -o "${OUTPUTDIR}/${PACKAGING_INSTALLER_NAME}-${TAG}.dmg"
rm "build.dmg"