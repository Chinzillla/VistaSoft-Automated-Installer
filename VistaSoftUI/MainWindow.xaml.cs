using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using VistaSoftUI.Pages;

namespace VistaSoftUI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ContentFrame.Navigate(typeof(Install), null, new SuppressNavigationTransitionInfo());
            RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
        }

        private void RootNavigation_SelectionChanged(
            NavigationView sender,
            NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is not NavigationViewItem selectedItem)
            {
                return;
            }

            string? tag = selectedItem.Tag?.ToString();
            Type? pageType = tag switch
            {
                "install" => typeof(Install),
                "uninstall" => typeof(Uninstall),
                "info" => typeof(Info),
                "dbcopy" => typeof(Dbcopy),
                _ => null,
            };

            if (pageType is null)
            {
                return;
            }

            ContentFrame.Navigate(pageType, null, new SuppressNavigationTransitionInfo());
        }
    }
}
