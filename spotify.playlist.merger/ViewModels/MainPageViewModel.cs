using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using spotify.playlist.merger.Data;
using spotify.playlist.merger.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;

namespace spotify.playlist.merger.ViewModels
{
    public partial class MainPageViewModel : ViewModelBase
    {
        public static MainPageViewModel Current = null;

        public MainPageViewModel()
        {
            Current = this;
            RegisterMessenger();
            Initialize();
        }

        private async void Initialize()
        {
            IsLoading = true;

            ClientID = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
            ClientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
            await DataSource.Current.Initialize();
            Profile = await DataSource.Current.GetProfile();
            if (Profile != null)
            {
                ShowLogin = false;
                PopulateFilterCollection();
                SelectedPlaylistCollection.CollectionChanged += SelectedPlaylistCollection_CollectionChanged;
                SelectedTracks.CollectionChanged += SelectedTracks_CollectionChanged;
                SelectedAddToPlaylist.CollectionChanged += SelectedAddToPlaylist_CollectionChanged;

                IsLoading = false;
                await LoadPlaylistsAsync();

                //check if user follows any spotify playlists, if none remove from category
                if (!_playlistCollectionCopy.Any(c => c.Type == PlaylistCategoryType.Spotify))
                    PlaylistCategoryCollection.Remove(PlaylistCategoryCollection.Where(c => c.Type == PlaylistCategoryType.Spotify).FirstOrDefault());
            }
            else if (string.IsNullOrEmpty(ClientID) || string.IsNullOrEmpty(ClientSecret))
            {
                //show settings
                Messenger.Default.Send(new MessengerHelper
                {
                    Action = MessengerAction.ShowSettings,
                });
                //check client id and secret
                ShowLogin = true;
            }
            else
                ShowLogin = true;

            IsLoading = false;
        }

        private void Refresh(bool init = true)
        {
            IsLoading = true;

            Profile = null;
            ClientID = null;
            ClientSecret = null;
            ResetPlaylistDialog();
            UnfollowAfterMerge = false;
            _playlistCollectionCopy.Clear();
            AdvancedCollectionView = null;
            TotalTracks = 0;
            SelectedPlaylistCollection.CollectionChanged -= SelectedPlaylistCollection_CollectionChanged;
            SelectedPlaylistCollection.Clear();
            if (PlaylistCategoryCollection != null) PlaylistCategoryCollection.Clear();
            SelectedPlaylistCategory = null;
            ResetTracksView();
            if (init) Initialize();

            IsLoading = false;
        }

        private void RegisterMessenger()
        {
            Messenger.Default.Register<DialogResult>(this, ManageDialogResult);
            Messenger.Default.Register<MessengerHelper>(this, HandleMessengerHelper);
        }

        private void HandleMessengerHelper(MessengerHelper obj)
        {
            switch (obj.Action)
            {
                case MessengerAction.IsLoading:
                    IsLoading = (bool)obj.Item;
                    break;
            }
        }

        private async void ManageDialogResult(DialogResult result)
        {
            if (result.ResultType == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                switch (result.Type)
                {
                    case DialogType.Merge:
                        MergePlaylist(SelectedPlaylistCollection);
                        break;
                    case DialogType.Unfollow:
                        await UnfollowPlaylists();
                        break;
                }
            }
        }

        #region Fields

        private bool _showLogin;
        public bool ShowLogin
        {
            get => _showLogin;
            set
            {
                _showLogin = value;
                RaisePropertyChanged("ShowLogin");
            }
        }

        private string _clientSecret;
        public string ClientSecret
        {
            get => _clientSecret;
            set
            {
                _clientSecret = value;
                RaisePropertyChanged("ClientSecret");
            }
        }

        private string _clientID;
        public string ClientID
        {
            get => _clientID;
            set
            {
                _clientID = value;
                RaisePropertyChanged("ClientID");
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                RaisePropertyChanged("IsLoading");
            }
        }

        private bool _isPlaylistsLoading;
        public bool IsPlaylistsLoading
        {
            get => _isPlaylistsLoading;
            set
            {
                _isPlaylistsLoading = value;
                RaisePropertyChanged("IsPlaylistsLoading");
            }
        }

        private User _profile;
        public User Profile
        {
            get => _profile;
            set
            {
                _profile = value;
                RaisePropertyChanged("Profile");
            }
        }

        #endregion

        #region Commands

        private RelayCommand _saveCredentialsCommand;
        public RelayCommand SaveCredentialsCommand
        {
            get
            {
                if (_saveCredentialsCommand == null)
                {
                    _saveCredentialsCommand = new RelayCommand(() =>
                    {
                        if (!string.IsNullOrEmpty(ClientID))
                        {
                            Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_ID", ClientID);
                        }

                        if (!string.IsNullOrEmpty(ClientSecret))
                        {
                            Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_SECRET", ClientSecret);
                        }
                    });
                }
                return _saveCredentialsCommand;
            }
        }

