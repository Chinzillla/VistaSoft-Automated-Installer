# VistaSoft Installation Script

@Author: Brandon Chin

This program is meant to reduce installation time and create a standardized protocol for VistaSoft by automating all pre and post installation steps that we take internally for a complete VistaSoft installation

- Standardization can ensure that every PC has proper permissions in place and maintains secure
- Reducing tech team operational costs
- Easier onboarding of dealer technicians and IT personnel for large volume VistaSoft installations

#### Prerequisites for Dev:

- Windows 11
- [Windows App SDK 2.2.0](https://aka.ms/windowsappsdk/2.2/2.2.0/windowsappruntimeinstall-x64.exe)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
- [WinUI3](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here?tabs=wingetconfig)

## Automated workflows

### 1. Uninstallation of older software
- manual uninstallation of visionx
- manual uninstallation of vsmonitor, and any other visionx packages
- manual removal of air techniques folders
- manual removal of visionx registries

#### Approximate Operational Time:
- 3-4 minutes per install

#### Edge Cases/Constraints:
- Manual Navigation

### 2. Download of VistaSoft
- Navigating to air techniques drivers page and starting installation

#### Approximate Operational Time:
- 10-15 seconds per install

#### Edge Cases/Constraints:
- Computer RAM free usage capacity
- Computer Network Speed
- Loading Air Techniques Drivers page

### 3. Mounting of VistaSoft
- Mounting vistasoft from downloads folder

#### Approximate Operational time 
- 3-4 seconds per install

#### Edge Cases/Constraints:
- ISO mounting requires Windows 8 or newer and administrator privileges for the virtual disk attach operation.

### Total Time Saved
Per Install:
- 3-4 minutes per install
- 10-15 seconds per install
- 3-4 seconds per install

4:19 minutes per install saved

average >5 clients per install = **21 minutes** saved per install session

## VistaSoft Installer UI

Using WinUI for the frontend of the project.

Purpose:
- I want to make installs quick, intuitive, and scalable.
- Providing anyone the ability to install VistaSoft on many machines even during rush hours without disruptions
- Ability to setup quickly and run in the background

The installer reads an .options file which can be reused on multiple machines rather than manually edited
- You can view a sample of the options file in Docs/VistaSoft-windows-installer-4.0.12.59006-x64.exe.options

### VistaSoft Installer Features

In my app, you can 
- Import an existing options file
- Create a new options file
- Install VistaSoft with the options file

## Future Features

- Uninstallation support for wider variety of VisionX, VistaEasy, and DBSWIN Softwares
- Post VistaSoft installation automation scripts for opening ports, setting folder virus exclusions, and permissions
- Ability to browse to an iso, otherwise to choose a version of VistaSoft to download from Air Techniques website

## Build the MSI installer

The MSI is built with WiX Toolset from a self-contained x64 publish of the WinUI app. The publish output includes the .NET runtime, Windows App SDK files, the CMD workflow, and the native ISO helper. The installed workflow does not use PowerShell.

From the repository root, run:

```powershell
.\Installer\Build-Msi.ps1
```

The installer will be written to:

```text
artifacts\msi\VistaSoftAutomatedInstaller-0.0.2-x64.msi
```

To build a new MSI version, increase the three-part internal Windows Installer version and set the version users should see:

```powershell
.\Installer\Build-Msi.ps1 -ProductVersion 0.0.6 -DisplayVersion 0.0.3
```

Run the MSI as administrator on target computers. It installs the app under `Program Files\VistaSoft Automated Installer`, adds all-user Start Menu and Desktop shortcuts, and opens the app after an interactive install finishes.

During a VistaSoft installation, the app mounts the exact ISO selected by the user, copies the installer locally, verifies that it has a trusted Air Techniques or Duerr Dental signature, unmounts the ISO, creates the options file, and starts the verified installer. Imported options that are not shown in the form are preserved.

If an install fails, the app explains which step failed and writes a diagnostic log under `%LOCALAPPDATA%\VistaSoftAutomatedInstaller\Logs`. A VistaSoft return code such as `255` is recorded separately from the automated workflow code, so it is clear whether Windows, the ISO helper, or VistaSoft itself reported the failure.

Windows Installer requires a numeric internal version. Lettered versions can still be used for the MSI filename and changelog while the internal version remains numeric. The current internal version is `0.0.5`, while the user-facing version is `0.0.2`. Rebuilt packages with the same internal version can replace each other, but increasing the internal version for every published release remains the clearest release history.

### Understand install errors

The app does not show a number by itself. It explains the likely cause, suggests the next action, and saves the complete diagnostic log. Examples include:

- Code `3`: a required file or folder path was not found.
- Code `5`: Windows denied access; run the app as administrator and check folder permissions.
- Code `225`: Windows Security or antivirus blocked a file; review Protection history before taking action.
- Code `740` or `1314`: administrator permission is required.
- Code `1223`: the administrator permission prompt was canceled.
- Code `1603`: VistaSoft reported a fatal installation error; restart Windows and review the log.
- Code `1618`: another installation is already running.
- Code `255`: VistaSoft returned an undocumented failure; the log shows which preparation steps completed successfully.

Codes `1641` and `3010` mean the installation succeeded and Windows must restart.

### Sign a public release

The default command creates an unsigned development package and displays a warning. A package distributed to other computers should be signed with your organization's trusted code-signing certificate.

List code-signing certificates installed for your Windows account:

```powershell
Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert
```

Build a signed release using the certificate thumbprint:

```powershell
.\Installer\Build-Msi.ps1 `
    -ProductVersion 0.0.6 `
    -DisplayVersion 0.0.3 `
    -CertificateThumbprint "YOUR_CERTIFICATE_THUMBPRINT" `
    -RequireSignature
```

The build signs both application executables before putting them into the MSI, signs the completed MSI, and verifies every signature. `-RequireSignature` stops the build if a valid signing certificate was not supplied.

### Run automated tests

```powershell
dotnet test .\VistaSoftUI.Tests\VistaSoftUI.Tests.csproj -c Release -p:Platform=x64
```
