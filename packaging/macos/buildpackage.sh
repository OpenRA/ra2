#!/bin/bash
# OpenRA Mod SDK packaging script for macOS
#
# The application bundles will be signed if the following environment variable is defined:
#   MACOS_DEVELOPER_IDENTITY: Certificate name, of the form `Developer\ ID\ Application:\ <name with escaped spaces>`
# If the identity is not already in the default keychain, specify the following environment variables to import it:
#   MACOS_DEVELOPER_CERTIFICATE_BASE64: base64 content of the exported .p12 developer ID certificate.
#                                       Generate using `base64 certificate.p12 | pbcopy`
#   MACOS_DEVELOPER_CERTIFICATE_PASSWORD: password to unlock the MACOS_DEVELOPER_CERTIFICATE_BASE64 certificate
#
# The applicaton bundles will be notarized if the following environment variables are defined:
#   MACOS_DEVELOPER_USERNAME: Email address for the developer account
#   MACOS_DEVELOPER_PASSWORD: App-specific password for the developer account
#
set -e

if [[ "$OSTYPE" != "darwin"* ]]; then
	echo >&2 "macOS packaging requires a macOS host"
	exit 1
fi

command -v make >/dev/null 2>&1 || { echo >&2 "The Red Alert 2 mod requires make."; exit 1; }
command -v python >/dev/null 2>&1 || { echo >&2 "The Red Alert 2 mod requires python."; exit 1; }

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
ARTWORK_DIR="${PACKAGING_DIR}/../artwork/"

# shellcheck source=mod.config
. "${TEMPLATE_ROOT}/mod.config"

if [ -f "${TEMPLATE_ROOT}/user.config" ]; then
	# shellcheck source=user.config
	. "${TEMPLATE_ROOT}/user.config"
fi

require_variables "MOD_ID" "ENGINE_DIRECTORY" "PACKAGING_DISPLAY_NAME" "PACKAGING_INSTALLER_NAME" \
	"PACKAGING_OSX_LAUNCHER_TAG" "PACKAGING_OSX_LAUNCHER_SOURCE" "PACKAGING_OSX_LAUNCHER_TEMP_ARCHIVE_NAME" \
	"PACKAGING_OSX_DMG_MOD_ICON_POSITION" "PACKAGING_OSX_DMG_APPLICATION_ICON_POSITION" "PACKAGING_OSX_DMG_HIDDEN_ICON_POSITION" \
	"PACKAGING_FAQ_URL" "PACKAGING_OVERWRITE_MOD_VERSION"

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
curl -s -L -o "${PACKAGING_OSX_LAUNCHER_TEMP_ARCHIVE_NAME}" -O "${PACKAGING_OSX_LAUNCHER_SOURCE}" || exit 3
unzip -qq -d "${BUILTDIR}" "${PACKAGING_OSX_LAUNCHER_TEMP_ARCHIVE_NAME}"
rm "${PACKAGING_OSX_LAUNCHER_TEMP_ARCHIVE_NAME}"

modify_plist "{DEV_VERSION}" "${TAG}" "${BUILTDIR}/OpenRA.app/Contents/Info.plist"
modify_plist "{FAQ_URL}" "${PACKAGING_FAQ_URL}" "${BUILTDIR}/OpenRA.app/Contents/Info.plist"

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

MOD_VERSION=$(grep 'Version:' mods/${MOD_ID}/mod.yaml | awk '{print $2}')

if [ "${PACKAGING_OVERWRITE_MOD_VERSION}" == "True" ]; then
	make version VERSION="${TAG}"
else
	echo "Mod version ${MOD_VERSION} will remain unchanged.";
fi

pushd "${ENGINE_DIRECTORY}" > /dev/null
echo "Building core files"

make clean
make osx-dependencies
make core
make version VERSION="${ENGINE_VERSION}"
make install-engine gameinstalldir="/Contents/Resources/" DESTDIR="${BUILTDIR}/OpenRA.app"
make install-common-mod-files gameinstalldir="/Contents/Resources/" DESTDIR="${BUILTDIR}/OpenRA.app"

