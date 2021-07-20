@echo off
setlocal EnableDelayedExpansion
title OpenRA

FOR /F "tokens=1,2 delims==" %%A IN (mod.config) DO (set %%A=%%B)
if exist user.config (FOR /F "tokens=1,2 delims==" %%A IN (user.config) DO (set %%A=%%B))
set TEMPLATE_LAUNCHER=%0
set MOD_SEARCH_PATHS=%~dp0mods,./mods

if "!MOD_ID!" == "" goto badconfig
if "!ENGINE_VERSION!" == "" goto badconfig
if "!ENGINE_DIRECTORY!" == "" goto badconfig

set TEMPLATE_DIR=%CD%
if not exist %ENGINE_DIRECTORY%\OpenRA.Game.exe goto noengine
>nul find %ENGINE_VERSION% %ENGINE_DIRECTORY%\VERSION || goto noengine
cd %ENGINE_DIRECTORY%

OpenRA.Game.exe Game.Mod=%MOD_ID% Engine.LaunchPath="%TEMPLATE_LAUNCHER%" "Engine.ModSearchPaths=%MOD_SEARCH_PATHS%"  "%*"
set ERROR=%errorlevel%
cd %TEMPLATE_DIR%

if %ERROR% neq 0 goto crashdialog
exit /b

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

:crashdialog
echo ----------------------------------------
echo OpenRA has encountered a fatal error.
echo   * Log Files are available in Documents\OpenRA\Logs
echo   * FAQ is available at https://github.com/OpenRA/OpenRA/wiki/FAQ
echo ----------------------------------------
pause
