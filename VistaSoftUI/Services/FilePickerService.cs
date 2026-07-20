using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using VistaSoftUI.Models;

namespace VistaSoftUI.Services
{
    public sealed class FilePickerService
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

            string selectedExtension = Path.GetExtension(result.Path);
            bool allowsAllFiles = selection.FileTypes.Contains("*");
            bool isAllowedFileType = allowsAllFiles || selection.FileTypes.Any(fileType =>
                string.Equals(selectedExtension, fileType, StringComparison.OrdinalIgnoreCase));

            if (!isAllowedFileType)
            {
                return null;
            }

            return new PickedFileSelection(result.Path, Path.GetFileName(result.Path));
        }
    }
}
