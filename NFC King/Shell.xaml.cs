using Intense.Presentation;
using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using NFC_King.Pages;
using NFC_King.Presentation;

namespace NFC_King
{
    public sealed partial class Shell : UserControl
    {
        public Shell()
        {
            this.InitializeComponent();

            var vm = new ShellViewModel();
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Início", PageType = typeof(Tutorial) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Aplicativos", PageType = typeof(Aplicativo) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Chamadas", PageType = typeof(Call) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Email", PageType = typeof(Mail) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Imagem", PageType = typeof(Imagem) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Link Web", PageType = typeof(LinkWeb) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Mapa", PageType = typeof(Map) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Sms", PageType = typeof(Sms) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Configurações do Sistema", PageType = typeof(Shortcuts) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Mídias Sociais", PageType = typeof(SocialMedia) });            
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Texto Simples", PageType = typeof(TxtSimple) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Avançado", PageType = typeof(Advanced) });            


            vm.BottomItems.Add(new NavigationItem { Icon = "", DisplayName = "Configurações", PageType = typeof(SettingsPage) });

            // select the first top item
            vm.SelectedItem = vm.TopItems.First();

            this.ViewModel = vm;
        }

        public ShellViewModel ViewModel { get; private set; }

        public Frame RootFrame
        {
            get
            {
                return this.Frame;
            }
        }
    }
}
