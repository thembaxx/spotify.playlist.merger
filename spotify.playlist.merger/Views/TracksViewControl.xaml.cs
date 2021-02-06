using GalaSoft.MvvmLight.Messaging;
using spotify.playlist.merger.Models;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace spotify.playlist.merger.Views
{
    public sealed partial class TracksViewControl : UserControl
    {
        public TracksViewControl()
        {
            this.InitializeComponent();
            Messenger.Default.Register<MessengerHelper>(this, (messenger) =>
            {
                switch (messenger.Action)
                {
                    case MessengerAction.ScrollToItem:
                        if (messenger.Target == TargetView.Tracks)
                        {
                            if (messenger.Item == null && Content.Items != null) messenger.Item = Content.Items.FirstOrDefault();
                            Content.ScrollIntoView(messenger.Item, ScrollIntoViewAlignment.Leading);
                        }
                        break;
                }
            });
        }

        private void MediaItem_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            FlyoutShowOptions options = new FlyoutShowOptions
            {
                Placement = FlyoutPlacementMode.RightEdgeAlignedTop,
                Position = e.GetPosition(senderElement),
                ShowMode = FlyoutShowMode.Standard
            };
            flyoutBase.ShowAt(senderElement, options);
        }
    }
}
