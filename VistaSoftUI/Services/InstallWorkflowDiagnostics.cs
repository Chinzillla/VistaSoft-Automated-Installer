using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VistaSoftUI.Services
{
    public static class InstallWorkflowDiagnostics
    {
        private static readonly IReadOnlyDictionary<int, string> WorkflowErrors =
            new Dictionary<int, string>
            {
                [1] = "The installer helper stopped before completing its task.",
                [2] = "The selected ISO file could not be found.",
                [3] = "Windows could not find a required path or folder.",
                [4] = "The selected ISO did not contain exactly one VistaSoft installer.",
                [5] = "Windows denied access or could not create the temporary installation workspace.",
                [7] = "The VistaSoft staging folder could not be opened.",
                [8] = "The VistaSoft installer could not be copied from the ISO.",
                [64] = "A VistaSoft ISO was not supplied to the install workflow.",
                [65] = "The VistaSoft options were not supplied to the install workflow.",
                [66] = "The VistaSoft options file could not be created.",
                [67] = "The bundled ISO helper is missing from the application installation.",
                [70] = "Windows could not mount the selected VistaSoft ISO.",
                [71] = "Windows mounted the ISO, but the matching VistaSoft drive could not be identified.",
                [72] = "Windows could not unmount the VistaSoft ISO.",
                [73] = "The VistaSoft installer digital signature was missing, invalid, or from an unexpected publisher.",
                [80] = "The verified VistaSoft installer started but reported a failure.",
                [81] = "Windows did not return a result from the VistaSoft installer.",
                [225] = "Windows Security or another antivirus product blocked a file used by the installation.",
                [255] = "The install process returned code 255 without a structured workflow result.",
                [740] = "This operation requires administrator permission.",
                [1223] = "The administrator permission prompt was canceled.",
                [1314] = "The current process does not have the Windows privilege required to mount the ISO.",
                [1602] = "The VistaSoft installation was canceled before it finished.",
                [1603] = "The VistaSoft installer reported a fatal installation error.",
                [1618] = "Another Windows installation is already running.",
            };

        private static readonly IReadOnlyDictionary<int, string> ErrorCodeGuidance =
            new Dictionary<int, string>
            {
                [2] = "Confirm that the selected ISO still exists and can be opened.",
                [3] = "A file or folder path no longer exists. Select the ISO again and retry.",
                [5] = "This is usually a permissions issue. Run the app as administrator and confirm that Windows allows access to the ISO and local AppData folder.",
                [32] = "The file is being used by another process. Close other installers or ISO tools and retry.",
                [87] = "Windows rejected an invalid parameter. The ISO may be damaged or may not be a supported VistaSoft image.",
                [225] = "Open Windows Security > Protection history to see what was blocked. Do not bypass the warning unless the file is confirmed to be the genuine signed VistaSoft installer.",
                [740] = "Close the app and start it with Run as administrator.",
                [1223] = "The Windows administrator prompt was canceled. Retry and approve the prompt to continue.",
                [1314] = "Close the app and start it with Run as administrator so Windows can attach the virtual disk.",
                [1602] = "The installer was canceled by the user or by Windows. Start the install again when ready.",
                [1603] = "Restart Windows, make sure no older VistaSoft installer is running, and review the diagnostic log for the last completed step.",
                [1618] = "Wait for the other installation to finish, or restart Windows if no installer window is visible, then retry.",
                [1641] = "The installation succeeded and Windows has started a restart.",
                [3010] = "The installation succeeded, but Windows must be restarted before VistaSoft is ready.",
            };

        private static readonly Regex NativeErrorPattern = new(
            @"\bError\s+(?<code>\d+)\s*:",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static string CreateFailureMessage(
            int exitCode,
            string standardOutput,
            string standardError,
            string? logPath)
        {
            string summary = CreateExitCodeSummary(exitCode);
            string? detail = GetOutputValue(standardOutput, "WORKFLOW_ERROR_DETAIL");
            string? installerExitCode = GetOutputValue(standardOutput, "VISTASOFT_INSTALLER_EXIT_CODE");

            StringBuilder message = new(summary);

            if (exitCode == 255 || ErrorCodeGuidance.ContainsKey(exitCode))
            {
                AppendCodeGuidance(message, exitCode);
            }

            if (!string.IsNullOrWhiteSpace(installerExitCode))
            {
                message.AppendLine();
                message.Append($"VistaSoft installer code: {installerExitCode}.");

                if (int.TryParse(installerExitCode, out int parsedInstallerExitCode))
                {
                    AppendCodeGuidance(message, parsedInstallerExitCode);
                }
            }

            int? nativeErrorCode = FindNativeErrorCode(standardOutput, standardError);

            if (nativeErrorCode.HasValue &&
                (!int.TryParse(installerExitCode, out int parsedInstallerCode) || parsedInstallerCode != nativeErrorCode.Value))
            {
                message.AppendLine();
                message.Append($"Windows error {nativeErrorCode.Value}.");
                AppendCodeGuidance(message, nativeErrorCode.Value);
            }

            if (!string.IsNullOrWhiteSpace(detail) &&
                !string.Equals(detail, summary, StringComparison.OrdinalIgnoreCase))
            {
                message.AppendLine();
                message.Append(detail);
            }

            string? helperError = GetLastNonEmptyLine(standardError);

            if (!string.IsNullOrWhiteSpace(helperError))
            {
                message.AppendLine();
                message.Append($"Windows reported: {helperError}");
            }

            AppendLogPath(message, logPath);
            return message.ToString();
        }

        public static string CreateSuccessMessage(string standardOutput, string? logPath)
        {
            StringBuilder message = new("VistaSoft installation completed successfully.");
            string? installerExitCode = GetOutputValue(standardOutput, "VISTASOFT_INSTALLER_EXIT_CODE");
            string? warning = GetOutputValue(standardOutput, "WORKFLOW_WARNING");

            if (!string.IsNullOrWhiteSpace(installerExitCode))
            {
                message.AppendLine();
                message.Append($"VistaSoft installer code: {installerExitCode}.");

                if (int.TryParse(installerExitCode, out int parsedInstallerExitCode))
                {
                    AppendCodeGuidance(message, parsedInstallerExitCode);
                }
            }

            if (!string.IsNullOrWhiteSpace(warning))
            {
                message.AppendLine();
                message.Append("Warning: Windows could not automatically unmount the ISO. You can eject it from File Explorer.");
            }

            AppendLogPath(message, logPath);
            return message.ToString();
        }

        public static async Task<string?> TryWriteLogAsync(
            string isoPath,
            int exitCode,
            string standardOutput,
            string standardError)
        {
            try
            {
                string logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VistaSoftAutomatedInstaller",
                    "Logs");
                Directory.CreateDirectory(logDirectory);

                string logPath = Path.Combine(
                    logDirectory,
                    $"install-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.log");
                string logContent = string.Join(
                    Environment.NewLine,
                    [
                        $"Timestamp: {DateTimeOffset.Now:O}",
                        $"ISO: {isoPath}",
                        $"Workflow exit code: {exitCode}",
                        string.Empty,
                        "Standard output:",
                        standardOutput.TrimEnd(),
                        string.Empty,
                        "Standard error:",
                        standardError.TrimEnd(),
                        string.Empty,
                    ]);

                await File.WriteAllTextAsync(logPath, logContent, Encoding.UTF8);
                return logPath;
            }
            catch
            {
                return null;
            }
        }

        internal static string? GetOutputValue(string output, string key)
        {
            string prefix = key + "=";
            string? matchingLine = output
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Reverse()
                .FirstOrDefault(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            return matchingLine?[prefix.Length..].Trim();
        }

        private static string CreateExitCodeSummary(int exitCode)
        {
            if (WorkflowErrors.TryGetValue(exitCode, out string? knownError))
            {
                return knownError;
            }

            string? windowsDescription = GetWindowsErrorDescription(exitCode);

            return windowsDescription is null
                ? $"The installation stopped with code {exitCode}. This code is not defined by Windows or the automated workflow; use the diagnostic log when contacting support."
                : $"The installation stopped with Windows code {exitCode}: {windowsDescription}";
        }

        private static void AppendCodeGuidance(StringBuilder message, int errorCode)
        {
            if (errorCode == 255)
            {
                message.Append(" Code 255 is an undocumented VistaSoft installer failure. The diagnostic log identifies the completed mount, signature, and options steps so support can see where VistaSoft stopped.");
                return;
            }

            if (ErrorCodeGuidance.TryGetValue(errorCode, out string? guidance))
            {
                message.Append(' ');
                message.Append(guidance);
                return;
            }

            string? windowsDescription = GetWindowsErrorDescription(errorCode);

            if (!string.IsNullOrWhiteSpace(windowsDescription))
            {
                message.Append($" Windows reports: {windowsDescription}");
            }
        }

        private static int? FindNativeErrorCode(string standardOutput, string standardError)
        {
            MatchCollection matches = NativeErrorPattern.Matches(standardOutput + Environment.NewLine + standardError);

            for (int index = matches.Count - 1; index >= 0; index--)
            {
                if (int.TryParse(matches[index].Groups["code"].Value, out int errorCode))
                {
                    return errorCode;
                }
            }

            return null;
        }

        private static string? GetWindowsErrorDescription(int errorCode)
        {
            string description = new Win32Exception(errorCode).Message.Trim();

            if (description.Length == 0 ||
                description.StartsWith("Unknown error", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(description, $"Error {errorCode}", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return description.EndsWith('.') ? description : description + ".";
        }

        private static string? GetLastNonEmptyLine(string value)
        {
            return value
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .LastOrDefault();
        }

        private static void AppendLogPath(StringBuilder message, string? logPath)
        {
            if (string.IsNullOrWhiteSpace(logPath))
            {
                message.AppendLine();
                message.Append("The diagnostic log could not be written.");
                return;
            }

            message.AppendLine();
            message.Append($"Diagnostic log: {logPath}");
        }
    }
}
