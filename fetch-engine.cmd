@echo off
git clone https://github.com/OpenRA/OpenRA.git engine
cd engine
powershell -NoProfile -ExecutionPolicy Bypass -File make.ps1 dependencies
powershell -NoProfile -ExecutionPolicy Bypass -File make.ps1 all
echo =============================================================
echo Copy dlls to common mod folder
echo =============================================================
copy OpenRA.Mods.Common\bin\Debug\OpenRA.Mods.Common.dll mods\common /y
copy OpenRA.Mods.CnC\bin\Debug\OpenRA.Mods.CnC.dll mods\common /y
cd ..
