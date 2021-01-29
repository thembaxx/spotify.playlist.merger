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
        private readonly PlaylistDialog playlistDialog = null;
        private ContentDialog _dialog = null;

        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
            Window.Current.SetTitleBar(DragGrid);
            Initialize();
            playlistDialog = new PlaylistDialog();
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
                case DialogType.Merge:
                    if (manager.Action == DialogAction.Show)
                        await playlistDialog.ShowAsync();
                    else
                        playlistDialog.Hide();
                    break;
                case DialogType.Unfollow:
                    _dialog = new ContentDialog
                    {
                        Title = manager.Title,
                        Content = manager.Message,
                        PrimaryButtonText = manager.PrimaryButtonText,
                        SecondaryButtonText = manager.SecondaryButtonText,
                        DefaultButton = ContentDialogButton.Primary
                    };

                    Messenger.Default.Send(new DialogResult(DialogType.Unfollow, await _dialog.ShowAsync(), manager.Item));
                    break;
            }
        }

        private void HandleMessengerHelper(MessengerHelper helper)
        {
            switch (helper.Action)
            {
                case MessengerAction.ShowSettings:
                    FlyoutBase.ShowAttachedFlyout(SettingsButton);
                    break;
            }
        }
    }
}
