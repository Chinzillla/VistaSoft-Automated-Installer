using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using VistaSoftUI.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VistaSoftUI.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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

        public Info()
        {
            InitializeComponent();
        }
    }
}
