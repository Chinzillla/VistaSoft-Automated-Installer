using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

const int MissingIsoPathExitCode = 64;
const int MountFailedExitCode = 1;
const int IsoNotFoundExitCode = 2;
const int MissingInstallerFolderExitCode = 3;

if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.WriteLine("ISO path is required.");
    return MissingIsoPathExitCode;
}

string isoPath = Path.GetFullPath(args[0]);

if (!File.Exists(isoPath))
{
    Console.WriteLine($"ISO file not found: {isoPath}");
    return IsoNotFoundExitCode;
}

Console.WriteLine($"Mounting VistaSoft ISO automatically: {isoPath}");

VirtualDiskMountResult mountResult = VirtualDiskIsoMounter.MountIso(isoPath);
string? installerFolder = FindVistaSoftInstallerFolder();

if (installerFolder is null)
{
    if (!mountResult.Success)
    {
        Console.WriteLine(mountResult.Message);
    }

    Console.WriteLine("Could not find mounted VistaSoft ISO drive with an Installer folder.");
    return mountResult.Success ? MissingInstallerFolderExitCode : MountFailedExitCode;
}

string isoDrive = Path.GetPathRoot(installerFolder)?.TrimEnd('\\') ?? string.Empty;
Console.WriteLine($"Found VistaSoft ISO drive: {isoDrive}");
Console.WriteLine($"Installer folder: {installerFolder}");
Console.WriteLine($"ISO_DRIVE={isoDrive}");
Console.WriteLine($"INSTALLER_FOLDER={installerFolder}");
return 0;

static string? FindVistaSoftInstallerFolder()
{
    for (int attempt = 1; attempt <= 30; attempt++)
    {
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.CDRom)
            {
                continue;
            }

            try
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                string installerFolder = Path.Combine(drive.RootDirectory.FullName, "Installer");

                if (Directory.Exists(installerFolder) &&
                    Directory.EnumerateFiles(installerFolder, "VistaSoft-windows-installer-*.exe").Any())
                {
                    return installerFolder.TrimEnd('\\');
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        Thread.Sleep(TimeSpan.FromSeconds(1));
    }

    return null;
}

internal sealed record VirtualDiskMountResult(bool Success, string Message);

internal static class VirtualDiskIsoMounter
{
    private const uint ErrorSuccess = 0;
    private const uint VirtualStorageTypeDeviceIso = 1;
    private const uint VirtualDiskAccessAttachReadOnly = 0x00010000;
    private const uint OpenVirtualDiskFlagNone = 0;
    private const uint OpenVirtualDiskVersion1 = 1;
    private const uint AttachVirtualDiskVersion1 = 1;
    private const uint AttachVirtualDiskFlagReadOnly = 0x00000001;
    private const uint AttachVirtualDiskFlagPermanentLifetime = 0x00000004;

    private static readonly Guid VirtualStorageTypeVendorMicrosoft =
        new("EC984AEC-A0F9-47E9-901F-71415A66345B");

    public static VirtualDiskMountResult MountIso(string isoPath)
    {
        VirtualStorageType storageType = new()
        {
            DeviceId = VirtualStorageTypeDeviceIso,
            VendorId = VirtualStorageTypeVendorMicrosoft,
        };

        OpenVirtualDiskParameters openParameters = new()
        {
            Version = OpenVirtualDiskVersion1,
            RWDepth = 0,
        };

        uint openResult = OpenVirtualDisk(
            ref storageType,
            isoPath,
            VirtualDiskAccessAttachReadOnly,
            OpenVirtualDiskFlagNone,
            ref openParameters,
            out SafeFileHandle virtualDiskHandle);

        if (openResult != ErrorSuccess)
        {
            return new VirtualDiskMountResult(
                false,
                $"Could not open ISO as a virtual disk. Error {openResult}: {GetErrorMessage(openResult)}");
        }

        using (virtualDiskHandle)
        {
            AttachVirtualDiskParameters attachParameters = new()
            {
                Version = AttachVirtualDiskVersion1,
                Reserved = 0,
            };

            uint attachResult = AttachVirtualDisk(
                virtualDiskHandle,
                IntPtr.Zero,
                AttachVirtualDiskFlagReadOnly | AttachVirtualDiskFlagPermanentLifetime,
                0,
                ref attachParameters,
                IntPtr.Zero);

            if (attachResult != ErrorSuccess)
            {
                return new VirtualDiskMountResult(
                    false,
                    $"Could not attach ISO as a virtual disk. Error {attachResult}: {GetErrorMessage(attachResult)}");
            }
        }

        return new VirtualDiskMountResult(true, "ISO mounted successfully.");
    }

    private static string GetErrorMessage(uint errorCode)
    {
        return new Win32Exception(unchecked((int)errorCode)).Message;
    }

    [DllImport("virtdisk.dll", CharSet = CharSet.Unicode)]
    private static extern uint OpenVirtualDisk(
        ref VirtualStorageType virtualStorageType,
        string path,
        uint virtualDiskAccessMask,
        uint flags,
        ref OpenVirtualDiskParameters parameters,
        out SafeFileHandle handle);

    [DllImport("virtdisk.dll")]
    private static extern uint AttachVirtualDisk(
        SafeFileHandle virtualDiskHandle,
        IntPtr securityDescriptor,
        uint flags,
        uint providerSpecificFlags,
        ref AttachVirtualDiskParameters parameters,
        IntPtr overlapped);

    [StructLayout(LayoutKind.Sequential)]
    private struct VirtualStorageType
    {
        public uint DeviceId;
        public Guid VendorId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct OpenVirtualDiskParameters
    {
        public uint Version;
        public uint RWDepth;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AttachVirtualDiskParameters
    {
        public uint Version;
        public uint Reserved;
    }
}
