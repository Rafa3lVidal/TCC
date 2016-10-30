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
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Mapa", PageType = typeof(Map) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Comunicação", PageType = typeof(Page1) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Facilidades", PageType = typeof(Page3) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Link Web", PageType = typeof(LinkWeb) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Email", PageType = typeof(Mail) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Ligação", PageType = typeof(Call) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Atalho Configs", PageType = typeof(Map) });
            vm.TopItems.Add(new NavigationItem { Icon = "", DisplayName = "Mapa", PageType = typeof(Map) });


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
