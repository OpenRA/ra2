#!/bin/sh

set -e
command -v python >/dev/null 2>&1 || { echo >&2 "This script requires python."; exit 1; }
command -v mono >/dev/null 2>&1 || { echo >&2 "This script requires mono."; exit 1; }

TEMPLATE_LAUNCHER=$(python -c "import os; print(os.path.realpath('$0'))")
TEMPLATE_ROOT=$(dirname "${TEMPLATE_LAUNCHER}")

# Mono >= 5.2 on macOS default mono to 64bit. Force 32 bit until the engine is ready
if [ "$(uname -s)" = "Darwin" ] && command -v mono32 >/dev/null 2>&1; then
	alias mono=mono32
fi

# shellcheck source=mod.config
. "${TEMPLATE_ROOT}/mod.config"

if [ -f "${TEMPLATE_ROOT}/user.config" ]; then
	# shellcheck source=user.config
	. "${TEMPLATE_ROOT}/user.config"
fi

MOD_SEARCH_PATHS="${TEMPLATE_ROOT}/mods"
if [ "${INCLUDE_DEFAULT_MODS}" = "True" ]; then
	MOD_SEARCH_PATHS="${MOD_SEARCH_PATHS},./mods"
fi

cd "${TEMPLATE_ROOT}"
if [ ! -f "${ENGINE_DIRECTORY}/OpenRA.Game.exe" ]; then
	echo "Required engine files not found."
	echo "Run \`make\` in the mod directory to fetch and build the required files, then try again.";
	exit 1
fi

cd "${ENGINE_DIRECTORY}"
mono OpenRA.Game.exe Engine.LaunchPath="${TEMPLATE_LAUNCHER}" "Engine.ModSearchPaths=${MOD_SEARCH_PATHS}" Game.Mod="${MOD_ID}" "$@"
