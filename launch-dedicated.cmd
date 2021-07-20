:: example launch script, see https://github.com/OpenRA/OpenRA/wiki/Dedicated for details

@echo on

set Name="Dedicated Server"
set ListenPort=1234
set AdvertiseOnline=True
set Password=""

set RequireAuthentication=False
set ProfileIDBlacklist=""
set ProfileIDWhitelist=""

set EnableSingleplayer=False
set EnableSyncReports=False
set EnableGeoIP=True
set ShareAnonymizedIPs=True

@echo off
setlocal EnableDelayedExpansion

title %Name%
FOR /F "tokens=1,2 delims==" %%A IN (mod.config) DO (set %%A=%%B)
if exist user.config (FOR /F "tokens=1,2 delims==" %%A IN (user.config) DO (set %%A=%%B))
set MOD_SEARCH_PATHS=%~dp0mods,./mods

if "!MOD_ID!" == "" goto badconfig
if "!ENGINE_VERSION!" == "" goto badconfig
if "!ENGINE_DIRECTORY!" == "" goto badconfig

if not exist %ENGINE_DIRECTORY%\bin\OpenRA.exe goto noengine
>nul find %ENGINE_VERSION% %ENGINE_DIRECTORY%\VERSION || goto noengine
cd %ENGINE_DIRECTORY%

:loop
bin\OpenRA.Server.exe Game.Mod=%MOD_ID% Engine.EngineDir=".." Server.Name=%Name% Server.ListenPort=%ListenPort% Server.AdvertiseOnline=%AdvertiseOnline% Server.EnableSingleplayer=%EnableSingleplayer% Server.Password=%Password% Server.RequireAuthentication=%RequireAuthentication% Server.ProfileIDBlacklist=%ProfileIDBlacklist% Server.ProfileIDWhitelist=%ProfileIDWhitelist% Server.EnableSyncReports=%EnableSyncReports% Server.EnableGeoIP=%EnableGeoIP% Server.ShareAnonymizedIPs=%ShareAnonymizedIPs% Engine.SupportDir=%SupportDir%
goto loop

:noengine
echo Required engine files not found.
echo Run `make all` in the mod directory to fetch and build the required files, then try again.
pause
exit /b

:badconfig
echo Required mod.config variables are missing.
echo Ensure that MOD_ID ENGINE_VERSION and ENGINE_DIRECTORY are
echo defined in your mod.config (or user.config) and try again.
pause
exit /b
