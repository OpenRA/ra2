#!/bin/sh

set -e
command -v mono >/dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK requires mono."; exit 1; }
if command -v python3 >/dev/null 2>&1; then
	PYTHON="python3"
else
	command -v python >/dev/null 2>&1 || { echo >&2 "The OpenRA mod SDK requires python."; exit 1; }
	PYTHON="python"
fi

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

TEMPLATE_LAUNCHER=$(${PYTHON} -c "import os; print(os.path.realpath('$0'))")
TEMPLATE_ROOT=$(dirname "${TEMPLATE_LAUNCHER}")
MOD_SEARCH_PATHS="${TEMPLATE_ROOT}/mods,./mods"

# shellcheck source=mod.config
. "${TEMPLATE_ROOT}/mod.config"

if [ -f "${TEMPLATE_ROOT}/user.config" ]; then
	# shellcheck source=user.config
	. "${TEMPLATE_ROOT}/user.config"
fi

require_variables "MOD_ID" "ENGINE_VERSION" "ENGINE_DIRECTORY"

cd "${TEMPLATE_ROOT}"
if [ ! -f "${ENGINE_DIRECTORY}/bin/OpenRA.exe" ] || [ "$(cat "${ENGINE_DIRECTORY}/VERSION")" != "${ENGINE_VERSION}" ]; then
	echo "Required engine files not found."
	echo "Run \`make\` in the mod directory to fetch and build the required files, then try again.";
	exit 1
fi

cd "${ENGINE_DIRECTORY}"
mono --debug bin/OpenRA.exe Engine.EngineDir=".." Engine.LaunchPath="${TEMPLATE_LAUNCHER}" "Engine.ModSearchPaths=${MOD_SEARCH_PATHS}" Game.Mod="${MOD_ID}" "$@"
