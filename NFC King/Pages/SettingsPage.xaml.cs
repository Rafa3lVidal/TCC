using System;
using Windows.UI.Xaml.Controls;
using NFC_King.Presentation;

namespace NFC_King.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            this.ViewModel = new SettingsViewModel();
        }

        public SettingsViewModel ViewModel { get; }

        private void BtnTutorial_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Tutorial));
        }
    }
}
