using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using VistaSoftUI.Models;

namespace VistaSoftUI.Pages
{
    public sealed partial class Info : Page
    {
        public IReadOnlyList<ChangelogEntry> ChangelogEntries { get; } =
        [
            // Add newest changelog entries at the top of this list.
            new ChangelogEntry(
                "0.0.1b",
                "07/20/2026",
                [
                    "Added a Changelog",
                    "Updated the styling of the scrolling bars",
                ]),
            new ChangelogEntry(
                "0.0.1a",
                "07/20/2026",
                [
                    "Initial release of VistaSoft Automated Installer",
                    "Created the Install page components",
                    "Added import options and ISO browse button support",
                    "Created the Info page",
                    "Added a database copier tool page",
                ]),
        ];

        public ChangelogEntry LatestChangelogEntry => ChangelogEntries[0];

        public Info()
        {
            InitializeComponent();
        }
    }
}
