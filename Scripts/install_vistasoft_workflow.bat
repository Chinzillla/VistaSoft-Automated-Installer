@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "ISO_PATH=%VISTASOFT_ISO_PATH%"
if not defined ISO_PATH set "ISO_PATH=%~1"

set "SCRIPT_DIR=%~dp0"
set "ISO_MOUNTER=%SCRIPT_DIR%..\VistaSoftIsoMounter.exe"
set "WORKFLOW_ID=%RANDOM%_%RANDOM%_%RANDOM%"
set "MOUNT_OUTPUT_FILE=%TEMP%\vistasoft_mount_%WORKFLOW_ID%.txt"
set "STAGING_ROOT=%LOCALAPPDATA%\VistaSoftAutomatedInstaller"
set "MOUNTED_BY_HELPER=0"
set "WORKFLOW_EXIT_CODE=0"
set "WORKFLOW_ERROR="
set "WORKFLOW_ERROR_DETAIL="

if not defined ISO_PATH (
    set "WORKFLOW_EXIT_CODE=64"
    set "WORKFLOW_ERROR=ISO_PATH_REQUIRED"
    set "WORKFLOW_ERROR_DETAIL=Select a VistaSoft ISO before starting the installation."
    goto workflow_failed
)

if not defined VISTASOFT_OPTIONS_CONTENT_BASE64 (
    set "WORKFLOW_EXIT_CODE=65"
    set "WORKFLOW_ERROR=OPTIONS_CONTENT_REQUIRED"
    set "WORKFLOW_ERROR_DETAIL=The VistaSoft options content was not supplied to the workflow."
    goto workflow_failed
)

if not exist "%ISO_PATH%" (
    set "WORKFLOW_EXIT_CODE=2"
    set "WORKFLOW_ERROR=ISO_NOT_FOUND"
    set "WORKFLOW_ERROR_DETAIL=The selected ISO file no longer exists."
    goto workflow_failed
)

if not exist "%ISO_MOUNTER%" (
    set "WORKFLOW_EXIT_CODE=67"
    set "WORKFLOW_ERROR=ISO_MOUNTER_NOT_FOUND"
    set "WORKFLOW_ERROR_DETAIL=The ISO mounter helper is missing from the application installation."
    goto workflow_failed
)

if not defined TEMP (
    set "WORKFLOW_EXIT_CODE=5"
    set "WORKFLOW_ERROR=TEMP_FOLDER_NOT_AVAILABLE"
    set "WORKFLOW_ERROR_DETAIL=Windows did not provide a temporary folder for the installation."
    goto workflow_failed
)

if not defined LOCALAPPDATA (
    set "WORKFLOW_EXIT_CODE=5"
    set "WORKFLOW_ERROR=LOCAL_APP_DATA_NOT_AVAILABLE"
    set "WORKFLOW_ERROR_DETAIL=Windows did not provide a local application data folder."
    goto workflow_failed
)

echo Mounting the selected VistaSoft ISO...
call "%SCRIPT_DIR%mount_vistasoft.bat" > "%MOUNT_OUTPUT_FILE%" 2>&1
set "MOUNT_EXIT_CODE=%ERRORLEVEL%"
type "%MOUNT_OUTPUT_FILE%"

if not "%MOUNT_EXIT_CODE%"=="0" (
    del "%MOUNT_OUTPUT_FILE%" >nul 2>nul
    set "WORKFLOW_EXIT_CODE=%MOUNT_EXIT_CODE%"
    set "WORKFLOW_ERROR=ISO_MOUNT_FAILED"
    set "WORKFLOW_ERROR_DETAIL=Windows could not mount and identify the selected VistaSoft ISO."
    goto workflow_failed
)

set "INSTALLER_FOLDER="
for /f "tokens=1,* delims==" %%A in ('findstr /b /i "INSTALLER_FOLDER=" "%MOUNT_OUTPUT_FILE%"') do set "INSTALLER_FOLDER=%%B"
for /f "tokens=1,* delims==" %%A in ('findstr /b /i "MOUNTED_BY_HELPER=" "%MOUNT_OUTPUT_FILE%"') do set "MOUNTED_BY_HELPER=%%B"
del "%MOUNT_OUTPUT_FILE%" >nul 2>nul

