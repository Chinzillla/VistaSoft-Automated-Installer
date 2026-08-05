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
                "0.0.2",
                "08/05/2026",
                [
                    "Install errors now explain the likely cause and what the user can do next.",
                    "Added clear messages for permission problems, missing paths, antivirus blocks, canceled administrator prompts, and other installations already in progress.",
                    "VistaSoft restart-required results are now treated as successful installations.",
                ]),
            new ChangelogEntry(
                "0.0.1",
                "08/05/2026",
                [
                    "Added a Windows installer so the app can be installed on other computers.",
                    "Desktop and Start Menu shortcuts are now available to every user on the computer.",
                    "The app now confirms that it is using the exact ISO selected by the user, then unmounts it automatically.",
                    "VistaSoft installers are checked for a valid Air Techniques or Duerr Dental digital signature before they are opened.",
                    "Imported options are preserved instead of silently changing or removing settings.",
                    "Install failures now show a clear explanation and save a diagnostic log, including VistaSoft installer code 255.",
                    "Improved upgrades so a rebuilt installer correctly replaces an earlier installation.",
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
