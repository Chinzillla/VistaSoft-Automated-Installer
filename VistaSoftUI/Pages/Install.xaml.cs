using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using VistaSoftUI.Models;
using VistaSoftUI.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;

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

        private PickedFileSelection? _selectedOptionsFile;

        private static readonly FilePickerSelection IsoFilePicker = new(
            [".ISO"],
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

            _selectedOptionsFile = selection;
            SelectedOptionsFileTextBlock.Text = selection.FilePath;
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

            _selectedIsoFile = selection;
            SelectedVistaSoftFileTextBlock.Text = selection.FilePath;
        }
    }
}
