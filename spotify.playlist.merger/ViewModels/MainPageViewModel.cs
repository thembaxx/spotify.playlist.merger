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

namespace spotify.playlist.merger.ViewModels
{
    public class MainPageViewModel : ViewModelBase
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
                await DataSource.Current.GetPlaylists();
                SelectedPlaylistCollection.CollectionChanged += SelectedPlaylistCollection_CollectionChanged;

            }
            else if (Profile != null)
            {
                //show message that this needs a premium account
                Helpers.DisplayDialog("Premium account required", "A premium account is required to use the features provided by this app.");
                //logout
                DataSource.Current.Logout();
                Profile = null;
                ShowLogin = true;
            } else if (string.IsNullOrEmpty(ClientID) || string.IsNullOrEmpty(ClientSecret))
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
            {
                ShowLogin = true;
            }
            IsLoading = false;
        }

        private void Refresh(bool init = true)
        {
            IsLoading = true;

            Profile = null;
            ClientID = null;
            ClientSecret = null;
            ResetCreatePlaylistDialog();
            UnfollowAfterMerge = false;
            _playlistCollectionCopy.Clear();
            _filteredPlaylistCollection.Clear();
            AdvancedCollectionView = null;
            TotalTracks = 0;
            SelectedPlaylistCollection.CollectionChanged -= SelectedPlaylistCollection_CollectionChanged;
            SelectedPlaylistCollection.Clear();
            PlaylistCategoryCollection.Clear();
            SelectedPlaylistCategory = null;
            if(init) Initialize();

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
                        MergePlaylist();
                        break;
                    case DialogType.Unfollow:
                        IsRightBarBusy = true;
                        var playlistIds = SelectedPlaylistCollection.Select(c => c.Id).ToList();
                        var succesfulItems = await DataSource.Current.UnfollowSpotifyPlaylist(playlistIds);
                        if (succesfulItems != null)
                        {
                            foreach (var id in succesfulItems)
                            {
                                var match = playlistIds.Find(c => c == id);
                                if (match != null)
                                {
                                    playlistIds.Remove(match);
                                    var item = SelectedPlaylistCollection.Where(c => c.Id == id).FirstOrDefault();
                                    if(item != null) SelectedPlaylistCollection.Remove(item);

                                    item = _playlistCollectionCopy.Where(c => c.Id == id).FirstOrDefault();
                                    if (item != null) _playlistCollectionCopy.Remove(item);
                                    
                                    item = _filteredPlaylistCollection.Where(c => c.Id == id).FirstOrDefault();
                                    if (item != null) _filteredPlaylistCollection.Remove(item);

                                    var it = AdvancedCollectionView.Where(c => ((Playlist)c).Id == id).FirstOrDefault();
                                    if (it != null) AdvancedCollectionView.Remove(it);
                                    UpdateItemPosition();
                                }
                            }
                        }
                        
                        if (playlistIds.Count > 0)
                        {
                            //what to do about items that failed?
                        }
                        IsRightBarBusy = false;
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

        private string _base64JpegData;

        private bool _showImageSizeError;
        public bool ShowImageSizeError
        {
            get => _showImageSizeError;
            set
            {
                _showImageSizeError = value;
                RaisePropertyChanged("ShowImageSizeError");
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

        private bool _isRightBarBusy;
        public bool IsRightBarBusy
        {
            get => _isRightBarBusy;
            set
            {
                _isRightBarBusy = value;
                RaisePropertyChanged("IsRightBarBusy");
            }
        }

        private bool _hasSelectedPlaylists;
        public bool HasSelectedPlaylists
        {
            get { return _hasSelectedPlaylists; }
            set { _hasSelectedPlaylists = value; RaisePropertyChanged("HasSelectedPlaylists"); }
        }

        private bool _unfollowAfterMerge;
        public bool UnfollowAfterMerge
        {
            get => _unfollowAfterMerge;
            set
            {
                _unfollowAfterMerge = value;
                RaisePropertyChanged("UnfollowAfterMerge");
            }
        }

        private string _newPlaylistName;
        public string NewPlaylistName
        {
            get => _newPlaylistName;
            set
            {
                _newPlaylistName = value;
                RaisePropertyChanged("NewPlaylistName");
            }
        }

        private string _newPlaylistDescription;
        public string NewPlaylistDescription
        {
            get => _newPlaylistDescription;
            set
            {
                _newPlaylistDescription = value;
                RaisePropertyChanged("NewPlaylistDescription");
            }
        }

        private string _mergeImageFilePath;
        public string MergeImageFilePath
        {
            get => _mergeImageFilePath;
            set
            {
                _mergeImageFilePath = value;
                RaisePropertyChanged("MergeImageFilePath");
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

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                RaisePropertyChanged("SearchText");
                FilterPlaylistCollectionView();
            }
        }

        private bool _canPlay;
        public bool CanPlay
        {
            get => _canPlay;
            set
            {
                _canPlay = value;
                RaisePropertyChanged("CanPlay");
            }
        }

        private bool _canMerge;
        public bool CanMerge
        {
            get => _canMerge;
            set
            {
                _canMerge = value;
                RaisePropertyChanged("CanMerge");
            }
        }

        private int _totalTracks;
        public int TotalTracks
        {
            get => _totalTracks;
            set
            {
                _totalTracks = value;
                RaisePropertyChanged("TotalTracks");
            }
        }

        private PlaylistCategory _selectedPlaylistCategory;
        public PlaylistCategory SelectedPlaylistCategory
        {
            get { return _selectedPlaylistCategory; }
            set
            {
                _selectedPlaylistCategory = value;
                RaisePropertyChanged("SelectedPlaylistCategory");
                FilterPlaylistCollectionView(true);
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

        private RelayCommand _mergeCommand;
        public RelayCommand MergeCommand
        {
            get
            {
                if (_mergeCommand == null)
                {
                    _mergeCommand = new RelayCommand(() =>
                    {
                        MergePlaylist();
                    });
                }
                return _mergeCommand;
            }
        }

        private RelayCommand _showMergeDialogCommand;
        public RelayCommand ShowMergeDialogCommand
        {
            get
            {
                if (_showMergeDialogCommand == null)
                {
                    _showMergeDialogCommand = new RelayCommand(() =>
                    {
                        if(SelectedPlaylistCollection != null && SelectedPlaylistCollection.Count > 0)
                        {
                            Messenger.Default.Send(new DialogManager
                            {
                                Type = DialogType.Merge,
                                Action = DialogAction.Show,
                            });
                        }
                    });
                }
                return _showMergeDialogCommand;
            }
        }

        private RelayCommand _cancelMergeCommand;
        public RelayCommand CancelMergeCommand
        {
            get
            {
                if (_cancelMergeCommand == null)
                {
                    _cancelMergeCommand = new RelayCommand(() =>
                    {
                        ResetCreatePlaylistDialog();
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.Merge,
                            Action = DialogAction.Hide
                        });
                    });
                }
                return _cancelMergeCommand;
            }
        }

        private RelayCommand _mergeImagePickerCommand;
        public RelayCommand MergeImagePickerCommand
        {
            get
            {
                if (_mergeImagePickerCommand == null)
                {
                    _mergeImagePickerCommand = new RelayCommand(async () =>
                    {
                        IsLoading = true;

                        var file = await Helpers.ImageFileDialogPicker();
                        if (file != null)
                        {
                            //max size 4mb
                            var props = await file.GetBasicPropertiesAsync();
                            var sizeInMB = props.Size / 1024 / 1024;
                            if (sizeInMB < 4)
                            {
                                ShowImageSizeError = false;
                                //load Base64JpegData
                                MergeImageFilePath = file.Path;
                                _base64JpegData = await Helpers.ImageToBase64(file);
                            }
                            else
                            {
                                ShowImageSizeError = true;
                                MergeImageFilePath = null;
                                _base64JpegData = null;
                            }
                        }

                        IsLoading = false;
                    });
                }
                return _mergeImagePickerCommand;
            }
        }

        private RelayCommand _mergeSelectedPlaylistCommand;
        public RelayCommand MergeSelectedPlaylistCommand
        {
            get
            {
                if (_mergeSelectedPlaylistCommand == null)
                {
                    _mergeSelectedPlaylistCommand = new RelayCommand(() =>
                    {
                        MergePlaylist();
                    });
                }
                return _mergeSelectedPlaylistCommand;
            }
        }

        private RelayCommand<Playlist> _playlistItemClickCommand;
        public RelayCommand<Playlist> PlaylistItemClickCommand
        {
            get
            {
                if (_playlistItemClickCommand == null)
                {
                    _playlistItemClickCommand = new RelayCommand<Playlist>((item) =>
                    {
                        if (!item.IsSelected && SelectedPlaylistCollection.Where(c => c.Id == item.Id).FirstOrDefault() == null)
                            SelectedPlaylistCollection.Add(item);
                        else if (item.IsSelected && SelectedPlaylistCollection.Where(c => c.Id == item.Id).FirstOrDefault() != null)
                            SelectedPlaylistCollection.Remove(item);

                        foreach (var it in SelectedPlaylistCollection)
                        {
                            it.PositionAlt = SelectedPlaylistCollection.IndexOf(it) + 1;
                        }
                    });
                }
                return _playlistItemClickCommand;
            }
        }

        private RelayCommand<Playlist> _unselectPlaylistCommand;
        public RelayCommand<Playlist> UnselectPlaylistCommand
        {
            get
            {
                if (_unselectPlaylistCommand == null)
                {
                    _unselectPlaylistCommand = new RelayCommand<Playlist>((item) =>
                    {
                        if (SelectedPlaylistCollection.Where(c => c.Id == item.Id).FirstOrDefault() != null)
                            SelectedPlaylistCollection.Remove(item);

                        foreach (var it in SelectedPlaylistCollection)
                        {
                            it.PositionAlt = SelectedPlaylistCollection.IndexOf(it) + 1;
                        }
                    });
                }
                return _unselectPlaylistCommand;
            }
        }

        private RelayCommand _clearSelectedCommand;
        public RelayCommand ClearSelectedCommand
        {
            get
            {
                if (_clearSelectedCommand == null)
                {
                    _clearSelectedCommand = new RelayCommand(() =>
                    {
                        var selected = SelectedPlaylistCollection.ToList();
                        foreach (var item in selected)
                        {
                            item.IsSelected = false;
                            item.PositionAlt = 0;
                            SelectedPlaylistCollection.Remove(item);
                        }
                    });
                }
                return _clearSelectedCommand;
            }
        }

        private RelayCommand _playSelectedCommand;
        public RelayCommand PlaySelectedCommand
        {
            get
            {
                if (_playSelectedCommand == null)
                {
                    _playSelectedCommand = new RelayCommand(async() =>
                    {
                        IsRightBarBusy = true;

                        var playlistIds = SelectedPlaylistCollection.Select(c => c.Id).ToList();
                        var tracks = new List<string>();
                        foreach (var item in playlistIds)
                        {
                            var items = await DataSource.Current.GetTrackIds(item);
                            if (items != null)
                            {
                                foreach (var id in items)
                                {
                                    if (tracks.Find(c => c == id) == null) tracks.Add(id);
                                }
                            }
                        }
                        await DataSource.Current.PlaySpotifyMedia(tracks);


                        IsRightBarBusy = false;
                    });
                }
                return _playSelectedCommand;
            }
        }

        private RelayCommand _unfollowSelectedCommand;
        public RelayCommand UnfollowSelectedCommand
        {
            get
            {
                if (_unfollowSelectedCommand == null)
                {
                    _unfollowSelectedCommand = new RelayCommand(() =>
                    {
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.Unfollow,
                            Action = DialogAction.Show,
                            Title = "Unfollow playlists?",
                            Message = "Are you sure you want to unfollow the selected playlists?",
                            PrimaryButtonText = "Unfollow",
                            SecondaryButtonText = "Cancel"
                        });
                    });
                }
                return _unfollowSelectedCommand;
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

        private AdvancedCollectionView _advancedCollectionView;
        public AdvancedCollectionView AdvancedCollectionView
        {
            get => _advancedCollectionView;
            set
            {
                _advancedCollectionView = value;
                RaisePropertyChanged("AdvancedCollectionView");
            }
        }

        ObservableCollection<Playlist> _selectedPlaylistCollection = new ObservableCollection<Playlist>();
        public ObservableCollection<Playlist> SelectedPlaylistCollection
        {
            get => _selectedPlaylistCollection;
            set
            {
                _selectedPlaylistCollection = value;
                RaisePropertyChanged("SelectedPlaylistCollection");
            }
        }

        private void SelectedPlaylistCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            HasSelectedPlaylists = (SelectedPlaylistCollection.Count > 0);
            CanPlay = (SelectedPlaylistCollection.Count > 0);
            CanMerge = (SelectedPlaylistCollection.Count > 1);

            if(e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is Playlist playlist)
                    {
                        playlist.IsSelected = false;
                        playlist.PositionAlt = 0;
                    }
                }

                //update position
                //foreach (var item in e.OldItems)
                //{
                //    if (item is Playlist playlist)
                //        playlist.PositionAlt = SelectedPlaylistCollection.IndexOf(playlist) + 1;
                //}
            }

            if(e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is Playlist playlist)
                    {
                        playlist.IsSelected = true;
                        
                    }
                }
            }
        }

        ObservableCollection<PlaylistCategory> _playlistCategoryCollection;
        public ObservableCollection<PlaylistCategory> PlaylistCategoryCollection
        {
            get => _playlistCategoryCollection;
            set { _playlistCategoryCollection = value; RaisePropertyChanged("PlaylistCategoryCollection"); }
        }

        readonly List<Playlist> _playlistCollectionCopy = new List<Playlist>();
        private readonly List<Playlist> _filteredPlaylistCollection = new List<Playlist>();

        ObservableCollection<string> _filterCollection = new ObservableCollection<string>();
        public ObservableCollection<string> FilterCollection
        {
            get => _filterCollection;
            set
            {
                _filterCollection = value;
                RaisePropertyChanged("FilterCollection");
            }
        }

        #endregion

        #region Methods

        private void PopulateFilterCollection()
        {
            PlaylistCategoryCollection = new ObservableCollection<PlaylistCategory>();
            var items = PlaylistCategory.GetCategoryItems();
            if (items != null)
            {
                var item = items.Where(c => c.Type == PlaylistCategoryType.MyPlaylist).FirstOrDefault();
                item.Title = Profile.Title;
                foreach (var it in items) PlaylistCategoryCollection.Add(it);

                SelectedPlaylistCategory = PlaylistCategoryCollection.Where(c => c.Type == PlaylistCategoryType.All).FirstOrDefault();
            }

        }

        public void FilterPlaylistCollectionView(bool isSwitchingCategory = false)
        {
            // isSwitchingCategory allows us to use AdvancedCollectionView.DeferRefresh() 
            //  if only switching category because using it when searching makes the autocomplete lose focus everytime
            // the text changes

            if (AdvancedCollectionView == null)
                return;

            //make sure the filtered items are clear
            _filteredPlaylistCollection.Clear();


            if (!isSwitchingCategory)
            {
                if (!string.IsNullOrEmpty(SearchText))
                {
                    if (SelectedPlaylistCategory.Type == PlaylistCategoryType.All)
                    {
                        AdvancedCollectionView.Filter = c => (((Playlist)c).Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Playlist)c).Description).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Playlist)c).Owner.Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase);

                        _filteredPlaylistCollection.AddRange(_playlistCollectionCopy.Where(c => c.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        c.Description.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        c.Owner.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)));
                    }
                    else
                    {
                        AdvancedCollectionView.Filter = c => ((((Playlist)c).Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Playlist)c).Description).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Playlist)c).Owner.Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)) &&
                        ((Playlist)c).Type == SelectedPlaylistCategory.Type;

                        _filteredPlaylistCollection.AddRange(_playlistCollectionCopy.Where(c => (c.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        c.Description.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        c.Owner.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))));
                    }
                }
                else
                {
                    if (SelectedPlaylistCategory.Type == PlaylistCategoryType.All)
                    {
                        AdvancedCollectionView.Filter = c => c != null;
                        _filteredPlaylistCollection.AddRange(_playlistCollectionCopy);
                    }
                    else
                    {
                        AdvancedCollectionView.Filter = c => ((Playlist)c).Type == SelectedPlaylistCategory.Type;
                        _filteredPlaylistCollection.AddRange(_playlistCollectionCopy);
                    }
                }

                AdvancedCollectionView.RefreshFilter();
            }
            else
            {
                using (AdvancedCollectionView.DeferRefresh())
                {
                    if (!string.IsNullOrEmpty(SearchText))
                    {
                        if (SelectedPlaylistCategory.Type == PlaylistCategoryType.All)
                        {
                            AdvancedCollectionView.Filter = c => (((Playlist)c).Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            (((Playlist)c).Description).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            (((Playlist)c).Owner.Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase);

                            _filteredPlaylistCollection.AddRange(_playlistCollectionCopy.Where(c => c.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            c.Description.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            c.Owner.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)));
                        }
                        else
                        {
                            AdvancedCollectionView.Filter = c => ((((Playlist)c).Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            (((Playlist)c).Description).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            (((Playlist)c).Owner.Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)) &&
                            ((Playlist)c).Type == SelectedPlaylistCategory.Type;

                            _filteredPlaylistCollection.AddRange(_playlistCollectionCopy.Where(c => (c.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            c.Description.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            c.Owner.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))));
                        }
                    }
                    else
                    {
                        if (SelectedPlaylistCategory.Type == PlaylistCategoryType.All)
                        {
                            AdvancedCollectionView.Filter = c => c != null;
                            _filteredPlaylistCollection.AddRange(_playlistCollectionCopy);
                        }
                        else
                        {
                            AdvancedCollectionView.Filter = c => ((Playlist)c).Type == SelectedPlaylistCategory.Type;
                            _filteredPlaylistCollection.AddRange(_playlistCollectionCopy);
                        }
                    }
                }
            }

            UpdateItemPosition();
        }

        private async void MergePlaylist()
        {
            IsLoading = true;

            //display a dialog for the time being to get name
            if (!string.IsNullOrEmpty(NewPlaylistName) && SelectedPlaylistCollection.Count > 0)
            {
                var playlist = await DataSource.Current.MergeSpotifyPlaylists(NewPlaylistName, NewPlaylistDescription, SelectedPlaylistCollection, _base64JpegData);
                if (playlist != null)
                {
                    if (UnfollowAfterMerge)
                    {
                        await DataSource.Current.UnfollowSpotifyPlaylist(SelectedPlaylistCollection.Select(c => c.Id));
                        UnfollowAfterMerge = false;
                    }

                    Messenger.Default.Send(new DialogManager
                    {
                        Type = DialogType.Merge,
                        Action = DialogAction.Hide
                    });

                    ResetCreatePlaylistDialog();
                    var selected = SelectedPlaylistCollection.ToList();
                    foreach (var item in selected)
                    {
                        item.IsSelected = false;
                        SelectedPlaylistCollection.Remove(item);
                    }

                    //add to first position, scroll to top
                    AdvancedCollectionView.Insert(0, playlist);
                    _playlistCollectionCopy.Add(playlist);
                    UpdateItemPosition();
                }
            }

            IsLoading = false;
        }

        private void ResetCreatePlaylistDialog()
        {
            NewPlaylistName = null;
            NewPlaylistDescription = null;
            _base64JpegData = null;
            MergeImageFilePath = null;
        }

        public void AddToCollection(List<Playlist> playlists)
        {
            if (AdvancedCollectionView == null)
            {
                AdvancedCollectionView = new AdvancedCollectionView(playlists, true);
            }
            else
            {
                using (AdvancedCollectionView.DeferRefresh())
                {
                    foreach (var item in playlists)
                    {
                        AdvancedCollectionView.Add(item);
                    }
                }
            }

            _playlistCollectionCopy.AddRange(playlists);
            TotalTracks = _playlistCollectionCopy.Sum(c => c.Count);
            UpdateItemPosition();
        }

        private void UpdateItemPosition()
        {
            foreach (var item in AdvancedCollectionView)
            {
                if(item is Playlist playlist)
                {
                    playlist.Position = AdvancedCollectionView.IndexOf(item) + 1;
                }
            }
        }

        #endregion
    }
}
