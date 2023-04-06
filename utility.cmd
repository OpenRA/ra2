@echo off
setlocal EnableDelayedExpansion

FOR /F "tokens=1,2 delims==" %%A IN (mod.config) DO (set %%A=%%B)
if exist user.config (FOR /F "tokens=1,2 delims==" %%A IN (user.config) DO (set %%A=%%B))
set MOD_SEARCH_PATHS=%~dp0mods,./mods
set ENGINE_DIR=..
if "!MOD_ID!" == "" goto badconfig
if "!ENGINE_VERSION!" == "" goto badconfig
if "!ENGINE_DIRECTORY!" == "" goto badconfig

title OpenRA.Utility.exe %MOD_ID%

set TEMPLATE_DIR=%CD%
if not exist %ENGINE_DIRECTORY%\bin\OpenRA.exe goto noengine
>nul find %ENGINE_VERSION% %ENGINE_DIRECTORY%\VERSION || goto noengine
cd %ENGINE_DIRECTORY%

set argC=0
for %%x in (%*) do set /A argC+=1

if %argC% == 0 goto choosemod

if %argC% == 1 (
    set MOD_ID=%1
    goto loop
)

if %argC% GEQ 2 (
    @REM This option is for use by other scripts so we don't want any extra output here - before or after.
    call bin\OpenRA.Utility.exe %*
    EXIT /B 0
)

:choosemod
echo ----------------------------------------
echo.
call bin\OpenRA.Utility.exe
echo Enter --exit to exit
set /P mod="Please enter a modname: OpenRA.Utility.exe "
if /I "%mod%" EQU "--exit" (exit /b)
set MOD_ID=%mod%
echo.

:loop
echo.
echo ----------------------------------------
echo.
echo Enter a utility command or --exit to exit.
echo Press enter to view a list of valid utility commands.
echo.

set /P command="Please enter a command: OpenRA.Utility.exe %MOD_ID% "
if /I "%command%" EQU "--exit" (cd %TEMPLATE_DIR% & exit /b)
echo.
echo ----------------------------------------
echo.
echo Starting OpenRA.Utility.exe %MOD_ID% %command%
call bin\OpenRA.Utility.exe %MOD_ID% %command%
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