for f in ${PACKAGING_COPY_ENGINE_FILES}; do
	mkdir -p "${BUILTDIR}/OpenRA.app/Contents/Resources/$(dirname "${f}")"
	cp -r "${f}" "${BUILTDIR}/OpenRA.app/Contents/Resources/${f}"
done

popd > /dev/null

echo "Building mod files"
make core
cp -LR mods/* "${BUILTDIR}/OpenRA.app/Contents/Resources/mods"

popd > /dev/null

pushd "${BUILTDIR}" > /dev/null
mv "OpenRA.app" "${PACKAGING_OSX_APP_NAME}"


# Assemble multi-resolution icon
mkdir mod.iconset
cp "${ARTWORK_DIR}/icon_16x16.png" "mod.iconset/icon_16x16.png"
cp "${ARTWORK_DIR}/icon_32x32.png" "mod.iconset/icon_16x16@2.png"
cp "${ARTWORK_DIR}/icon_32x32.png" "mod.iconset/icon_32x32.png"
cp "${ARTWORK_DIR}/icon_64x64.png" "mod.iconset/icon_32x32@2x.png"
cp "${ARTWORK_DIR}/icon_128x128.png" "mod.iconset/icon_128x128.png"
cp "${ARTWORK_DIR}/icon_256x256.png" "mod.iconset/icon_128x128@2x.png"
cp "${ARTWORK_DIR}/icon_256x256.png" "mod.iconset/icon_256x256.png"
cp "${ARTWORK_DIR}/icon_512x512.png" "mod.iconset/icon_256x256@2x.png"
iconutil --convert icns "mod.iconset" -o "${PACKAGING_OSX_APP_NAME}/Contents/Resources/${MOD_ID}.icns"
rm -rf mod.iconset

# Copy macOS specific files
modify_plist "{MOD_ID}" "${MOD_ID}" "${PACKAGING_OSX_APP_NAME}/Contents/Info.plist"
modify_plist "{MOD_NAME}" "${PACKAGING_DISPLAY_NAME}" "${PACKAGING_OSX_APP_NAME}/Contents/Info.plist"
modify_plist "{JOIN_SERVER_URL_SCHEME}" "openra-${MOD_ID}-${TAG}" "${PACKAGING_OSX_APP_NAME}/Contents/Info.plist"

# Sign binaries with developer certificate
if [ -n "${MACOS_DEVELOPER_IDENTITY}" ]; then
	codesign -s "${MACOS_DEVELOPER_IDENTITY}" --timestamp --options runtime -f --entitlements "${PACKAGING_DIR}/entitlements.plist" "${PACKAGING_OSX_APP_NAME}/Contents/Resources/"*.dylib
	codesign -s "${MACOS_DEVELOPER_IDENTITY}" --timestamp --options runtime -f --entitlements "${PACKAGING_DIR}/entitlements.plist" --deep "${PACKAGING_OSX_APP_NAME}"
fi

if [ -n "${MACOS_DEVELOPER_CERTIFICATE_BASE64}" ] && [ -n "${MACOS_DEVELOPER_CERTIFICATE_PASSWORD}" ] && [ -n "${MACOS_DEVELOPER_IDENTITY}" ]; then
	security delete-keychain build.keychain
fi

echo "Packaging disk image"
hdiutil create build.dmg -format UDRW -volname "${PACKAGING_DISPLAY_NAME}" -fs HFS+ -srcfolder "${BUILTDIR}"
DMG_DEVICE=$(hdiutil attach -readwrite -noverify -noautoopen "build.dmg" | egrep '^/dev/' | sed 1q | awk '{print $1}')
sleep 2

# Background image is created from source svg in artsrc repository
mkdir "/Volumes/${PACKAGING_DISPLAY_NAME}/.background/"
tiffutil -cathidpicheck "${ARTWORK_DIR}/macos-background.png" "${ARTWORK_DIR}/macos-background-2x.png" -out "/Volumes/${PACKAGING_DISPLAY_NAME}/.background/background.tiff"

cp "${BUILTDIR}/${PACKAGING_OSX_APP_NAME}/Contents/Resources/${MOD_ID}.icns" "/Volumes/${PACKAGING_DISPLAY_NAME}/.VolumeIcon.icns"

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
cp "${BUILTDIR}/${PACKAGING_OSX_APP_NAME}/Contents/Resources/${MOD_ID}.icns" "/Volumes/${PACKAGING_DISPLAY_NAME}/.VolumeIcon.icns"
SetFile -c icnC "/Volumes/${PACKAGING_DISPLAY_NAME}/.VolumeIcon.icns"
SetFile -a C "/Volumes/${PACKAGING_DISPLAY_NAME}"

chmod -Rf go-w "/Volumes/${PACKAGING_DISPLAY_NAME}"
sync
sync

hdiutil detach "${DMG_DEVICE}"

# Submit for notarization
if [ -n "${MACOS_DEVELOPER_USERNAME}" ] && [ -n "${MACOS_DEVELOPER_PASSWORD}" ]; then
	echo "Submitting disk image for notarization"

	# Reset xcode search path to fix xcrun not finding altool
	sudo xcode-select -r

	# Create a temporary read-only dmg for submission (notarization service rejects read/write images)
	hdiutil convert build.dmg -format UDZO -imagekey zlib-level=9 -ov -o notarization.dmg

	NOTARIZATION_UUID=$(xcrun altool --notarize-app --primary-bundle-id "net.openra.modsdk" -u "${MACOS_DEVELOPER_USERNAME}" -p "${MACOS_DEVELOPER_PASSWORD}" --file notarization.dmg 2>&1 | awk -F' = ' '/RequestUUID/ { print $2; exit }')
	if [ -z "${NOTARIZATION_UUID}" ]; then
		echo "Submission failed"
		exit 1
	fi

	echo "Submission UUID is ${NOTARIZATION_UUID}"
	rm notarization.dmg

	while :; do
		sleep 30
		NOTARIZATION_RESULT=$(xcrun altool --notarization-info "${NOTARIZATION_UUID}" -u "${MACOS_DEVELOPER_USERNAME}" -p "${MACOS_DEVELOPER_PASSWORD}" 2>&1 | awk -F': ' '/Status/ { print $2; exit }')
		echo "Submission status: ${NOTARIZATION_RESULT}"

		if [ "${NOTARIZATION_RESULT}" == "invalid" ]; then
			NOTARIZATION_LOG_URL=$(xcrun altool --notarization-info "${NOTARIZATION_UUID}" -u "${MACOS_DEVELOPER_USERNAME}" -p "${MACOS_DEVELOPER_PASSWORD}" 2>&1 | awk -F': ' '/LogFileURL/ { print $2; exit }')
			echo "Notarization failed with error:"
			curl -s "${NOTARIZATION_LOG_URL}" -w "\n"
			exit 1
		fi

		if [ "${NOTARIZATION_RESULT}" == "success" ]; then
			echo "Stapling notarization ticket"
			DMG_DEVICE=$(hdiutil attach -readwrite -noverify -noautoopen "build.dmg" | egrep '^/dev/' | sed 1q | awk '{print $1}')
			sleep 2

			xcrun stapler staple "/Volumes/${PACKAGING_DISPLAY_NAME}/${PACKAGING_OSX_APP_NAME}"

			sync
			sync

			hdiutil detach "${DMG_DEVICE}"
			break
		fi
	done
fi

hdiutil convert build.dmg -format UDZO -imagekey zlib-level=9 -ov -o "${OUTPUTDIR}/${PACKAGING_INSTALLER_NAME}-${TAG}.dmg"

popd > /dev/null

# Clean up
rm -rf "${BUILTDIR}"
