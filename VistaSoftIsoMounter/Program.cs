using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32.SafeHandles;

const int UsageExitCode = 64;
const int FileNotFoundExitCode = 2;
const int MountFailedExitCode = 70;
const int MountedDriveNotFoundExitCode = 71;
const int DetachFailedExitCode = 72;
const int SignatureVerificationFailedExitCode = 73;

try
{
    if (args.Length == 0)
    {
        WriteUsage();
        return UsageExitCode;
    }

    string command;
    string filePath;

    if (args.Length == 1)
    {
        // Preserve the original one-argument mount command for developer use.
        command = "mount";
        filePath = args[0];
    }
    else
    {
        command = args[0].Trim().ToLowerInvariant();
        filePath = args[1];
    }

    if (string.IsNullOrWhiteSpace(filePath))
    {
        Console.WriteLine("HELPER_ERROR=FILE_PATH_REQUIRED");
        Console.WriteLine("A file path is required.");
        return UsageExitCode;
    }

    string fullPath = Path.GetFullPath(filePath);

    return command switch
    {
        "mount" => MountIso(fullPath),
        "unmount" or "detach" => UnmountIso(fullPath),
        "verify" => VerifyInstaller(fullPath),
        _ => UnknownCommand(command),
    };
}
catch (Exception ex)
{
    Console.WriteLine("HELPER_ERROR=UNEXPECTED_HELPER_FAILURE");
    Console.WriteLine($"The installer helper failed unexpectedly: {ex.Message}");
    return MountFailedExitCode;
}

static int MountIso(string isoPath)
{
    if (!File.Exists(isoPath))
    {
        Console.WriteLine("HELPER_ERROR=ISO_NOT_FOUND");
        Console.WriteLine($"ISO file not found: {isoPath}");
        return FileNotFoundExitCode;
    }

    Console.WriteLine($"Mounting the selected VistaSoft ISO: {isoPath}");

    VirtualDiskMountResult mountResult = VirtualDiskIsoMounter.MountIso(isoPath);

    if (!mountResult.Success || string.IsNullOrWhiteSpace(mountResult.PhysicalDevicePath))
    {
        Console.WriteLine("HELPER_ERROR=ISO_MOUNT_FAILED");
        Console.WriteLine(mountResult.Message);
        return MountFailedExitCode;
    }

    string? isoDrive = MountedDriveLocator.FindDriveForPhysicalDevice(mountResult.PhysicalDevicePath);

    if (isoDrive is null)
    {
        if (mountResult.MountedByHelper)
        {
            VirtualDiskIsoMounter.DetachIso(isoPath);
        }

        Console.WriteLine("HELPER_ERROR=MOUNTED_DRIVE_NOT_FOUND");
        Console.WriteLine($"Windows mounted the ISO as {mountResult.PhysicalDevicePath}, but no matching drive letter appeared.");
        return MountedDriveNotFoundExitCode;
    }

    string installerFolder = Path.Combine(isoDrive, "Installer");

    if (!Directory.Exists(installerFolder) ||
        !Directory.EnumerateFiles(installerFolder, "VistaSoft-windows-installer-*.exe").Any())
    {
        if (mountResult.MountedByHelper)
        {
            VirtualDiskIsoMounter.DetachIso(isoPath);
        }

        Console.WriteLine("HELPER_ERROR=VISTASOFT_INSTALLER_NOT_ON_SELECTED_ISO");
        Console.WriteLine($"The selected ISO mounted as {isoDrive}, but it does not contain the expected VistaSoft installer.");
        return MountedDriveNotFoundExitCode;
    }

    Console.WriteLine($"Selected ISO drive: {isoDrive}");
    Console.WriteLine($"Installer folder: {installerFolder}");
    Console.WriteLine($"ISO_DRIVE={isoDrive.TrimEnd('\\')}");
    Console.WriteLine($"INSTALLER_FOLDER={installerFolder.TrimEnd('\\')}");
    Console.WriteLine($"MOUNTED_BY_HELPER={(mountResult.MountedByHelper ? "1" : "0")}");
    return 0;
}

