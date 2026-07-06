# VistaSoft Installation Script

@Author: Brandon Chin

Script is meant to reduce installation time and create a standardized protocol for VistaSoft by automating all pre and post installation steps that we take internally for a complete VistaSoft installation

- Standardization can ensure that every PC has proper permissions in place and maintains secure
- Reducing tech team operational costs
- Easier onboarding of dealer technicians and it personal for large volume vistasoft installations

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

## Future Features

- Uninstallation support for wider variety of VisionX Softwares (3.0.33 and lower)
- Option File creation for vistasoft
- Installation of VistaSoft
- Post VistaSoft installation automation scripts
- Ability to point to a network shared folder that contains the iso instead of reinstallation