using GalaSoft.MvvmLight.Messaging;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using spotify.playlist.merger.Models;
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace spotify.playlist.merger.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        private readonly PlaylistDialog playlistDialog;
        private readonly AddToPlaylistDialog addToPlaylistDialog;
        private UnfollowDialog _unfollowDialog;

        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
            Window.Current.SetTitleBar(DragGrid);
            Initialize();
            playlistDialog = new PlaylistDialog();
            _unfollowDialog = new UnfollowDialog();
            addToPlaylistDialog = new AddToPlaylistDialog();
            RegisterMessenger();
        }

        private void Initialize()
        {
            // Reset app back to normal.
            StatusBarExtensions.SetIsVisible(this, false);

            ApplicationViewExtensions.SetTitle(this, string.Empty);

            var lightGreyBrush = (Color)Application.Current.Resources["Status-bar-foreground"];
            var statusBarColor = (Color)Application.Current.Resources["Status-bar-color"];
            var brandColor = (Color)Application.Current.Resources["BrandColorThemeColor"];

            ApplicationViewExtensions.SetTitle(this, "Playlist Merge");
            StatusBarExtensions.SetBackgroundOpacity(this, 0.8);
            TitleBarExtensions.SetButtonBackgroundColor(this, statusBarColor);
            TitleBarExtensions.SetButtonForegroundColor(this, lightGreyBrush);
            TitleBarExtensions.SetBackgroundColor(this, statusBarColor);
            TitleBarExtensions.SetForegroundColor(this, lightGreyBrush);
            TitleBarExtensions.SetButtonBackgroundColor(this, Colors.Transparent);
            TitleBarExtensions.SetButtonHoverBackgroundColor(this, brandColor);
            TitleBarExtensions.SetButtonHoverForegroundColor(this, Colors.White);
        }

        private void RegisterMessenger()
        {
            Messenger.Default.Register<DialogManager>(this, DialogManage);
            Messenger.Default.Register<MessengerHelper>(this, HandleMessengerHelper);
        }

        private async void DialogManage(DialogManager manager)
        {
            switch (manager.Type)
            {
                case DialogType.Clone:
                case DialogType.EditPlaylist:
                case DialogType.Merge:
                    if (manager.Action == DialogAction.Show)
                        await playlistDialog.ShowAsync();
                    else
                        playlistDialog.Hide();
                    break;
                case DialogType.Unfollow:
                    if (manager.Action == DialogAction.Show)
                    {
                        if (_unfollowDialog == null) _unfollowDialog = new UnfollowDialog();
                        await _unfollowDialog.ShowAsync();
                    }
                    else if (_unfollowDialog != null)
                        _unfollowDialog.Hide();
                    break;
                case DialogType.AddToPlaylist:
                    if (manager.Action == DialogAction.Show)
                    {
                        await addToPlaylistDialog.ShowAsync();
                    }
                    else if (addToPlaylistDialog != null)
                        addToPlaylistDialog.Hide();
                    break;
            }
        }

        private void HandleMessengerHelper(MessengerHelper helper)
        {
            switch (helper.Action)
            {
                case MessengerAction.ShowSettings:
                    try
                    {
                        FlyoutBase.ShowAttachedFlyout(SettingsButton);
                    }
                    catch (Exception)
                    {

                    }
                    break;
            }
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