static int UnmountIso(string isoPath)
{
    if (!File.Exists(isoPath))
    {
        Console.WriteLine("HELPER_ERROR=ISO_NOT_FOUND_FOR_UNMOUNT");
        Console.WriteLine($"ISO file not found while trying to unmount it: {isoPath}");
        return FileNotFoundExitCode;
    }

    VirtualDiskOperationResult detachResult = VirtualDiskIsoMounter.DetachIso(isoPath);

    if (!detachResult.Success)
    {
        Console.WriteLine("HELPER_ERROR=ISO_UNMOUNT_FAILED");
        Console.WriteLine(detachResult.Message);
        return DetachFailedExitCode;
    }

    Console.WriteLine("ISO_UNMOUNTED=1");
    Console.WriteLine("The VistaSoft ISO was unmounted successfully.");
    return 0;
}

static int VerifyInstaller(string installerPath)
{
    if (!File.Exists(installerPath))
    {
        Console.WriteLine("HELPER_ERROR=INSTALLER_NOT_FOUND_FOR_VERIFICATION");
        Console.WriteLine($"Installer file not found: {installerPath}");
        return FileNotFoundExitCode;
    }

    AuthenticodeVerificationResult verificationResult = AuthenticodeVerifier.VerifyVistaSoftInstaller(installerPath);

    if (!verificationResult.Success)
    {
        Console.WriteLine("HELPER_ERROR=INSTALLER_SIGNATURE_INVALID");
        Console.WriteLine(verificationResult.Message);
        return SignatureVerificationFailedExitCode;
    }

    Console.WriteLine("INSTALLER_SIGNATURE_VALID=1");
    Console.WriteLine($"INSTALLER_SIGNER={verificationResult.SignerName}");
    Console.WriteLine($"Verified VistaSoft installer publisher: {verificationResult.SignerName}");
    return 0;
}

static int UnknownCommand(string command)
{
    Console.WriteLine("HELPER_ERROR=UNKNOWN_COMMAND");
    Console.WriteLine($"Unknown helper command: {command}");
    WriteUsage();
    return UsageExitCode;
}

static void WriteUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  VistaSoftIsoMounter.exe mount <VistaSoft ISO path>");
    Console.WriteLine("  VistaSoftIsoMounter.exe unmount <VistaSoft ISO path>");
    Console.WriteLine("  VistaSoftIsoMounter.exe verify <VistaSoft installer exe path>");
}

internal sealed record VirtualDiskOperationResult(bool Success, string Message);

internal sealed record VirtualDiskMountResult(
    bool Success,
    bool MountedByHelper,
    string? PhysicalDevicePath,
    string Message);

internal static class VirtualDiskIsoMounter
{
    private const uint ErrorSuccess = 0;
    private const uint ErrorInsufficientBuffer = 122;
    private const uint VirtualStorageTypeDeviceIso = 1;
    private const uint VirtualDiskAccessAttachReadOnly = 0x00010000;
    private const uint VirtualDiskAccessDetach = 0x00040000;
    private const uint VirtualDiskAccessGetInfo = 0x00080000;
    private const uint OpenVirtualDiskFlagNone = 0;
    private const uint OpenVirtualDiskVersion1 = 1;
    private const uint AttachVirtualDiskVersion1 = 1;
    private const uint AttachVirtualDiskFlagReadOnly = 0x00000001;
    private const uint AttachVirtualDiskFlagPermanentLifetime = 0x00000004;

    private static readonly Guid VirtualStorageTypeVendorMicrosoft =
        new("EC984AEC-A0F9-47E9-901F-71415A66345B");

