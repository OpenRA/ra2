#!/bin/sh
# Usage:
#  $ ./launch-dedicated.sh # Launch a dedicated server with default settings
#  $ Mod="<mod id>" ./launch-dedicated.sh # Launch a dedicated server with default settings but override the Mod
#  Read the file to see which settings you can override

set -e
command -v python >/dev/null 2>&1 || { echo >&2 "This script requires python."; exit 1; }
command -v mono >/dev/null 2>&1 || { echo >&2 "This script requires mono."; exit 1; }

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

TEMPLATE_LAUNCHER=$(python -c "import os; print(os.path.realpath('$0'))")
TEMPLATE_ROOT=$(dirname "${TEMPLATE_LAUNCHER}")
MOD_SEARCH_PATHS="${TEMPLATE_ROOT}/mods,./mods"

# shellcheck source=mod.config
. "${TEMPLATE_ROOT}/mod.config"

if [ -f "${TEMPLATE_ROOT}/user.config" ]; then
	# shellcheck source=user.config
	. "${TEMPLATE_ROOT}/user.config"
fi

require_variables "MOD_ID" "ENGINE_VERSION" "ENGINE_DIRECTORY"

NAME="${Name:-"Dedicated Server"}"
LAUNCH_MOD="${Mod:-"${MOD_ID}"}"
LISTEN_PORT="${ListenPort:-"1234"}"
ADVERTISE_ONLINE="${AdvertiseOnline:-"True"}"
ENABLE_SINGLE_PLAYER="${EnableSingleplayer:-"False"}"
PASSWORD="${Password:-""}"

cd "${TEMPLATE_ROOT}"
if [ ! -f "${ENGINE_DIRECTORY}/OpenRA.Game.exe" ] || [ "$(cat "${ENGINE_DIRECTORY}/VERSION")" != "${ENGINE_VERSION}" ]; then
	echo "Required engine files not found."
	echo "Run \`make\` in the mod directory to fetch and build the required files, then try again.";
	exit 1
fi

cd "${ENGINE_DIRECTORY}"

while true; do
     MOD_SEARCH_PATHS="${MOD_SEARCH_PATHS}" mono --debug OpenRA.Server.exe Game.Mod="${LAUNCH_MOD}" \
     Server.Name="${NAME}" Server.ListenPort="${LISTEN_PORT}" \
     Server.AdvertiseOnline="${ADVERTISE_ONLINE}" \
     Server.EnableSingleplayer="${ENABLE_SINGLE_PLAYER}" Server.Password="${PASSWORD}"
done
