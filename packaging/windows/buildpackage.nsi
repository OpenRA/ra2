; Copyright 2007-2015 OpenRA developers (see AUTHORS)
; This file is part of OpenRA.
;
;  OpenRA is free software: you can redistribute it and/or modify
;  it under the terms of the GNU General Public License as published by
;  the Free Software Foundation, either version 3 of the License, or
;  (at your option) any later version.
;
;  OpenRA is distributed in the hope that it will be useful,
;  but WITHOUT ANY WARRANTY; without even the implied warranty of
;  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;  GNU General Public License for more details.
;
;  You should have received a copy of the GNU General Public License
;  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.


!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "WordFunc.nsh"

Name "${PACKAGING_DISPLAY_NAME}"
OutFile "OpenRA.Setup.exe"

InstallDir "$PROGRAMFILES\${PACKAGING_WINDOWS_INSTALL_DIR_NAME}"
InstallDirRegKey HKLM "Software\${PACKAGING_WINDOWS_REGISTRY_KEY}" "InstallDir"

SetCompressor lzma
RequestExecutionLevel admin

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${PACKAGING_WINDOWS_LICENSE_FILE}"
!insertmacro MUI_PAGE_DIRECTORY

!define MUI_STARTMENUPAGE_REGISTRY_ROOT "HKLM"
!define MUI_STARTMENUPAGE_REGISTRY_KEY "Software\${PACKAGING_WINDOWS_REGISTRY_KEY}"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "Start Menu Folder"
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "OpenRA"

Var StartMenuFolder
!insertmacro MUI_PAGE_STARTMENU Application $StartMenuFolder

!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

;***************************
;Section Definitions
;***************************
Section "-Reg" Reg

	; Installation directory
	WriteRegStr HKLM "Software\${PACKAGING_WINDOWS_REGISTRY_KEY}" "InstallDir" $INSTDIR

	; Join server URL Scheme
	WriteRegStr HKLM "Software\Classes\openra-${MOD_ID}-${TAG}" "" "URL:Join OpenRA server"
	WriteRegStr HKLM "Software\Classes\openra-${MOD_ID}-${TAG}" "URL Protocol" ""
	WriteRegStr HKLM "Software\Classes\openra-${MOD_ID}-${TAG}\DefaultIcon" "" "$INSTDIR\${MOD_ID}.ico,0"
	WriteRegStr HKLM "Software\Classes\openra-${MOD_ID}-${TAG}\Shell\Open\Command" "" "$INSTDIR\${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe Launch.URI=%1"

SectionEnd

Section "Game" GAME
	SectionIn RO

	SetOutPath "$INSTDIR"
	File /r "${SRCDIR}\mods"
	File "${SRCDIR}\${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe"
	File "${SRCDIR}\OpenRA.Game.exe"
	File "${SRCDIR}\OpenRA.Game.exe.config"
	File "${SRCDIR}\OpenRA.Utility.exe"
	File "${SRCDIR}\OpenRA.Server.exe"
	File "${SRCDIR}\OpenRA.Platforms.Default.dll"
	File "${SRCDIR}\ICSharpCode.SharpZipLib.dll"
	File "${SRCDIR}\FuzzyLogicLibrary.dll"
	File "${SRCDIR}\Open.Nat.dll"
	File "${SRCDIR}\VERSION"
	File "${SRCDIR}\AUTHORS"
	File "${SRCDIR}\COPYING"
	File "${SRCDIR}\${MOD_ID}.ico"
	File "${SRCDIR}\SharpFont.dll"
	File "${SRCDIR}\SDL2-CS.dll"
	File "${SRCDIR}\OpenAL-CS.dll"
	File "${SRCDIR}\global mix database.dat"
	File "${SRCDIR}\MaxMind.Db.dll"
	File "${SRCDIR}\GeoLite2-Country.mmdb.gz"
	File "${SRCDIR}\eluant.dll"
	File "${SRCDIR}\rix0rrr.BeaconLib.dll"
	File "${DEPSDIR}\soft_oal.dll"
	File "${DEPSDIR}\SDL2.dll"
	File "${DEPSDIR}\freetype6.dll"
	File "${DEPSDIR}\lua51.dll"

	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
		CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\${PACKAGING_DISPLAY_NAME}.lnk" "$OUTDIR\${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe" "" \
			"$OUTDIR\${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe" "" "" "" ""
	!insertmacro MUI_STARTMENU_WRITE_END

	SetOutPath "$INSTDIR\lua"
	File "${SRCDIR}\lua\*.lua"

	SetOutPath "$INSTDIR\glsl"
	File "${SRCDIR}\glsl\*.frag"
	File "${SRCDIR}\glsl\*.vert"

	; Estimated install size for the control panel properties
	${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
	IntFmt $0 "0x%08X" $0
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}" "EstimatedSize" "$0"

	SetShellVarContext all
	CreateDirectory "$APPDATA\OpenRA\ModMetadata"
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" ${MOD_ID} --register-mod "$INSTDIR\${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe" system'
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" ${MOD_ID} --clear-invalid-mod-registrations system'
	SetShellVarContext current

SectionEnd

Section "Desktop Shortcut" DESKTOPSHORTCUT
	SetOutPath "$INSTDIR"
	CreateShortCut "$DESKTOP\OpenRA - ${PACKAGING_DISPLAY_NAME}.lnk" "$INSTDIR\${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe" "" \
		"$INSTDIR\${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe" "" "" "" ""
SectionEnd

;***************************
;Dependency Sections
;***************************
Section "-DotNet" DotNet
	ClearErrors
	ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client" "Install"
	IfErrors error 0
	IntCmp $0 1 0 error 0
	ClearErrors
	ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Install"
	IfErrors error 0
	IntCmp $0 1 done error done
	error:
		MessageBox MB_OK ".NET Framework v4.5 or later is required to run OpenRA."
		Abort
	done:
SectionEnd

;***************************
;Uninstaller Sections
;***************************
Section "-Uninstaller"
	WriteUninstaller $INSTDIR\uninstaller.exe
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}" "DisplayName" "${PACKAGING_DISPLAY_NAME}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}" "UninstallString" "$INSTDIR\uninstaller.exe"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}" "QuietUninstallString" "$\"$INSTDIR\uninstall.exe$\" /S"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}" "InstallLocation" "$INSTDIR"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}" "DisplayIcon" "$INSTDIR\${MOD_ID}.ico"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}" "Publisher" "${PACKAGING_AUTHORS}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}" "URLInfoAbout" "${PACKAGING_WEBSITE_URL}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}" "DisplayVersion" "${TAG}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}" "NoModify" "1"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}" "NoRepair" "1"
SectionEnd

