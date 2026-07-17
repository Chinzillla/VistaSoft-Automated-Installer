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
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using VistaSoftUI.Pages;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace VistaSoftUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
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

            if (tag == "install")
            {
                ContentFrame.Navigate(typeof(Install), null, new SuppressNavigationTransitionInfo());
            }
            else if (tag == "uninstall")
            {
                ContentFrame.Navigate(typeof(Uninstall), null, new SuppressNavigationTransitionInfo());
            }
            else if (tag == "info")
            {
                ContentFrame.Navigate(typeof(Info), null, new SuppressNavigationTransitionInfo());
            }
        }
    }
}
