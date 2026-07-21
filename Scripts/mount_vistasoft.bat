@echo off
setlocal

set "ISO_PATH=%~1"

if "%ISO_PATH%"=="" (
    echo ISO path is required.
    exit /b 64
)

if not exist "%ISO_PATH%" (
    echo ISO file not found: %ISO_PATH%
    exit /b 2
)

echo Mounting ISO: %ISO_PATH%
explorer "%ISO_PATH%"

set "ISO_DRIVE="

for /l %%A in (1,1,30) do (
    for %%D in (D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
        if exist "%%D:\Installer\" (
            set "ISO_DRIVE=%%D:"
            goto found_iso
        )
    )

    timeout /t 1 /nobreak >nul
)

echo Could not find mounted VistaSoft ISO drive with an Installer folder.
exit /b 1

:found_iso
echo ISO_DRIVE=%ISO_DRIVE%
echo INSTALLER_FOLDER=%ISO_DRIVE%\Installer
exit /b 0