!macro Clean UN
Function ${UN}Clean
	nsExec::ExecToLog '"$INSTDIR\OpenRA.Utility.exe" ${MOD_ID} --unregister-mod system'

	RMDir /r $INSTDIR\mods
	RMDir /r $INSTDIR\maps
	RMDir /r $INSTDIR\glsl
	RMDir /r $INSTDIR\lua
	Delete "$INSTDIR\${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe"
	Delete $INSTDIR\OpenRA.Game.exe
	Delete $INSTDIR\OpenRA.Game.exe.config
	Delete $INSTDIR\OpenRA.Utility.exe
	Delete $INSTDIR\OpenRA.Server.exe
	Delete $INSTDIR\OpenRA.Platforms.Default.dll
	Delete $INSTDIR\ICSharpCode.SharpZipLib.dll
	Delete $INSTDIR\FuzzyLogicLibrary.dll
	Delete $INSTDIR\Open.Nat.dll
	Delete $INSTDIR\SharpFont.dll
	Delete $INSTDIR\VERSION
	Delete $INSTDIR\AUTHORS
	Delete $INSTDIR\COPYING
	Delete $INSTDIR\${MOD_ID}.ico
	Delete "$INSTDIR\global mix database.dat"
	Delete $INSTDIR\MaxMind.Db.dll
	Delete $INSTDIR\GeoLite2-Country.mmdb.gz
	Delete $INSTDIR\KopiLua.dll
	Delete $INSTDIR\soft_oal.dll
	Delete $INSTDIR\SDL2.dll
	Delete $INSTDIR\lua51.dll
	Delete $INSTDIR\eluant.dll
	Delete $INSTDIR\freetype6.dll
	Delete $INSTDIR\SDL2-CS.dll
	Delete $INSTDIR\OpenAL-CS.dll
	Delete $INSTDIR\rix0rrr.BeaconLib.dll
	RMDir /r $INSTDIR\Support

	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}"
	DeleteRegKey HKLM "Software\Classes\openra-${MOD_ID}-${TAG}"

	Delete $INSTDIR\uninstaller.exe
	RMDir $INSTDIR

	!insertmacro MUI_STARTMENU_GETFOLDER Application $StartMenuFolder

	; Clean up start menu: Delete all our icons, and the OpenRA folder
	; *only* if we were the only installed version
	Delete "$SMPROGRAMS\$StartMenuFolder\${PACKAGING_DISPLAY_NAME}.lnk"
	RMDir "$SMPROGRAMS\$StartMenuFolder"

	Delete "$DESKTOP\OpenRA - ${PACKAGING_DISPLAY_NAME}.lnk"
	DeleteRegKey HKLM "Software\${PACKAGING_WINDOWS_REGISTRY_KEY}"
FunctionEnd
!macroend

!insertmacro Clean ""
!insertmacro Clean "un."

Section "Uninstall"
	Call un.Clean
SectionEnd

;***************************
;Section Descriptions
;***************************
LangString DESC_GAME ${LANG_ENGLISH} "${PACKAGING_DISPLAY_NAME} game files."
LangString DESC_DESKTOPSHORTCUT ${LANG_ENGLISH} "Place shortcut on the Desktop."

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${GAME} $(DESC_GAME)
	!insertmacro MUI_DESCRIPTION_TEXT ${DESKTOPSHORTCUT} $(DESC_DESKTOPSHORTCUT)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;***************************
;Callbacks
;***************************

Function .onInstFailed
	Call Clean
FunctionEnd
