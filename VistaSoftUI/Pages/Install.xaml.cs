using System;
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
            if (App.MainWindow is not Window owner)
            {
                SelectedVistaSoftFileTextBlock.Text = "Unable to open the file picker.";
                return;
            }

            PickedFileSelection? selection = await FilePickerService.PickFileAsync(owner, IsoFilePicker);

            if (selection is null)
            {
                SelectedVistaSoftFileTextBlock.Text = "No ISO file selected.";
                return;
            }

            SelectedVistaSoftFileTextBlock.Text = selection.FilePath;
        }

        private VistaSoftInstallOptions ReadOptionsFromForm()
        {
            return new VistaSoftInstallOptions
            {
                AutoSetup = true,
                ConnectMode = ConnectModeCheckBox.IsChecked == true,
                OperationMode = GetSelectedOperationMode(),
                PracticeName = PracticeNameTextBox.Text,
                Street = StreetTextBox.Text,
                City = CityTextBox.Text,
                State = StateTextBox.Text,
                Zip = ZipTextBox.Text,
                Country = GetSelectedCountry(),
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

            if (options.Street is not null)
            {
                StreetTextBox.Text = options.Street;
            }

            if (options.City is not null)
            {
                CityTextBox.Text = options.City;
            }

            if (options.State is not null)
            {
                StateTextBox.Text = options.State;
            }

            if (options.Zip is not null)
            {
                ZipTextBox.Text = options.Zip;
            }

            if (!string.IsNullOrWhiteSpace(options.Country))
            {
                SetSelectedCountry(options.Country);
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

        private string? GetSelectedCountry()
        {
            return CountryComboBox.SelectedItem is ComboBoxItem selectedItem
                ? selectedItem.Content?.ToString()
                : null;
        }

        private void SetSelectedCountry(string country)
        {
            foreach (object item in CountryComboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem
                    && string.Equals(
                        comboBoxItem.Content?.ToString(),
                        country,
                        StringComparison.OrdinalIgnoreCase))
                {
                    CountryComboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }
        }
    }
}
