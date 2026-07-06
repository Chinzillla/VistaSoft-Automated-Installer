# VistaSoft Installation Script

@Author: Brandon Chin

This program is meant to reduce installation time and create a standardized protocol for VistaSoft by automating all pre and post installation steps that we take internally for a complete VistaSoft installation

- Standardization can ensure that every PC has proper permissions in place and maintains secure
- Reducing tech team operational costs
- Easier onboarding of dealer technicians and IT personnel for large volume VistaSoft installations

## Automated workflows

### 1. Uninstallation of older software
- manual uninstallation of visionx
- manual uninstallation of vsmonitor, and any other visionx packages
- manual removal of air techniques folders
- manual removal of visionx registries

#### Approximate Time:
- 3-4 minutes

#### Edge Cases/Constraints:
- Manual Navigation
#
### 2. Download of VistaSoft
- Navigating to air techniques drivers page and starting installation

#### Approximate Time:
- 10-15 seconds

#### Edge Cases/Constraints:
- Computer RAM free usage capacity
- Computer Network Speed
- Loading Air Techniques Drivers page
#
### 3. Mounting of VistaSoft
- Mounting vistasoft from downloads folder

#### Approximate time 
- 3-4 seconds

#### Edge Cases/Constraints:
- Some computers have issues using explorer.exe to mount images (very rare)

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
- Uninstall detected legacy softwares (currently only visionx 3.0.34)

## Future Features

- Uninstallation support for wider variety of VisionX, VistaEasy, and DBSWIN Softwares
- Post VistaSoft installation automation scripts for opening ports, setting folder virus exclusions, and permissions
- Ability to browse to an iso, otherwise to choose a version of VistaSoft to download from Air Techniques website