    public static VirtualDiskMountResult MountIso(string isoPath)
    {
        uint openResult = OpenIso(
            isoPath,
            VirtualDiskAccessAttachReadOnly | VirtualDiskAccessGetInfo,
            out SafeFileHandle virtualDiskHandle);

        if (openResult != ErrorSuccess)
        {
            virtualDiskHandle.Dispose();
            return new VirtualDiskMountResult(
                false,
                false,
                null,
                $"Could not open the selected ISO as a virtual disk. {FormatError(openResult)}");
        }

        using (virtualDiskHandle)
        {
            string? existingPhysicalDevicePath = TryGetPhysicalDevicePath(virtualDiskHandle, TimeSpan.Zero);

            if (existingPhysicalDevicePath is not null)
            {
                return new VirtualDiskMountResult(
                    true,
                    false,
                    existingPhysicalDevicePath,
                    "The selected ISO was already mounted.");
            }

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

            bool mountedByHelper = attachResult == ErrorSuccess;
            string? physicalDevicePath = TryGetPhysicalDevicePath(virtualDiskHandle, TimeSpan.FromSeconds(30));

            if (physicalDevicePath is null)
            {
                if (mountedByHelper)
                {
                    DetachVirtualDisk(virtualDiskHandle, 0, 0);
                }

                return new VirtualDiskMountResult(
                    false,
                    mountedByHelper,
                    null,
                    attachResult == ErrorSuccess
                        ? "Windows attached the ISO, but its physical CD-ROM device could not be identified."
                        : $"Could not attach the selected ISO. {FormatError(attachResult)}");
            }

            return new VirtualDiskMountResult(
                true,
                mountedByHelper,
                physicalDevicePath,
                mountedByHelper ? "ISO mounted successfully." : "The selected ISO was already mounted.");
        }
    }

    public static VirtualDiskOperationResult DetachIso(string isoPath)
    {
        uint openResult = OpenIso(isoPath, VirtualDiskAccessDetach, out SafeFileHandle virtualDiskHandle);

        if (openResult != ErrorSuccess)
        {
            virtualDiskHandle.Dispose();
            return new VirtualDiskOperationResult(
                false,
                $"Could not open the selected ISO for unmounting. {FormatError(openResult)}");
        }

        using (virtualDiskHandle)
        {
            uint detachResult = DetachVirtualDisk(virtualDiskHandle, 0, 0);

            return detachResult == ErrorSuccess
                ? new VirtualDiskOperationResult(true, "ISO unmounted successfully.")
                : new VirtualDiskOperationResult(false, $"Could not unmount the selected ISO. {FormatError(detachResult)}");
        }
    }

    private static uint OpenIso(string isoPath, uint accessMask, out SafeFileHandle virtualDiskHandle)
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

