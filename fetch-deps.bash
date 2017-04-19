#!/usr/bin/env bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

while [ -z "$OPENRA_ROOT" ]; do
	read -p "Enter the path to your OpenRA root: " OPENRA_ROOT
done

OPENRA_ROOT="${OPENRA_ROOT/\~/$HOME}"

DEST="$DIR/OpenRA.Mods.RA2/dependencies"

deps=("Eluant.dll" "OpenRA.Game.exe" "OpenRA.Mods.Common.dll" "OpenRA.Mods.RA.dll" "OpenRA.Mods.TS.dll")
for dep in "${deps[@]}"; do
	SRC="${OPENRA_ROOT}/$dep"
	ln -sf "$SRC" "$DEST" 2>/dev/null || :

	if [ ! -f "$DEST/$dep" ]; then
		find "$OPENRA_ROOT" -iname "$dep" -exec ln -sf {} "$DEST" \;
	fi

	if [ ! -f "$DEST/$dep" ]; then
		echo "Failed to link '$SRC' to '$DEST', please do so manually."
	fi
done
