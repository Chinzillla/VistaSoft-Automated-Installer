@echo off
setlocal

set "ISO_PATH=%~1"
set "WORKFLOW_TIMESTAMP="

if "%ISO_PATH%"=="" (
    echo ISO path is required.
    exit /b 64
)

if "%VISTASOFT_OPTIONS_CONTENT_BASE64%"=="" (
    echo VistaSoft options content is required.
    exit /b 65
)

if not exist "%ISO_PATH%" (
    echo ISO file not found: %ISO_PATH%
    exit /b 2
)

set "WORKFLOW_TIMESTAMP=%DATE%_%TIME%"
set "WORKFLOW_TIMESTAMP=%WORKFLOW_TIMESTAMP:/=-%"
set "WORKFLOW_TIMESTAMP=%WORKFLOW_TIMESTAMP::=-%"
set "WORKFLOW_TIMESTAMP=%WORKFLOW_TIMESTAMP:.=-%"
set "WORKFLOW_TIMESTAMP=%WORKFLOW_TIMESTAMP: =_%"
set "WORKFLOW_TIMESTAMP=%WORKFLOW_TIMESTAMP:,=-%"

if "%WORKFLOW_TIMESTAMP%"=="" (
    echo Could not create workflow timestamp.
    exit /b 5
)

set "SCRIPT_DIR=%~dp0"
set "MOUNT_OUTPUT_FILE=%TEMP%\vistasoft_mount_%WORKFLOW_TIMESTAMP%.txt"

echo Running mount script...
call "%SCRIPT_DIR%mount_vistasoft.bat" "%ISO_PATH%" > "%MOUNT_OUTPUT_FILE%" 2>&1
set "MOUNT_EXIT_CODE=%ERRORLEVEL%"
type "%MOUNT_OUTPUT_FILE%"

if not "%MOUNT_EXIT_CODE%"=="0" (
    del "%MOUNT_OUTPUT_FILE%" >nul 2>nul
    exit /b %MOUNT_EXIT_CODE%
)

set "INSTALLER_FOLDER="
for /f "tokens=1,* delims==" %%A in ('findstr /b /i "INSTALLER_FOLDER=" "%MOUNT_OUTPUT_FILE%"') do (
    set "INSTALLER_FOLDER=%%B"
)

del "%MOUNT_OUTPUT_FILE%" >nul 2>nul

if "%INSTALLER_FOLDER%"=="" (
    echo Mount script did not return INSTALLER_FOLDER.
    exit /b 3
)

if not exist "%INSTALLER_FOLDER%\" (
    echo Installer folder not found: %INSTALLER_FOLDER%
    exit /b 3
)

set "INSTALLER_EXE="
set "INSTALLER_FILE="
set "INSTALLER_NAME="
set "INSTALLER_COUNT=0"

for %%F in ("%INSTALLER_FOLDER%\VistaSoft-windows-installer-*.exe") do (
    if exist "%%~fF" (
        set /a INSTALLER_COUNT+=1
        set "INSTALLER_EXE=%%~fF"
        set "INSTALLER_FILE=%%~nxF"
        set "INSTALLER_NAME=%%~nF"
    )
)

if "%INSTALLER_COUNT%"=="0" (
    echo Could not find VistaSoft installer exe in: %INSTALLER_FOLDER%
    exit /b 4
)

if not "%INSTALLER_COUNT%"=="1" (
    echo Expected one VistaSoft installer exe, but found %INSTALLER_COUNT%.
    exit /b 4
)

if "%LOCALAPPDATA%"=="" (
    echo LOCALAPPDATA is not available.
    exit /b 5
)

set "STAGING_ROOT=%LOCALAPPDATA%\VistaSoftAutomatedInstaller"
set "STAGING_FOLDER=%STAGING_ROOT%\%INSTALLER_NAME%-%WORKFLOW_TIMESTAMP%"

echo Creating staging folder: %STAGING_FOLDER%
mkdir "%STAGING_FOLDER%" >nul 2>nul

if errorlevel 1 (
    echo Could not create staging folder: %STAGING_FOLDER%
    exit /b 5
)

echo Copying installer application...
copy /y "%INSTALLER_EXE%" "%STAGING_FOLDER%\%INSTALLER_FILE%" >nul

if errorlevel 1 (
    echo Could not copy installer application: %INSTALLER_EXE%
    exit /b 8
)

set "STAGED_INSTALLER=%STAGING_FOLDER%\%INSTALLER_FILE%"
set "OPTIONS_FILE=%STAGED_INSTALLER%.options"
set "OPTIONS_BASE64_FILE=%TEMP%\vistasoft_options_%WORKFLOW_TIMESTAMP%.b64"

echo Writing options file: %OPTIONS_FILE%
> "%OPTIONS_BASE64_FILE%" echo %VISTASOFT_OPTIONS_CONTENT_BASE64%
certutil -f -decode "%OPTIONS_BASE64_FILE%" "%OPTIONS_FILE%" >nul
set "OPTIONS_WRITE_EXIT_CODE=%ERRORLEVEL%"
del "%OPTIONS_BASE64_FILE%" >nul 2>nul

if not "%OPTIONS_WRITE_EXIT_CODE%"=="0" (
    echo Could not write options file: %OPTIONS_FILE%
    exit /b 66
)

echo Running VistaSoft installer: %STAGED_INSTALLER%
pushd "%STAGING_FOLDER%"

if errorlevel 1 (
    echo Could not open staging folder: %STAGING_FOLDER%
    exit /b 7
)

start "" /wait "%STAGED_INSTALLER%"
set "INSTALLER_EXIT_CODE=%ERRORLEVEL%"
popd

echo INSTALLER_EXIT_CODE=%INSTALLER_EXIT_CODE%

call :is_success_installer_exit_code "%INSTALLER_EXIT_CODE%"
if "%ERRORLEVEL%"=="0" goto installer_success

echo VistaSoft installer failed. Exit code: %INSTALLER_EXIT_CODE%
exit /b %INSTALLER_EXIT_CODE%

:installer_success
echo VistaSoft installer completed with accepted exit code: %INSTALLER_EXIT_CODE%
echo STAGING_FOLDER=%STAGING_FOLDER%
echo INSTALLER_FILE=%STAGED_INSTALLER%
echo OPTIONS_FILE=%OPTIONS_FILE%
exit /b 0

:is_success_installer_exit_code
if "%~1"=="0" exit /b 0
if "%~1"=="6" exit /b 0
exit /b 1
