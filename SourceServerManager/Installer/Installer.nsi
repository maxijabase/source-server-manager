; SourceServerManager Installer Script
; Created with NSIS
!include "MUI2.nsh"

; General
Name "SourceServerManager"
OutFile "SourceServerManager.Installer.exe"
Unicode True

; Icon settings
!define ICON_FILE "..\Assets\icon.ico"
!define MUI_ICON "${ICON_FILE}"
!define MUI_UNICON "${ICON_FILE}"

; Default installation folder
InstallDir "$PROGRAMFILES\SourceServerManager"

; Request application privileges
RequestExecutionLevel admin

; Interface Settings
!define MUI_ABORTWARNING

; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "License.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

; Main install section
Section "MainSection" SEC01
    SetOutPath "$INSTDIR"
    SetOverwrite on
    
    ; Add your compiled application files here
    File "..\bin\Release\net8.0\publish\SourceServerManager.exe"
    File /r "..\bin\Release\net8.0\publish\*.*"
    
    ; Make sure the icon is included
    SetOutPath "$INSTDIR\Assets"
    File "${ICON_FILE}"
    SetOutPath "$INSTDIR"
    
    ; Create shortcuts
    CreateDirectory "$SMPROGRAMS\Source Server Manager"
    CreateShortcut "$SMPROGRAMS\Source Server Manager\Source Server Manager.lnk" "$INSTDIR\SourceServerManager.exe" "" "$INSTDIR\Assets\icon.ico"
    CreateShortcut "$DESKTOP\Source Server Manager.lnk" "$INSTDIR\SourceServerManager.exe" "" "$INSTDIR\Assets\icon.ico"
SectionEnd

; Uninstaller section
Section "Uninstall"
    Delete "$INSTDIR\*.*"
    
    ; Remove subdirectories
    RMDir /r "$INSTDIR\Assets"
    RMDir /r "$INSTDIR"
    
    ; Remove shortcuts
    Delete "$SMPROGRAMS\Source Server Manager\*.*"
    RMDir "$SMPROGRAMS\Source Server Manager"
    Delete "$DESKTOP\Source Server Manager.lnk"
    
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SourceServerManager"
SectionEnd

; Create the uninstaller
Section -Post
    WriteUninstaller "$INSTDIR\uninstall.exe"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SourceServerManager" "DisplayName" "Source Server Manager"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SourceServerManager" "UninstallString" "$INSTDIR\uninstall.exe"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SourceServerManager" "DisplayIcon" "$INSTDIR\Assets\icon.ico"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\SourceServerManager" "Publisher" "github.com/maxijabase"
SectionEnd