        return OpenVirtualDisk(
            ref storageType,
            isoPath,
            accessMask,
            OpenVirtualDiskFlagNone,
            ref openParameters,
            out virtualDiskHandle);
    }

    private static string? TryGetPhysicalDevicePath(SafeFileHandle virtualDiskHandle, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow.Add(timeout);

        do
        {
            uint pathSizeInBytes = 1024;
            StringBuilder pathBuilder = new((int)(pathSizeInBytes / sizeof(char)));
            uint result = GetVirtualDiskPhysicalPath(virtualDiskHandle, ref pathSizeInBytes, pathBuilder);

            if (result == ErrorSuccess)
            {
                return pathBuilder.ToString();
            }

            if (result == ErrorInsufficientBuffer && pathSizeInBytes > 0)
            {
                pathBuilder = new StringBuilder((int)(pathSizeInBytes / sizeof(char)) + 1);
                result = GetVirtualDiskPhysicalPath(virtualDiskHandle, ref pathSizeInBytes, pathBuilder);

                if (result == ErrorSuccess)
                {
                    return pathBuilder.ToString();
                }
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(250));
        }
        while (DateTime.UtcNow < deadline);

        return null;
    }

    private static string FormatError(uint errorCode)
    {
        return $"Error {errorCode}: {new Win32Exception(unchecked((int)errorCode)).Message}";
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

    [DllImport("virtdisk.dll")]
    private static extern uint DetachVirtualDisk(
        SafeFileHandle virtualDiskHandle,
        uint flags,
        uint providerSpecificFlags);

    [DllImport("virtdisk.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetVirtualDiskPhysicalPath(
        SafeFileHandle virtualDiskHandle,
        ref uint diskPathSizeInBytes,
        StringBuilder diskPath);

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

internal static class MountedDriveLocator
{
    public static string? FindDriveForPhysicalDevice(string physicalDevicePath)
    {
        string? expectedDevice = GetCdRomDeviceIdentity(physicalDevicePath);

        if (expectedDevice is null)
        {
            return null;
        }

        for (int attempt = 1; attempt <= 120; attempt++)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.CDRom)
                {
                    continue;
                }

                string driveName = drive.Name.TrimEnd('\\');
                StringBuilder targetPath = new(1024);

                if (QueryDosDevice(driveName, targetPath, targetPath.Capacity) == 0)
                {
                    continue;
                }

                if (string.Equals(
                    expectedDevice,
                    GetCdRomDeviceIdentity(targetPath.ToString()),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return drive.RootDirectory.FullName.TrimEnd('\\');
                }
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(250));
        }

        return null;
    }

    internal static string? GetCdRomDeviceIdentity(string? devicePath)
    {
        if (string.IsNullOrWhiteSpace(devicePath))
        {
            return null;
        }

        int cdRomIndex = devicePath.LastIndexOf("cdrom", StringComparison.OrdinalIgnoreCase);

        if (cdRomIndex < 0)
        {
            return null;
        }

        StringBuilder identity = new("cdrom");

        for (int index = cdRomIndex + "cdrom".Length; index < devicePath.Length; index++)
        {
            char candidate = devicePath[index];

            if (!char.IsDigit(candidate))
            {
                break;
            }

            identity.Append(candidate);
        }

        return identity.Length > "cdrom".Length ? identity.ToString() : null;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint QueryDosDevice(
        string deviceName,
        StringBuilder targetPath,
        int maxLength);
}

internal sealed record AuthenticodeVerificationResult(bool Success, string? SignerName, string Message);

internal static class AuthenticodeVerifier
{
    private const uint WinTrustUiNone = 2;
    private const uint WinTrustRevokeNone = 0;
    private const uint WinTrustChoiceFile = 1;
    private const uint WinTrustStateActionIgnore = 0;
    private const uint WinTrustUiContextExecute = 0;

    private static readonly Guid WinTrustActionGenericVerifyV2 =
        new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    public static AuthenticodeVerificationResult VerifyVistaSoftInstaller(string installerPath)
    {
        int trustResult = VerifySignature(installerPath);

        if (trustResult != 0)
        {
            string errorMessage = Marshal.GetExceptionForHR(trustResult)?.Message ?? "Unknown signature error.";
            return new AuthenticodeVerificationResult(
                false,
                null,
                $"The VistaSoft installer does not have a valid trusted digital signature. Error 0x{trustResult:X8}: {errorMessage}");
        }

        string signerName;

        try
        {
#pragma warning disable SYSLIB0057
            using X509Certificate signerCertificate = X509Certificate.CreateFromSignedFile(installerPath);
            using X509Certificate2 signerCertificate2 = new(signerCertificate);
#pragma warning restore SYSLIB0057
            signerName = signerCertificate2.GetNameInfo(X509NameType.SimpleName, false);
        }
        catch (Exception ex)
        {
            return new AuthenticodeVerificationResult(
                false,
                null,
                $"The installer signature was valid, but its publisher could not be read: {ex.Message}");
        }

        if (!IsAllowedVistaSoftPublisher(signerName))
        {
            return new AuthenticodeVerificationResult(
                false,
                signerName,
                $"The installer is signed, but the publisher '{signerName}' is not an approved VistaSoft publisher.");
        }

        return new AuthenticodeVerificationResult(true, signerName, "Installer signature is valid.");
    }

    internal static bool IsAllowedVistaSoftPublisher(string? signerName)
    {
        string normalizedName = NormalizePublisherName(signerName);

        return normalizedName.Contains("air techniques", StringComparison.Ordinal) ||
               normalizedName.Contains("durr dental", StringComparison.Ordinal) ||
               normalizedName.Contains("duerr dental", StringComparison.Ordinal);
    }

    internal static string NormalizePublisherName(string? signerName)
    {
        if (string.IsNullOrWhiteSpace(signerName))
        {
            return string.Empty;
        }

        string decomposedName = signerName.Normalize(NormalizationForm.FormD);
        StringBuilder normalizedName = new(decomposedName.Length);

        foreach (char candidate in decomposedName)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(candidate) != UnicodeCategory.NonSpacingMark)
            {
                normalizedName.Append(char.ToLowerInvariant(candidate));
            }
        }

        return normalizedName.ToString().Normalize(NormalizationForm.FormC);
    }

    private static int VerifySignature(string installerPath)
    {
        IntPtr filePathPointer = IntPtr.Zero;
        IntPtr fileInfoPointer = IntPtr.Zero;
        IntPtr trustDataPointer = IntPtr.Zero;

        try
        {
            filePathPointer = Marshal.StringToCoTaskMemUni(installerPath);

            WinTrustFileInfo fileInfo = new()
            {
                StructSize = (uint)Marshal.SizeOf<WinTrustFileInfo>(),
                FilePath = filePathPointer,
                FileHandle = IntPtr.Zero,
                KnownSubject = IntPtr.Zero,
            };

            fileInfoPointer = Marshal.AllocCoTaskMem(Marshal.SizeOf<WinTrustFileInfo>());
            Marshal.StructureToPtr(fileInfo, fileInfoPointer, false);

            WinTrustData trustData = new()
            {
                StructSize = (uint)Marshal.SizeOf<WinTrustData>(),
                PolicyCallbackData = IntPtr.Zero,
                SipClientData = IntPtr.Zero,
                UIChoice = WinTrustUiNone,
                RevocationChecks = WinTrustRevokeNone,
                UnionChoice = WinTrustChoiceFile,
                FileInfo = fileInfoPointer,
                StateAction = WinTrustStateActionIgnore,
                StateData = IntPtr.Zero,
                UrlReference = IntPtr.Zero,
                ProviderFlags = 0,
                UIContext = WinTrustUiContextExecute,
            };

            trustDataPointer = Marshal.AllocCoTaskMem(Marshal.SizeOf<WinTrustData>());
            Marshal.StructureToPtr(trustData, trustDataPointer, false);

            Guid actionId = WinTrustActionGenericVerifyV2;
            return WinVerifyTrust(new IntPtr(-1), ref actionId, trustDataPointer);
        }
        finally
        {
            if (trustDataPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(trustDataPointer);
            }

            if (fileInfoPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(fileInfoPointer);
            }

            if (filePathPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(filePathPointer);
            }
        }
    }

    [DllImport("wintrust.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern int WinVerifyTrust(
        IntPtr windowHandle,
        ref Guid actionId,
        IntPtr trustData);

    [StructLayout(LayoutKind.Sequential)]
    private struct WinTrustFileInfo
    {
        public uint StructSize;
        public IntPtr FilePath;
        public IntPtr FileHandle;
        public IntPtr KnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WinTrustData
    {
        public uint StructSize;
        public IntPtr PolicyCallbackData;
        public IntPtr SipClientData;
        public uint UIChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public IntPtr FileInfo;
        public uint StateAction;
        public IntPtr StateData;
        public IntPtr UrlReference;
        public uint ProviderFlags;
        public uint UIContext;
    }
}
