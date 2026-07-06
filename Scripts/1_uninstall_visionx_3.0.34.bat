@echo off

taskkill /F /IM visionx*

winget uninstall "visionx"
winget uninstall --id "ARP\Machine\X64\VS Monitor_is1"
winget uninstall --id "ARP\Machine\X86\VS Monitor_is1"
winget uninstall --id "ARP\User\X64\VS Monitor_is1"
winget uninstall --id "ARP\User\X86\VS Monitor_is1"
winget uninstall --name "Microsoft Visual C++ 2008 Redistributable - x64 9.0.30729" --silent --accept-source-agreements --disable-interactivity
winget uninstall --name "Microsoft Visual C++ 2008 Redistributable - x86 9.0.30729.17" --silent --accept-source-agreements --disable-interactivity
winget uninstall --name "Microsoft Visual C++ 2010  x64 Redistributable - 10.0.30319" --silent --accept-source-agreements --disable-interactivity
winget uninstall --name "Microsoft Visual C++ 2010  x86 Redistributable - 10.0.30319" --silent --accept-source-agreements --disable-interactivity
winget uninstall --name "Microsoft Windows Desktop Runtime - 6.0.15 (x64)" --silent --accept-source-agreements --disable-interactivity
winget uninstall --name "Microsoft Windows Desktop Runtime - 6.0.15 (x86)" --silent --accept-source-agreements --disable-interactivity
winget uninstall --name "Microsoft Visual C++ 2012 Redistributable (x64) - 11.0.61030" --silent --accept-source-agreements --disable-interactivity
winget uninstall --name "Microsoft Visual C++ 2012 Redistributable (x86) - 11.0.61030" --silent --accept-source-agreements --disable-interactivity
winget uninstall --name "Microsoft Visual C++ 2013 Redistributable (x64) - 12.0.30501" --silent --accept-source-agreements --disable-interactivity
winget uninstall --name "Microsoft Visual C++ 2013 Redistributable (x86) - 12.0.30501" --silent --accept-source-agreements --disable-interactivity
winget uninstall --name "Microsoft Visual C++ 2015-2019 Redistributable (x86) - 14.21.27702" --silent --accept-source-agreements --disable-interactivity
winget uninstall --name "Microsoft Visual C++ 2015-2019 Redistributable (x64) - 14.26.28720" --silent --accept-source-agreements --disable-interactivity
winget uninstall --name "Microsoft ASP.NET Core 6.0.15 - Shared Framework (x64)" --silent --accept-source-agreements --disable-interactivity

reg delete "HKLM\SOFTWARE\Air Techniques" /f
reg delete "HKLM\SOFTWARE\Duerr" /f
reg delete "HKLM\SOFTWARE\DUERR DENTAL" /f
reg delete "HKLM\SOFTWARE\DUERR DENTAL AG" /f
reg delete "HKLM\SOFTWARE\Dürr Dental" /f
reg delete "HKLM\SOFTWARE\WOW6432Node\AIR TECHNIQUES" /f
reg delete "HKLM\SOFTWARE\WOW6432Node\Duerr" /f
reg delete "HKLM\SOFTWARE\WOW6432Node\DUERR DENTAL" /f
reg delete "HKLM\SOFTWARE\WOW6432Node\DUERR DENTAL AG" /f
reg delete "HKLM\SOFTWARE\WOW6432Node\Dürr Dental" /f
reg delete "HKCU\SOFTWARE\AIR TECHNIQUES" /f
reg delete "HKCU\SOFTWARE\Duerr" /f
reg delete "HKCU\SOFTWARE\Air Tech" /f
reg delete "HKCU\SOFTWARE\DuerrDental" /f

rmdir /s /q  "C:\ProgramData\Air Techniques"
rmdir /s /q  "C:\program files\air techniques"

pause