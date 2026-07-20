using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Windows.Storage.Pickers;

namespace VistaSoftUI.Models
{
    public sealed class FilePickerSelection
    {
        public FilePickerSelection(
            string[] fileTypes,
            string buttonText,
            string? fileTypeLabel = null,
            PickerLocationId suggestedStartLocation = PickerLocationId.DocumentsLibrary)
        {
            ArgumentNullException.ThrowIfNull(fileTypes);
            ArgumentException.ThrowIfNullOrWhiteSpace(buttonText);

            FileTypes = fileTypes
                .Select(NormalizeFileType)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (FileTypes.Count == 0)
            {
                throw new ArgumentException("At least one file type is required.", nameof(fileTypes));
            }

            ButtonText = buttonText;
            FileTypeLabel = string.IsNullOrWhiteSpace(fileTypeLabel)
                ? CreateDefaultFileTypeLabel(FileTypes)
                : fileTypeLabel;
            SuggestedStartLocation = suggestedStartLocation;
        }

        public IReadOnlyList<string> FileTypes { get; }
        public string ButtonText { get; }
        public string FileTypeLabel { get; }
        public PickerLocationId SuggestedStartLocation { get; }

        private static string NormalizeFileType(string fileType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileType);

            string trimmedFileType = fileType.Trim();

            if (trimmedFileType == "*")
            {
                return trimmedFileType;
            }

            return trimmedFileType.StartsWith('.')
                ? trimmedFileType
                : $".{trimmedFileType}";
        }

        private static string CreateDefaultFileTypeLabel(IReadOnlyList<string> fileTypes)
        {
            if (fileTypes.Contains("*"))
            {
                return "All files (*.*)";
            }

            string extensions = string.Join("; ", fileTypes.Select(fileType => $"*{fileType}"));
            return $"Supported files ({extensions})";
        }
    }
}
