using System;
using Windows.UI.Xaml.Controls;

namespace NFC_King.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Tutorial : Page
    {
        public Tutorial()
        {
            this.InitializeComponent();
            // Create a new flip view, add content, 
            // and add a SelectionChanged event handler.
            FlipView flipViewTutorial = new FlipView();
            flipViewTutorial.Items.Add("Item 1");
            flipViewTutorial.Items.Add("Item 2");

            // Add the flip view to a parent container in the visual tree.
            StackTutorial.Children.Add(flipViewTutorial);
        }
    }
}