        private RelayCommand<MediaItemBase> _playItemCommand;
        public RelayCommand<MediaItemBase> PlayItemCommand
        {
            get
            {
                if (_playItemCommand == null)
                {
                    _playItemCommand = new RelayCommand<MediaItemBase>(async (item) =>
                    {
                        IsLoading = true;

                        if (item is Track track)
                            await PlayTrack(track, ActivePlaylist, TrackSearchText);
                        else
                            await DataSource.Current.PlaybackMediaItem(item, 0);

                        IsLoading = false;
                    });
                }
                return _playItemCommand;
            }
        }

        private RelayCommand _refreshCommand;
        public RelayCommand RefreshCommand
        {
            get
            {
                if (_refreshCommand == null)
                {
                    _refreshCommand = new RelayCommand(() =>
                    {
                        Refresh();
                    });
                }
                return _refreshCommand;
            }
        }

        private RelayCommand _logoutCommand;
        public RelayCommand LogoutCommand
        {
            get
            {
                if (_logoutCommand == null)
                {
                    _logoutCommand = new RelayCommand(() =>
                    {
                        DataSource.Current.Logout();
                        Refresh();
                        ShowLogin = true;
                    });
                }
                return _logoutCommand;
            }
        }

        private RelayCommand _connectSpotifyCommand;
        public RelayCommand ConnectSpotifyCommand
        {
            get
            {
                if (_connectSpotifyCommand == null)
                {
                    _connectSpotifyCommand = new RelayCommand(() =>
                    {
                        IsLoading = true;
                        Initialize();
                        IsLoading = false;
                    });
                }
                return _connectSpotifyCommand;
            }
        }

        #endregion

        #region Collections

        public ObservableCollection<Sorting> TracksSortList { get; } = new ObservableCollection<Sorting>(Sorting._tracksSortList);
        public ObservableCollection<Sorting> PlaylistSortList { get; } = new ObservableCollection<Sorting>(Sorting._playlistSortList);

        ObservableCollection<PlaylistCategory> _playlistCategoryCollection;
        public ObservableCollection<PlaylistCategory> PlaylistCategoryCollection
        {
            get => _playlistCategoryCollection;
            set { _playlistCategoryCollection = value; RaisePropertyChanged("PlaylistCategoryCollection"); }
        }

        #endregion

        #region Methods

        private void UpdateItemIndex(AdvancedCollectionView collectionView)
        {
            if (collectionView == null) return;
            for (int i = 0; i < collectionView.Count; i++)
                ((MediaItemBase)collectionView[i]).IndexA = i + 1;
        }

        private void RemoveItems(IEnumerable<string> playlistIds)
        {
            if (playlistIds == null || playlistIds.Count() == 0)
                return;

            Playlist item;

            foreach (var id in playlistIds)
            {
                item = _playlistCollectionCopy.Where(c => c.Id == id).FirstOrDefault();
                if (item != null) _playlistCollectionCopy.Remove(item);

                item = SelectedPlaylistCollection.Where(c => c.Id == id).FirstOrDefault();
                if(item != null)
                {
                    SelectedPlaylistCollection.Remove(item);
                }
            }

            using (AdvancedCollectionView.DeferRefresh())
            {
                foreach (var id in playlistIds)
                {
                    var it = AdvancedCollectionView.Where(c => ((Playlist)c).Id == id).FirstOrDefault();
                    if (it != null) AdvancedCollectionView.Remove(it);
                }
            }
            UpdateItemIndex(AdvancedCollectionView);
        }

        #endregion

        #region Notifications

        private NotificationHelper _notification;
        public NotificationHelper Notification
        {
            get => _notification;
            set
            {
                _notification = value;
                RaisePropertyChanged("Notification");
            }
        }

        private bool _isNotificationOpen;
        public bool IsNotificationOpen
        {
            get => _isNotificationOpen;
            set
            {
                _isNotificationOpen = value;
                RaisePropertyChanged("IsNotificationOpen");
                if (!value) StopTimer();
            }
        }

        private void ShowNotification(NotificationType type, string message)
        {
            Notification = new NotificationHelper
            {
                Type = type,
                Text = message
            };
            IsNotificationOpen = true;
            DispatcherTimerSetup();
        }

        DispatcherTimer dispatcherTimer;
        int timesTicked = 1;
        readonly int timesToTick = 6;

        public void DispatcherTimerSetup()
        {
            StopTimer();
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        void DispatcherTimer_Tick(object sender, object e)
        {
            timesTicked++;
            if (timesTicked > timesToTick)
            {
                IsNotificationOpen = false;
                dispatcherTimer.Stop();
            }
        }

        void StopTimer()
        {
            timesTicked = 1;
            if (dispatcherTimer != null)
            {
                dispatcherTimer.Stop();
                dispatcherTimer.Tick -= DispatcherTimer_Tick;
            }
        }

        #endregion
    }
}
