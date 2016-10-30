using System;
using Windows.UI.Xaml.Controls;

namespace NFC_King.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Page1 : Page
    {
        public Page1()
        {
            this.InitializeComponent();
        }

        private void BtnSms_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            //this.Frame.Navigate(typeof(Email));
        }

        private void BtnLigacao_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            //this.Frame.Navigate(typeof(Call));
        }
    }
}