if not defined INSTALLER_FOLDER (
    set "WORKFLOW_EXIT_CODE=71"
    set "WORKFLOW_ERROR=INSTALLER_FOLDER_NOT_RETURNED"
    set "WORKFLOW_ERROR_DETAIL=The selected ISO mounted, but the expected Installer folder was not returned."
    goto workflow_failed
)

if not exist "%INSTALLER_FOLDER%\" (
    set "WORKFLOW_EXIT_CODE=71"
    set "WORKFLOW_ERROR=INSTALLER_FOLDER_NOT_FOUND"
    set "WORKFLOW_ERROR_DETAIL=The expected Installer folder is not available on the selected ISO."
    goto workflow_failed
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
    set "WORKFLOW_EXIT_CODE=4"
    set "WORKFLOW_ERROR=VISTASOFT_INSTALLER_NOT_FOUND"
    set "WORKFLOW_ERROR_DETAIL=No VistaSoft installer application was found on the selected ISO."
    goto workflow_failed
)

if not "%INSTALLER_COUNT%"=="1" (
    set "WORKFLOW_EXIT_CODE=4"
    set "WORKFLOW_ERROR=MULTIPLE_VISTASOFT_INSTALLERS_FOUND"
    set "WORKFLOW_ERROR_DETAIL=More than one VistaSoft installer was found, so the workflow stopped rather than choosing the wrong one."
    goto workflow_failed
)

set "STAGING_FOLDER=%STAGING_ROOT%\%INSTALLER_NAME%-%WORKFLOW_ID%"
echo Creating staging folder: %STAGING_FOLDER%
mkdir "%STAGING_FOLDER%" >nul 2>nul

if errorlevel 1 (
    set "WORKFLOW_EXIT_CODE=5"
    set "WORKFLOW_ERROR=STAGING_FOLDER_CREATE_FAILED"
    set "WORKFLOW_ERROR_DETAIL=The local staging folder for the VistaSoft installer could not be created."
    goto workflow_failed
)

echo Copying the VistaSoft installer from the selected ISO...
copy /y "%INSTALLER_EXE%" "%STAGING_FOLDER%\%INSTALLER_FILE%" >nul

if errorlevel 1 (
    set "WORKFLOW_EXIT_CODE=8"
    set "WORKFLOW_ERROR=INSTALLER_COPY_FAILED"
    set "WORKFLOW_ERROR_DETAIL=The VistaSoft installer could not be copied from the selected ISO."
    goto workflow_failed
)

set "STAGED_INSTALLER=%STAGING_FOLDER%\%INSTALLER_FILE%"
set "OPTIONS_FILE=%STAGED_INSTALLER%.options"
set "OPTIONS_BASE64_FILE=%TEMP%\vistasoft_options_%WORKFLOW_ID%.b64"

echo Verifying the VistaSoft installer digital signature...
"%ISO_MOUNTER%" verify "%STAGED_INSTALLER%"
set "VERIFY_EXIT_CODE=%ERRORLEVEL%"

if not "%VERIFY_EXIT_CODE%"=="0" (
    set "WORKFLOW_EXIT_CODE=%VERIFY_EXIT_CODE%"
    set "WORKFLOW_ERROR=INSTALLER_SIGNATURE_INVALID"
    set "WORKFLOW_ERROR_DETAIL=The copied installer was not signed by an approved VistaSoft publisher. It was not opened."
    goto workflow_failed
)

call :cleanup_mount

echo Writing VistaSoft options file: %OPTIONS_FILE%
> "%OPTIONS_BASE64_FILE%" echo %VISTASOFT_OPTIONS_CONTENT_BASE64%
certutil -f -decode "%OPTIONS_BASE64_FILE%" "%OPTIONS_FILE%" >nul 2>&1
set "OPTIONS_WRITE_EXIT_CODE=%ERRORLEVEL%"
del "%OPTIONS_BASE64_FILE%" >nul 2>nul

