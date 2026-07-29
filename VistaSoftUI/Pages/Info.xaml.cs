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
                "0.0.1c",
                "07/29/2026",
                [
                    "Added a Windows installer so the app can be installed on other computers.",
                    "The installer now creates Start Menu and Desktop shortcuts.",
                    "The app opens automatically after setup is finished.",
                    "When installing VistaSoft, the selected ISO now mounts automatically and the install continues without asking the user to open the image manually.",
                    "The VistaSoft options file is still created and used automatically during the install.",
                ]),
            new ChangelogEntry(
                "0.0.1b",
                "07/20/2026",
                [
                    "Added this changelog so users can see what changed between versions.",
                    "Improved the look and feel of the Info page.",
                ]),
            new ChangelogEntry(
                "0.0.1a",
                "07/20/2026",
                [
                    "First working version of the VistaSoft Automated Installer.",
                    "Added the main Install page.",
                    "Added support for importing VistaSoft options files.",
                    "Added the ability to select a VistaSoft ISO file.",
                    "Added the Info page.",
                    "Started work on tools for database copy tasks.",
                ]),
        ];

        public ChangelogEntry LatestChangelogEntry => ChangelogEntries[0];

        public Info()
        {
            InitializeComponent();
        }
    }
}
