using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using VistaSoftUI.Models;

namespace VistaSoftUI.Services
{
    public static class FilePickerService
    {
        public static async Task<PickedFileSelection?> PickFileAsync(
            Window owner,
            FilePickerSelection selection)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(selection);

            FileOpenPicker picker = new(owner.AppWindow.Id)
            {
                SuggestedStartLocation = selection.SuggestedStartLocation,
                CommitButtonText = selection.ButtonText,
                ViewMode = PickerViewMode.List,
            };

            picker.FileTypeChoices.Add(selection.FileTypeLabel, [.. selection.FileTypes]);

            PickFileResult? result = await picker.PickSingleFileAsync();

            if (result is null)
            {
                return null;
            }

            if (!IsAllowedFileType(result.Path, selection))
            {
                return null;
            }

            return CreatePickedFileSelection(result.Path);
        }

        public static async Task<PickedFileSelection?> PickSaveFileAsync(
            Window owner,
            FilePickerSelection selection)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(selection);

            FileSavePicker picker = new(owner.AppWindow.Id)
            {
                SuggestedStartLocation = selection.SuggestedStartLocation,
                CommitButtonText = selection.ButtonText,
            };

            if (!string.IsNullOrWhiteSpace(selection.SuggestedFileName))
            {
                picker.SuggestedFileName = selection.SuggestedFileName;
            }

            if (!string.IsNullOrWhiteSpace(selection.DefaultFileExtension))
            {
                picker.DefaultFileExtension = selection.DefaultFileExtension;
            }

            picker.FileTypeChoices.Add(selection.FileTypeLabel, [.. selection.FileTypes]);

            PickFileResult? result = await picker.PickSaveFileAsync();

            if (result is null)
            {
                return null;
            }

            if (!IsAllowedFileType(result.Path, selection))
            {
                return null;
            }

            return CreatePickedFileSelection(result.Path);
        }

        private static PickedFileSelection CreatePickedFileSelection(string path)
        {
            return new PickedFileSelection(path, Path.GetFileName(path));
        }

        private static bool IsAllowedFileType(string path, FilePickerSelection selection)
        {
            string selectedExtension = Path.GetExtension(path);
            bool allowsAllFiles = selection.FileTypes.Contains("*");

            return allowsAllFiles || selection.FileTypes.Any(fileType =>
                string.Equals(selectedExtension, fileType, StringComparison.OrdinalIgnoreCase));
        }
    }
}
