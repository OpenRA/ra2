; Copyright (c) The OpenRA Developers and Contributors
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
OutFile "${OUTFILE}"

ManifestDPIAware true

Unicode True

Function .onInit
	!ifndef USE_PROGRAMFILES32
		SetRegView 64
	!endif
	ReadRegStr $INSTDIR HKLM "Software\${PACKAGING_WINDOWS_REGISTRY_KEY}" "InstallDir"
	StrCmp $INSTDIR "" unset done
	unset:
	!ifndef USE_PROGRAMFILES32
		StrCpy $INSTDIR "$PROGRAMFILES64\${PACKAGING_WINDOWS_INSTALL_DIR_NAME}"
	!else
		StrCpy $INSTDIR "$PROGRAMFILES32\${PACKAGING_WINDOWS_INSTALL_DIR_NAME}"
	!endif
	done:
FunctionEnd

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

	!ifdef USE_DISCORDID
		WriteRegStr HKLM "Software\Classes\discord-${USE_DISCORDID}" "" "URL:Run game ${USE_DISCORDID} protocol"
		WriteRegStr HKLM "Software\Classes\discord-${USE_DISCORDID}" "URL Protocol" ""
		WriteRegStr HKLM "Software\Classes\discord-${USE_DISCORDID}\DefaultIcon" "" "$INSTDIR\${MOD_ID}.ico,0"
		WriteRegStr HKLM "Software\Classes\discord-${USE_DISCORDID}\Shell\Open\Command" "" "$INSTDIR\${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe"
	!endif

SectionEnd

Section "Game" GAME
	SectionIn RO

	SetOutPath "$INSTDIR"
	File "${SRCDIR}\*.exe"
	File "${SRCDIR}\*.dll.config"
	File "${SRCDIR}\*.dll"
	File "${SRCDIR}\*.ico"
	File "${SRCDIR}\*.deps.json"
	File "${SRCDIR}\*.runtimeconfig.json"
	File "${SRCDIR}\global mix database.dat"
	File "${SRCDIR}\IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP"
	File "${SRCDIR}\VERSION"
	File "${SRCDIR}\AUTHORS"
	File "${SRCDIR}\COPYING"
	File /r "${SRCDIR}\mods"

	!insertmacro MUI_STARTMENU_WRITE_BEGIN Application
		CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
		CreateShortCut "$SMPROGRAMS\$StartMenuFolder\${PACKAGING_DISPLAY_NAME}.lnk" "$OUTDIR\${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe" "" \
			"$OUTDIR\${PACKAGING_WINDOWS_LAUNCHER_NAME}.exe" "" "" "" ""
	!insertmacro MUI_STARTMENU_WRITE_END

	SetOutPath "$INSTDIR\glsl"
	File "${SRCDIR}\glsl\*.frag"
	File "${SRCDIR}\glsl\*.vert"

	; Estimated install size for the control panel properties
	${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
	IntFmt $0 "0x%08X" $0
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}" "EstimatedSize" "$0"

	SetShellVarContext all
	CreateDirectory "$APPDATA\OpenRA\ModMetadata"
	SetOutPath "$INSTDIR"
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
	Delete $INSTDIR\*.exe
	Delete $INSTDIR\*.dll
	Delete $INSTDIR\*.ico
	Delete $INSTDIR\*.dll.config
	Delete $INSTDIR\*.deps.json
	Delete $INSTDIR\*.runtimeconfig.json
	Delete $INSTDIR\VERSION
	Delete $INSTDIR\AUTHORS
	Delete $INSTDIR\COPYING
	Delete "$INSTDIR\global mix database.dat"
	Delete $INSTDIR\IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP

	RMDir /r $INSTDIR\Support

	!ifndef USE_PROGRAMFILES32
		SetRegView 64
	!endif

	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PACKAGING_WINDOWS_REGISTRY_KEY}"
	DeleteRegKey HKLM "Software\Classes\openra-${MOD_ID}-${TAG}"

	!ifdef USE_DISCORDID
		DeleteRegKey HKLM "Software\Classes\discord-${USE_DISCORDID}"
	!endif

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
