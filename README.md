# Red Alert 2 mod for OpenRA

[![Build Status](https://api.travis-ci.com/OpenRA/ra2.svg?branch=master)](https://travis-ci.com/github/OpenRA/ra2)

Consult the [wiki](https://github.com/OpenRA/ra2/wiki) for instructions on how to install and use this.

[![Bountysource](https://api.bountysource.com/badge/tracker?tracker_id=27677844)](https://www.bountysource.com/teams/openra/issues?tracker_ids=27677844)

EA has not endorsed and does not support this product.

A Red Alert 2 mod based on the 2.5D features of the OpenRA engine. Requires the original game data to play.

## Dependencies
[Install](https://github.com/OpenRA/OpenRA/blob/release-20200202/INSTALL.md) the dependencies required to compile OpenRA.
To use higher version of dotnet in linux/mac:
 export DOTNET_ROLL_FORWARD=LatestMajor

## Installing the mod

- Download the latest version of the RA2 repository from https://github.com/OpenRA/ra2/archive/master.zip.
- Unzip it and run `make all` (in the command line) on Windows and `make` on Unix systems.
- Use `launch-game.cmd` on Windows and `launch-game.sh` on Unix systems to run Red Alert 2.

### Alternate method

The method above automatically downloads the OpenRA engine from GitHub. If you have already cloned the engine (OpenRA), you may follow the instructions below.

- Make sure the engine resides in `ra2/engine`. There should be a file named `VERSION` in `ra2/engine`.
- Check out the appropriate version (as indicated by the `ENGINE_VERSION` variable in `ra2/mod.config`) of the OpenRA engine. For example, run `git checkout release-20180307` in `ra2/engine`.
- Edit `ra2/engine/VERSION` to match the value of `ENGINE_VERSION` in `ra2/mod.config`. For example, if `ra2/mod.config` says `ENGINE_VERSION="release-20180307"`, then the contents of `ra2/engine/VERSION` should be `release-20180307`.
- Create file `ra2/user.config` with the following contents: `AUTOMATIC_ENGINE_MANAGEMENT="False"`
- Run `make dependencies` in `ra2/engine`. This fetches required external libraries.
- Run `make all` in `ra2`

## Installing the content manually

Note: Normally installing the content from the ingame content installer is enough.

The mod expects the original Red Alert 2 game assets in place. Put the .mix archives in the following directory depending on your operating system.
  * Windows:  `%APPDATA%\OpenRA\Content\ra2\` or `%USERPROFILE%\Documents\OpenRA\Content\ra2\` (older installations)
  * Mac OSX:  `~/Library/Application Support/OpenRA/Content/ra2/`
  * Linux:  `~/.config/openra/Content/ra2/`

Create the `ra2` directory if it does not exist.

### Download
The game can be bought and downloaded from the following official places:

* [EA Origin](https://www.origin.com/en-de/store/buy/c-c-the-ultimate-collection/pc-download/bundle/ultimate-collection)

### Disc
If you own the original CDs:

1. Locate Game1.CAB on CD1 in the INSTALL/ directory.
2. Copy all required mixes into your content folder.  

The mixes inside of `Game1.CAB` are `ra2.mix` and `language.mix`.
Copy those two into your content folder.  
For the soundtrack you want `theme.mix` from your CD.

#### The "Install content" panel shows still up?

Make sure that you have files named `ra2.mix` and `language.mix` in your content folder. Even though you e.g. extracted all mixes from `ra2.mix` (which would work ingame) the OpenRA content installer still looks for `ra2.mix`.
