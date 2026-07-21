using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VistaSoftUI.Models;
using VistaSoftUI.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VistaSoftUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Install : Page
    {
        private static readonly FilePickerSelection OptionsFilePicker = new(
            [".options"],
            "Import Options",
            "VistaSoft options (*.options)");

        private static readonly FilePickerSelection OptionsSavePicker = new(
            [".options"],
            "Export Options",
            "VistaSoft options (*.options)",
            suggestedFileName: "VistaSoft-windows-installer");

        private static readonly FilePickerSelection IsoFilePicker = new(
            [".iso"],
            "Open VistaSoft ISO",
            "VistaSoft ISO (*.iso)");

        private PickedFileSelection? _selectedIsoFile;

        public Install()
        {
            InitializeComponent();
        }

        private async void ImportOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.MainWindow is not Window owner)
            {
                SelectedOptionsFileTextBlock.Text = "Unable to open the file picker.";
                return;
            }

            PickedFileSelection? selection = await FilePickerService.PickFileAsync(owner, OptionsFilePicker);

            if (selection is null)
            {
                SelectedOptionsFileTextBlock.Text = "No options file selected.";
                return;
            }

            try
            {
                VistaSoftInstallOptions options = await OptionsFileService.ReadAsync(selection.FilePath);
                ApplyOptionsToForm(options);

                SelectedOptionsFileTextBlock.Text = $"Imported options: {selection.FilePath}";
            }
            catch (Exception ex)
            {
                SelectedOptionsFileTextBlock.Text = $"Unable to import options file: {ex.Message}";
            }
        }

        private async void ExportOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.MainWindow is not Window owner)
            {
                SelectedOptionsFileTextBlock.Text = "Unable to open the file picker.";
                return;
            }

            PickedFileSelection? selection = await FilePickerService.PickSaveFileAsync(owner, OptionsSavePicker);

            if (selection is null)
            {
                SelectedOptionsFileTextBlock.Text = "Options export canceled.";
                return;
            }

            VistaSoftInstallOptions options = ReadOptionsFromForm();

            try
            {
                await OptionsFileService.WriteAsync(selection.FilePath, options);

                SelectedOptionsFileTextBlock.Text = $"Exported options: {selection.FilePath}";
            }
            catch (Exception ex)
            {
                SelectedOptionsFileTextBlock.Text = $"Unable to export options file: {ex.Message}";
            }
        }

        private async void OpenVistaSoftIsoButton_Click(object sender, RoutedEventArgs e)
        {
            await PickVistaSoftIsoAsync();
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            PickedFileSelection? selectedIsoFile = _selectedIsoFile;

            if (selectedIsoFile is null)
            {
                InstallStatusTextBlock.Text = "Select a VistaSoft ISO to continue.";

                if (!await PickVistaSoftIsoAsync())
                {
                    InstallStatusTextBlock.Text = "Install canceled. No ISO selected.";
                    return;
                }

                selectedIsoFile = _selectedIsoFile;
            }

            if (selectedIsoFile is null)
            {
                InstallStatusTextBlock.Text = "Install canceled. No ISO selected.";
                return;
            }

            if (!File.Exists(selectedIsoFile.FilePath))
            {
                InstallStatusTextBlock.Text = $"ISO file no longer exists: {selectedIsoFile.FilePath}";
                return;
            }

            InstallButton.IsEnabled = false;
            InstallStatusTextBlock.Text = "Mounting selected VistaSoft ISO...";

            try
            {
                string scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "mount_vistasoft.bat");

                if (!File.Exists(scriptPath))
                {
                    InstallStatusTextBlock.Text = $"Mount script not found: {scriptPath}";
                    return;
                }

                ProcessStartInfo startInfo = new()
                {
                    FileName = "cmd.exe",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                startInfo.ArgumentList.Add("/d");
                startInfo.ArgumentList.Add("/c");
                startInfo.ArgumentList.Add(scriptPath);
                startInfo.ArgumentList.Add(selectedIsoFile.FilePath);

                using Process? process = Process.Start(startInfo);

                if (process is null)
                {
                    InstallStatusTextBlock.Text = "Unable to start the mount script.";
                    return;
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                InstallStatusTextBlock.Text = process.ExitCode == 0
                    ? $"ISO mounted successfully.{Environment.NewLine}{output.Trim()}"
                    : $"Unable to mount ISO. Exit code: {process.ExitCode}{Environment.NewLine}{output.Trim()}{Environment.NewLine}{error.Trim()}";
            }
            catch (Exception ex)
            {
                InstallStatusTextBlock.Text = $"Unable to mount ISO: {ex.Message}";
            }
            finally
            {
                InstallButton.IsEnabled = true;
            }
        }

        private async Task<bool> PickVistaSoftIsoAsync()
        {
            if (App.MainWindow is not Window owner)
            {
                SelectedVistaSoftFileTextBlock.Text = "Unable to open the file picker.";
                return false;
            }

            PickedFileSelection? selection = await FilePickerService.PickFileAsync(owner, IsoFilePicker);

            if (selection is null)
            {
                SelectedVistaSoftFileTextBlock.Text = "No ISO file selected.";
                return false;
            }

            _selectedIsoFile = selection;
            SelectedVistaSoftFileTextBlock.Text = selection.FilePath;
            InstallStatusTextBlock.Text = string.Empty;

            return true;
        }

        private VistaSoftInstallOptions ReadOptionsFromForm()
        {
            return new VistaSoftInstallOptions
            {
                AutoSetup = true,
                ConnectMode = ConnectModeCheckBox.IsChecked == true,
                OperationMode = GetSelectedOperationMode(),
                PracticeName = PracticeNameTextBox.Text,
                PracticeAddress = PracticeAddressTextBox.Text,
                InstallScanXPlugin = ScanXCheckBox.IsChecked == true,
                InstallScanXClassicPlugin = ScanXClassicCheckBox.IsChecked == true,
                InstallCamXPlugin = CamXCheckBox.IsChecked == true,
                InstallSensorXPlugin = SensorXCheckBox.IsChecked == true,
                InstallTwainPlugin = TwainCheckBox.IsChecked == true,
            };
        }

        private void ApplyOptionsToForm(VistaSoftInstallOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (options.ConnectMode.HasValue)
            {
                ConnectModeCheckBox.IsChecked = options.ConnectMode.Value;
            }

            if (!string.IsNullOrWhiteSpace(options.OperationMode))
            {
                SetSelectedOperationMode(options.OperationMode);
            }

            if (options.PracticeName is not null)
            {
                PracticeNameTextBox.Text = options.PracticeName;
            }

            if (options.PracticeAddress is not null)
            {
                PracticeAddressTextBox.Text = options.PracticeAddress;
            }

            if (options.InstallScanXPlugin.HasValue)
            {
                ScanXCheckBox.IsChecked = options.InstallScanXPlugin.Value;
            }

            if (options.InstallScanXClassicPlugin.HasValue)
            {
                ScanXClassicCheckBox.IsChecked = options.InstallScanXClassicPlugin.Value;
            }

            if (options.InstallCamXPlugin.HasValue)
            {
                CamXCheckBox.IsChecked = options.InstallCamXPlugin.Value;
            }

            if (options.InstallSensorXPlugin.HasValue)
            {
                SensorXCheckBox.IsChecked = options.InstallSensorXPlugin.Value;
            }

            if (options.InstallTwainPlugin.HasValue)
            {
                TwainCheckBox.IsChecked = options.InstallTwainPlugin.Value;
            }
        }

        private string GetSelectedOperationMode()
        {
            if (ClientRadioButton.IsChecked == true)
            {
                return VistaSoftOperationModes.Client;
            }

            if (ServerRadioButton.IsChecked == true)
            {
                return VistaSoftOperationModes.Server;
            }

            return VistaSoftOperationModes.Local;
        }

        private void SetSelectedOperationMode(string operationMode)
        {
            string normalizedOperationMode = VistaSoftOperationModes.NormalizeOrDefault(operationMode);

            LocalRadioButton.IsChecked = normalizedOperationMode == VistaSoftOperationModes.Local;
            ClientRadioButton.IsChecked = normalizedOperationMode == VistaSoftOperationModes.Client;
            ServerRadioButton.IsChecked = normalizedOperationMode == VistaSoftOperationModes.Server;
        }
    }
}
