@echo off
setlocal EnableExtensions

rem Optional argument:
rem   1. Path to the VistaSoft ISO
set "ISO_PATH=%~1"
if "%ISO_PATH%"=="" set "ISO_PATH=%USERPROFILE%\Downloads\vistasoft_4.0.12.59006.iso"

rem Make sure the ISO exists before asking Windows to mount it.
if not exist "%ISO_PATH%" (
    echo VistaSoft ISO not found:
    echo %ISO_PATH%
    exit /b 1
)

echo Mounting VistaSoft ISO:
echo %ISO_PATH%
explorer "%ISO_PATH%"

rem Wait up to 30 seconds for the mounted ISO drive to appear.
set "ISO_DRIVE="

for /L %%A in (1,1,30) do (
    for %%D in (D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
        if exist "%%D:\Installer\" (
            set "ISO_DRIVE=%%D:"
            goto found_iso
        )
    )

    timeout /t 1 /nobreak >nul
)

echo Could not find a mounted VistaSoft ISO with an Installer folder.
exit /b 1

:found_iso
echo Found VistaSoft ISO drive: %ISO_DRIVE%
echo Installer folder: %ISO_DRIVE%\Installer
echo ISO_DRIVE=%ISO_DRIVE%
echo INSTALLER_FOLDER=%ISO_DRIVE%\Installer
exit /b 0
