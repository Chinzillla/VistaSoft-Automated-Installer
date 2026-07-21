using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VistaSoftUI.Models;

namespace VistaSoftUI.Services
{
    public static class OptionsFileService
    {
        private static readonly IReadOnlyList<string> ExportKeyOrder =
        [
            OptionsFileKeys.Mode,
            OptionsFileKeys.UnattendedModeUi,
            OptionsFileKeys.AutoSetup,
            OptionsFileKeys.ConnectMode,
            OptionsFileKeys.OperationMode,
            OptionsFileKeys.CompanyName,
            OptionsFileKeys.Address,
            OptionsFileKeys.InstallAllScannerPlugins,
            OptionsFileKeys.InstallVistaScanClassicPlugin,
            OptionsFileKeys.InstallCameraPlugin,
            OptionsFileKeys.InstallVistaRay7Plugin,
            OptionsFileKeys.InstallTwainPlugin,
        ];

        private static readonly IReadOnlySet<string> OptionalStringKeys = new HashSet<string>(
            [
                OptionsFileKeys.CompanyName,
                OptionsFileKeys.Address,
            ],
            StringComparer.OrdinalIgnoreCase);

        public static async Task<VistaSoftInstallOptions> ReadAsync(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            string[] lines = await File.ReadAllLinesAsync(filePath);

            return ParseOptions(lines);
        }

        public static async Task WriteAsync(string filePath, VistaSoftInstallOptions options)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            ArgumentNullException.ThrowIfNull(options);

            string content = CreateFileContent(options);

            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        }

        public static string CreateFileContent(VistaSoftInstallOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            Dictionary<string, string> optionValues = CreateSupportedOptionValues(options);
            IEnumerable<string> lines = ExportKeyOrder
                .Where(key => ShouldExportOption(key, optionValues[key]))
                .Select(key => $"{key}={optionValues[key]}");

            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        private static VistaSoftInstallOptions ParseOptions(IEnumerable<string> lines)
        {
            VistaSoftInstallOptions options = new();

            foreach (string line in lines)
            {
                if (!TryParseOptionLine(line, out string key, out string value))
                {
                    continue;
                }

                ApplyOption(options, key, value);
            }

            return options;
        }

        private static void ApplyOption(VistaSoftInstallOptions options, string key, string value)
        {
            if (StringEquals(key, OptionsFileKeys.AutoSetup))
            {
                options.AutoSetup = ParseBoolean(value);
            }
            else if (StringEquals(key, OptionsFileKeys.ConnectMode))
            {
                options.ConnectMode = ParseBoolean(value);
            }
            else if (StringEquals(key, OptionsFileKeys.OperationMode))
            {
                options.OperationMode = VistaSoftOperationModes.NormalizeOrDefault(value);
            }
            else if (StringEquals(key, OptionsFileKeys.CompanyName))
            {
                options.PracticeName = value;
            }
            else if (StringEquals(key, OptionsFileKeys.Address))
            {
                options.PracticeAddress = value;
            }
            else if (StringEquals(key, OptionsFileKeys.InstallAllScannerPlugins))
            {
                options.InstallScanXPlugin = ParseBoolean(value);
            }
            else if (StringEquals(key, OptionsFileKeys.InstallVistaScanClassicPlugin))
            {
                options.InstallScanXClassicPlugin = ParsePluginEnabled(value);
            }
            else if (StringEquals(key, OptionsFileKeys.InstallCameraPlugin))
            {
                options.InstallCamXPlugin = ParseBoolean(value);
            }
            else if (StringEquals(key, OptionsFileKeys.InstallVistaRay7Plugin))
            {
                options.InstallSensorXPlugin = ParseBoolean(value);
            }
            else if (StringEquals(key, OptionsFileKeys.InstallTwainPlugin))
            {
                options.InstallTwainPlugin = ParseBoolean(value);
            }
        }

        private static Dictionary<string, string> CreateSupportedOptionValues(VistaSoftInstallOptions options)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [OptionsFileKeys.Mode] = OptionsFileValues.Unattended,
                [OptionsFileKeys.UnattendedModeUi] = OptionsFileValues.None,
                [OptionsFileKeys.AutoSetup] = ToInstallerBoolean(options.AutoSetup ?? true),
                [OptionsFileKeys.ConnectMode] = ToInstallerBoolean(options.ConnectMode),
                [OptionsFileKeys.OperationMode] = VistaSoftOperationModes.NormalizeOrDefault(options.OperationMode),
                [OptionsFileKeys.CompanyName] = options.PracticeName ?? string.Empty,
                [OptionsFileKeys.Address] = FlattenMultilineValue(options.PracticeAddress),
                [OptionsFileKeys.InstallAllScannerPlugins] = ToInstallerBoolean(options.InstallScanXPlugin),
                [OptionsFileKeys.InstallVistaScanClassicPlugin] = options.InstallScanXClassicPlugin == true ? "2" : "0",
                [OptionsFileKeys.InstallCameraPlugin] = ToInstallerBoolean(options.InstallCamXPlugin),
                [OptionsFileKeys.InstallVistaRay7Plugin] = ToInstallerBoolean(options.InstallSensorXPlugin),
                [OptionsFileKeys.InstallTwainPlugin] = ToInstallerBoolean(options.InstallTwainPlugin),
            };
        }

        private static bool ShouldExportOption(string key, string value)
        {
            return !OptionalStringKeys.Contains(key) || !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryParseOptionLine(string line, out string key, out string value)
        {
            key = string.Empty;
            value = string.Empty;

            string candidate = line.TrimStart();

            if (candidate.Length == 0 || candidate.StartsWith('#'))
            {
                return false;
            }

            int equalsIndex = candidate.IndexOf('=');

            if (equalsIndex <= 0)
            {
                return false;
            }

            key = candidate[..equalsIndex].Trim();
            value = candidate[(equalsIndex + 1)..].Trim();

            return !string.IsNullOrWhiteSpace(key);
        }

        private static string FlattenMultilineValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return string.Join(
                " ",
                value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        private static bool? ParseBoolean(string value)
        {
            return value.Trim().ToLowerInvariant() switch
            {
                "1" or "true" or "yes" => true,
                "0" or "false" or "no" => false,
                _ => null,
            };
        }

        private static bool? ParsePluginEnabled(string value)
        {
            return int.TryParse(value.Trim(), out int pluginValue)
                ? pluginValue > 0
                : ParseBoolean(value);
        }

        private static string ToInstallerBoolean(bool? value)
        {
            return value == true ? "1" : "0";
        }

        private static bool StringEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static class OptionsFileKeys
        {
            public const string Mode = "mode";
            public const string UnattendedModeUi = "unattendedmodeui";
            public const string AutoSetup = "autosetup";
            public const string ConnectMode = "connectmode";
            public const string OperationMode = "operationmode";
            public const string CompanyName = "companyname";
            public const string Address = "address";
            public const string InstallAllScannerPlugins = "InstallAllScannerPlugins";
            public const string InstallVistaScanClassicPlugin = "InstallVistaScanClassicPlugin";
            public const string InstallCameraPlugin = "InstallCameraPlugin";
            public const string InstallVistaRay7Plugin = "InstallVistaRay7Plugin";
            public const string InstallTwainPlugin = "InstallTwainPlugin";
        }

        private static class OptionsFileValues
        {
            public const string Unattended = "unattended";
            public const string None = "none";
        }
    }
}
