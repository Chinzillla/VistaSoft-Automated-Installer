# VistaSoft Automated Installer

This program is meant to reduce installation time for a complete VistaSoft installation

- Create, export, and import options files across computers to reduce repeat configuring each install on multiple client computers
- Easy to use silent installer for dealer techs or IT staff

Currently only handles installation of vistasoft only, no pre or post installation configuration steps are added.

#### Download
- [Latest Version](https://drive.google.com/drive/folders/1tjfUEi0MWbsSNOpPHTx6kTrQ7HPCo1X6?usp=sharing)

#### Prerequisites for Development:
- Windows 11
- [Windows App SDK 2.2.0](https://aka.ms/windowsappsdk/2.2/2.2.0/windowsappruntimeinstall-x64.exe)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
- [WinUI3](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here?tabs=wingetconfig)


# Standard Procedure for VistaSoft Installer

> **Start on the Server PC**  
> **Note:** This procedure does not cover the installation of VistaSoft on the Server PC nor the Reconstruction PCs for 3D Prime/VistaPano 2.0/Provecta S-Pan itself.

---

## Prerequisites

1. Download the latest VistaSoft ISO:  
   [Air Techniques Drivers](https://www.airtechniques.com/drivers/)

2. Install VistaSoft on the Server.

3. Open VistaSoft from the Desktop.

4. Select **Server**.

---

## Moving an Existing Database or Changing the Original Database Path

1. Shut down all VistaSoft tasks and services.

2. Copy the following folder:

   ```text
   C:\VistaSoftData
   ```

3. Paste the copied folder into the desired destination folder.

### If Moving an Existing Database

1. Copy the following folder from the existing database:

   ```text
   {Data Folder}\P1\Images
   ```

2. Paste the `Images` folder into the destination database:

   ```text
   {Destination Data Folder}\P1\
   ```

3. Run **ServerManager (VistaSoft)**.

4. Select **Change Database**.

5. Select the destination database folder.

---

## First-Time Setup

1. Download the VistaSoft Installer:

   [VistaSoft Installer](https://drive.google.com/file/d/1P1d8pgWKMKcKqejk9F4k2eigHj6sgaoJ/view?usp=drive_link)

2. Inside the database folder, create a folder named `install`:

   ```text
   {destination_path}\VistaSoftData\install
   ```

3. Move the VistaSoft Installer into the `install` folder:

   ```text
   {destination_path}\VistaSoftData\install\{installer}
   ```

4. Run the VistaSoft Installer for the first time.

   > The application will automatically open after the installation is complete.

5. Configure the **Options** file.

### For 3D Prime

- Uncheck **VistaSoft Connect**.
- Select **Client**.
- If the clinic has any other Air Techniques devices, select the corresponding devices.

6. Click **Export Options** and save the Options file in:

   ```text
   {destination_path}\VistaSoftData\install
   ```

7. Move the VistaSoft ISO into the `install` folder:

   ```text
   {destination_path}\VistaSoftData\install\{vistasoft.iso}
   ```

8. Share the following folder over the network:

   ```text
   {destination_path}\VistaSoftData
   ```

---

## Client Setup

1. Navigate to the shared `install` folder on the Server:

   ```text
   \\{ServerHostname}\VistaSoftData\install\
   ```

2. Run the **VistaSoft Installer**.

3. Select **Import Options** and select the Options file located at:

   ```text
   \\{ServerHostname}\VistaSoftData\install\{options file}
   ```

4. Select **Open VistaSoft ISO** and select the ISO file located at:

   ```text
   \\{ServerHostname}\VistaSoftData\install\{vistasoft.iso}
   ```

5. Click **Install**.

   > **Note:** You do not need to wait for the installation to finish before moving on to the next Client PC. Once the installation has started, you can begin setting up the next computer.
