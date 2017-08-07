@echo off

FOR /F "tokens=1,2 delims==" %%A IN (mod.config) DO (set %%A=%%B)
if exist user.config (FOR /F "tokens=1,2 delims==" %%A IN (user.config) DO (set %%A=%%B))

title OpenRA.Utility.exe %MOD_ID%

set MOD_SEARCH_PATHS=%~dp0mods
if %INCLUDE_DEFAULT_MODS% neq "True" goto start
set MOD_SEARCH_PATHS=%MOD_SEARCH_PATHS%,./mods

:start
set TEMPLATE_DIR=%CD%
if not exist %ENGINE_DIRECTORY%\OpenRA.Game.exe goto noengine
cd %ENGINE_DIRECTORY%

:loop
echo.
echo ----------------------------------------
echo.
echo Enter a utility command or --exit to exit.
echo Press enter to view a list of valid utility commands.
echo.

set /P command=Please enter a command: OpenRA.Utility.exe %MOD_ID% 
if /I "%command%" EQU "--exit" (cd %TEMPLATE_DIR% & exit /b)
echo.
echo ----------------------------------------
echo.
echo OpenRA.Utility.exe %MOD_ID% %command%
call OpenRA.Utility.exe %MOD_ID% %command%
goto loop

:noengine
echo Required engine files not found.
echo Run `make all` in the mod directory to fetch and build the required files, then try again.
pause
exit /b