@echo off

explorer "%USERPROFILE%\Downloads\vistasoft_4.0.12.59006.iso"

set "ISO_DRIVE="

for %%D in (D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    if exist "%%D:\Installer\" (
        set "ISO_DRIVE=%%D:"
        goto found_iso
    )
)

echo Could not find mounted VistaSoft iso.
pause
exit /b 1

:found_iso
echo Found installer drive: %ISO_DRIVE%

pause