@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "ISO_MOUNTER=%~dp0..\VistaSoftIsoMounter.exe"
set "ISO_PATH=%VISTASOFT_ISO_PATH%"

if not defined ISO_PATH set "ISO_PATH=%~1"

if not exist "%ISO_MOUNTER%" (
    echo HELPER_ERROR=ISO_MOUNTER_NOT_FOUND
    echo ISO mounter helper not found: %ISO_MOUNTER%
    exit /b 67
)

"%ISO_MOUNTER%" mount "%ISO_PATH%"
exit /b %ERRORLEVEL%
