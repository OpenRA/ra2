#!/bin/sh
# Helper functions for packaging and installing projects using the OpenRA Mod SDK

####
# This file must stay /bin/sh and POSIX compliant for macOS and BSD portability.
# Copy-paste the entire script into http://shellcheck.net to check.
####

# Compile and publish any mod assemblies to the target directory
# Arguments:
#   SRC_PATH: Path to the root SDK directory
#   DEST_PATH: Path to the root of the install destination (will be created if necessary)
#   TARGETPLATFORM: Platform type (win-x86, win-x64, osx-x64, osx-arm64, linux-x64, linux-arm64, unix-generic)
#   RUNTIME: Runtime type (net6, mono)
#   ENGINE_PATH: Path to the engine root directory
install_mod_assemblies() {
	SRC_PATH="${1}"
	DEST_PATH="${2}"
	TARGETPLATFORM="${3}"
	RUNTIME="${4}"
	ENGINE_PATH="${5}"

	ORIG_PWD=$(pwd)
	cd "${SRC_PATH}" || exit 1

	if [ "${RUNTIME}" = "mono" ]; then
		echo "Building assemblies"

		rm -rf "${ENGINE_PATH:?}/bin"

		find . -maxdepth 1 -name '*.sln' -exec msbuild -verbosity:m -nologo -t:Build -restore -p:Configuration=Release -p:TargetPlatform="${TARGETPLATFORM}" -p:Mono=true \;

		cd "${ORIG_PWD}" || exit 1
		for LIB in "${ENGINE_PATH}/bin/"*.dll "${ENGINE_PATH}/bin/"*.dll.config; do
			install -m644 "${LIB}" "${DEST_PATH}"
		done

		if [ "${TARGETPLATFORM}" = "linux-x64" ] || [ "${TARGETPLATFORM}" = "linux-arm64" ]; then
			for LIB in "${ENGINE_PATH}/bin/"*.so; do
				install -m755 "${LIB}" "${DEST_PATH}"
			done
		fi

		if [ "${TARGETPLATFORM}" = "osx-x64" ] || [ "${TARGETPLATFORM}" = "osx-arm64" ]; then
			for LIB in "${ENGINE_PATH}/bin/"*.dylib; do
				install -m755 "${LIB}" "${DEST_PATH}"
			done
		fi
	else
		find . -maxdepth 1 -name '*.sln' -exec dotnet publish -c Release -p:TargetPlatform="${TARGETPLATFORM}" -r "${TARGETPLATFORM}" -p:PublishDir="${DEST_PATH}" --self-contained true \;
		cd "${ORIG_PWD}" || exit 1
	fi
}
