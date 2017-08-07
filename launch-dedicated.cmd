:: example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated for details

@echo on

set Name="Dedicated Server"
set ListenPort=1234
set ExternalPort=1234
set AdvertiseOnline=True
set EnableSingleplayer=False
set Password=""

@echo off

title %Name%
FOR /F "tokens=1,2 delims==" %%A IN (mod.config) DO (set %%A=%%B)
if exist user.config (FOR /F "tokens=1,2 delims==" %%A IN (user.config) DO (set %%A=%%B))

set MOD_SEARCH_PATHS=%~dp0mods
if %INCLUDE_DEFAULT_MODS% neq "True" goto start
set MOD_SEARCH_PATHS=%MOD_SEARCH_PATHS%,./mods

:start
if not exist %ENGINE_DIRECTORY%\OpenRA.Game.exe goto noengine
cd %ENGINE_DIRECTORY%

:loop
OpenRA.Server.exe Game.Mod=%MOD_ID% Server.Name=%Name% Server.ListenPort=%ListenPort% Server.ExternalPort=%ExternalPort% Server.AdvertiseOnline=%AdvertiseOnline% Server.EnableSingleplayer=%EnableSingleplayer% Server.Password=%Password%
goto loop

:noengine
echo Required engine files not found.
echo Run `make all` in the mod directory to fetch and build the required files, then try again.
pause
exit /b