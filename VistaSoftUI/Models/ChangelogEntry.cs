using System.Collections.Generic;

namespace VistaSoftUI.Models
{
    public sealed record ChangelogEntry(
        string Version,
        string ReleaseDate,
        IReadOnlyList<string> Changes)
    {
        public string VersionLabel => $"Version {Version}";
        public string DateLabel => $"Date: {ReleaseDate}";
    }
}
