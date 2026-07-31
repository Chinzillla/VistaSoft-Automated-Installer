@echo off
setlocal EnableExtensions

set "ISO_MOUNTER=%~dp0..\VistaSoftIsoMounter.exe"

if not exist "%ISO_MOUNTER%" (
    echo ISO mounter helper not found: %ISO_MOUNTER%
    exit /b 67
)

"%ISO_MOUNTER%" "%~1"
exit /b %ERRORLEVEL%
