#!/bin/sh
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
mono OpenRA/OpenRA.Game.exe Game.Mod=ra2 SupportDir="$DIR" "$@"