if not "%OPTIONS_WRITE_EXIT_CODE%"=="0" (
    set "WORKFLOW_EXIT_CODE=66"
    set "WORKFLOW_ERROR=OPTIONS_FILE_WRITE_FAILED"
    set "WORKFLOW_ERROR_DETAIL=The VistaSoft options file could not be created in the staging folder."
    goto workflow_failed
)

echo Running verified VistaSoft installer: %STAGED_INSTALLER%
pushd "%STAGING_FOLDER%"

if errorlevel 1 (
    set "WORKFLOW_EXIT_CODE=7"
    set "WORKFLOW_ERROR=STAGING_FOLDER_OPEN_FAILED"
    set "WORKFLOW_ERROR_DETAIL=The workflow could not open the VistaSoft staging folder."
    goto workflow_failed
)

start "" /wait "%STAGED_INSTALLER%"
set "INSTALLER_EXIT_CODE=%ERRORLEVEL%"
popd

if not defined INSTALLER_EXIT_CODE (
    set "WORKFLOW_EXIT_CODE=81"
    set "WORKFLOW_ERROR=VISTASOFT_INSTALLER_RESULT_MISSING"
    set "WORKFLOW_ERROR_DETAIL=Windows did not return a result from the VistaSoft installer."
    goto workflow_failed
)

echo VISTASOFT_INSTALLER_EXIT_CODE=%INSTALLER_EXIT_CODE%
call :is_success_installer_exit_code "%INSTALLER_EXIT_CODE%"
if "%ERRORLEVEL%"=="0" goto installer_success

set "WORKFLOW_EXIT_CODE=80"
set "WORKFLOW_ERROR=VISTASOFT_INSTALLER_FAILED"
set "WORKFLOW_ERROR_DETAIL=The verified VistaSoft installer started but reported a failure."
goto workflow_failed

:installer_success
echo WORKFLOW_RESULT=SUCCESS
echo VistaSoft installer completed with accepted exit code: %INSTALLER_EXIT_CODE%
echo STAGING_FOLDER=%STAGING_FOLDER%
echo INSTALLER_FILE=%STAGED_INSTALLER%
echo OPTIONS_FILE=%OPTIONS_FILE%
exit /b 0

:workflow_failed
call :cleanup_mount
if exist "%MOUNT_OUTPUT_FILE%" del "%MOUNT_OUTPUT_FILE%" >nul 2>nul
if defined OPTIONS_BASE64_FILE if exist "%OPTIONS_BASE64_FILE%" del "%OPTIONS_BASE64_FILE%" >nul 2>nul
echo WORKFLOW_RESULT=FAILED
echo WORKFLOW_ERROR=%WORKFLOW_ERROR%
echo WORKFLOW_ERROR_DETAIL=%WORKFLOW_ERROR_DETAIL%
echo WORKFLOW_EXIT_CODE=%WORKFLOW_EXIT_CODE%
if defined INSTALLER_EXIT_CODE echo VISTASOFT_INSTALLER_EXIT_CODE=%INSTALLER_EXIT_CODE%
exit /b %WORKFLOW_EXIT_CODE%

:cleanup_mount
if not "%MOUNTED_BY_HELPER%"=="1" exit /b 0
echo Unmounting the VistaSoft ISO...
"%ISO_MOUNTER%" unmount "%ISO_PATH%"
set "UNMOUNT_EXIT_CODE=%ERRORLEVEL%"
set "MOUNTED_BY_HELPER=0"
if not "%UNMOUNT_EXIT_CODE%"=="0" (
    echo WORKFLOW_WARNING=ISO_UNMOUNT_FAILED
    echo The installation can continue, but Windows could not automatically unmount the selected ISO.
)
exit /b 0

:is_success_installer_exit_code
if "%~1"=="0" exit /b 0
if "%~1"=="6" exit /b 0
if "%~1"=="1641" exit /b 0
if "%~1"=="3010" exit /b 0
exit /b 1
