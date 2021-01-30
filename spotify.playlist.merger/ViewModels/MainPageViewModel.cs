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
using System.Threading.Tasks;

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
                await Helpers.DisplayDialog("Premium account required", "A premium account is required to use the features provided by this app.");
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
            ResetPlaylistDialog();
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
                        await UnfollowPlaylists();
                        break;
                }
            }
        }

        #region Fields

        private bool _isDialogBusy;
        public bool IsDialogBusy
        {
            get => _isDialogBusy;
            set
            {
                _isDialogBusy = value;
                RaisePropertyChanged("IsDialogBusy");
            }
        }

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

        private RelayCommand<Playlist> _playlistItemClickCommand;
        public RelayCommand<Playlist> PlaylistItemClickCommand
        {
            get
            {
                if (_playlistItemClickCommand == null)
                {
                    _playlistItemClickCommand = new RelayCommand<Playlist>((item) =>
                    {
                        //show playlist items
                    });
                }
                return _playlistItemClickCommand;
            }
        }

        private RelayCommand<Playlist> _toggleSelectedPlaylistCommand;
        public RelayCommand<Playlist> ToggleSelectedPlaylistCommand
        {
            get
            {
                if (_toggleSelectedPlaylistCommand == null)
                {
                    _toggleSelectedPlaylistCommand = new RelayCommand<Playlist>((item) =>
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
                return _toggleSelectedPlaylistCommand;
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

        #endregion

        #region Methods

        public void AddToCollection(List<Playlist> playlists)
        {
            if (AdvancedCollectionView == null)
            {
                AdvancedCollectionView = new AdvancedCollectionView(playlists, true);
            }
            else
            {
                foreach (var item in playlists)
                {
                    AdvancedCollectionView.Add(item);
                }
                //using (AdvancedCollectionView.DeferRefresh())
                //{
                    
                //}
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

        private void RemoveItems(IEnumerable<string> playlistIds)
        {
            if (playlistIds == null || playlistIds.Count() == 0)
                return;

            Playlist item;

            foreach (var id in playlistIds)
            {
                item = _playlistCollectionCopy.Where(c => c.Id == id).FirstOrDefault();
                if (item != null) _playlistCollectionCopy.Remove(item);

                item = _filteredPlaylistCollection.Where(c => c.Id == id).FirstOrDefault();
                if (item != null) _filteredPlaylistCollection.Remove(item);
            }

            using (AdvancedCollectionView.DeferRefresh())
            {
                foreach (var id in playlistIds)
                {
                    var it = AdvancedCollectionView.Where(c => ((Playlist)c).Id == id).FirstOrDefault();
                    if (it != null) AdvancedCollectionView.Remove(it);
                }
            }
            UpdateItemPosition();
        }

        #endregion

        #region Sorting & Filtering 

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

            TotalTracks = _filteredPlaylistCollection.Sum(c => c.Count);
            UpdateItemPosition();
        }

        #endregion

        #region PlaylistDialog

        private string _playlistDialogTitle;
        public string PlaylistDialogTitle
        {
            get => _playlistDialogTitle;
            set
            {
                _playlistDialogTitle = value;
                RaisePropertyChanged("PlaylistDialogTitle");
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

        private string _playlistDialogName;
        public string PlaylistDialogName
        {
            get => _playlistDialogName;
            set
            {
                _playlistDialogName = value;
                RaisePropertyChanged("PlaylistDialogName");
            }
        }

        private string _playlistDialogDescription;
        public string PlaylistDialogDescription
        {
            get => _playlistDialogDescription;
            set
            {
                _playlistDialogDescription = value;
                RaisePropertyChanged("PlaylistDialogDescription");
            }
        }

        private string _playlistDialogImagePath;
        public string PlaylistDialogImagePath
        {
            get => _playlistDialogImagePath;
            set
            {
                _playlistDialogImagePath = value;
                RaisePropertyChanged("PlaylistDialogImagePath");
            }
        }

        private bool _isMergeMode;
        public bool IsMergeMode
        {
            get => _isMergeMode;
            set
            {
                _isMergeMode = value;
                RaisePropertyChanged("IsMergeMode");
            }
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                RaisePropertyChanged("IsEditMode");
            }
        }

        private bool _isCloneMode;
        public bool IsCloneMode
        {
            get => _isCloneMode;
            set
            {
                _isCloneMode = value;
                RaisePropertyChanged("IsCloneMode");
            }
        }

        private Playlist _currentPlaylist;
        public Playlist CurrentPlaylist
        {
            get => _currentPlaylist;
            set
            {
                _currentPlaylist = value;
                RaisePropertyChanged("CurrentPlaylist");
            }
        }

        private RelayCommand _cancelPlaylistDialogCommand;
        public RelayCommand CancelPlaylistDialogCommand
        {
            get
            {
                if (_cancelPlaylistDialogCommand == null)
                {
                    _cancelPlaylistDialogCommand = new RelayCommand(() =>
                    {
                        ResetPlaylistDialog();
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.Merge,
                            Action = DialogAction.Hide
                        });
                    });
                }
                return _cancelPlaylistDialogCommand;
            }
        }

        private RelayCommand _playlistDialogImagePickerCommand;
        public RelayCommand PlaylistDialogImagePickerCommand
        {
            get
            {
                if (_playlistDialogImagePickerCommand == null)
                {
                    _playlistDialogImagePickerCommand = new RelayCommand(async () =>
                    {
                        IsDialogBusy = true;

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
                                PlaylistDialogImagePath = file.Path;
                                _base64JpegData = await Helpers.ImageToBase64(file);
                            }
                            else
                            {
                                ShowImageSizeError = true;
                                PlaylistDialogImagePath = null;
                                _base64JpegData = null;
                            }
                        }

                        IsDialogBusy = false;
                    });
                }
                return _playlistDialogImagePickerCommand;
            }
        }

        private void ShowPlaylistDialog(DialogType dialogType)
        {
            IsMergeMode = false;
            IsEditMode = false;
            IsCloneMode = false;

            switch (dialogType)
            {
                case DialogType.Merge:
                    IsMergeMode = true;
                    PlaylistDialogTitle = "Merge";
                    break;
                case DialogType.CreatePlaylist:
                    PlaylistDialogTitle = "New playlist";
                    break;
                case DialogType.EditPlaylist:
                    PlaylistDialogTitle = "Edit";
                    IsEditMode = true;
                    break;
                case DialogType.Clone:
                    PlaylistDialogTitle = "Clone";
                    IsCloneMode = true;
                    break;
            }

            Messenger.Default.Send(new DialogManager
            {
                Type = dialogType,
                Action = DialogAction.Show,
            });

        }

        private void ResetPlaylistDialog()
        {
            PlaylistDialogTitle = "Playlist";
            CurrentPlaylist = null;
            PlaylistDialogName = null;
            PlaylistDialogDescription = null;
            _base64JpegData = null;
            PlaylistDialogImagePath = null;
        }

        #endregion

        #region Merge

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
                        if (SelectedPlaylistCollection != null && SelectedPlaylistCollection.Count > 0)
                        {
                            ShowPlaylistDialog(DialogType.Merge);
                        }
                    });
                }
                return _showMergeDialogCommand;
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

        private async void MergePlaylist()
        {
            IsDialogBusy = true;

            //display a dialog for the time being to get name
            if (!string.IsNullOrEmpty(PlaylistDialogName) && SelectedPlaylistCollection.Count > 0)
            {
                var playlist = await DataSource.Current.MergeSpotifyPlaylists(PlaylistDialogName, PlaylistDialogDescription, SelectedPlaylistCollection, _base64JpegData);
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

                    ResetPlaylistDialog();
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

            IsDialogBusy = false;
        }

        #endregion

        #region Edit

        private RelayCommand<Playlist> _showEditPlaylistDialogCommand;
        public RelayCommand<Playlist> ShowEditPlaylistDialogCommand
        {
            get
            {
                if (_showEditPlaylistDialogCommand == null)
                {
                    _showEditPlaylistDialogCommand = new RelayCommand<Playlist>((item) =>
                    {
                        CurrentPlaylist = item;
                        PlaylistDialogName = CurrentPlaylist.Title;
                        PlaylistDialogDescription = CurrentPlaylist.Description;
                        ShowPlaylistDialog(DialogType.EditPlaylist);
                    });
                }
                return _showEditPlaylistDialogCommand;
            }
        }

        private RelayCommand<Playlist> _updatePlaylistCommand;
        public RelayCommand<Playlist> UpdatePlaylistCommand
        {
            get
            {
                if (_updatePlaylistCommand == null)
                {
                    _updatePlaylistCommand = new RelayCommand<Playlist>((item) =>
                    {
                        UpdatePlaylist();
                    });
                }
                return _updatePlaylistCommand;
            }
        }

        private async void UpdatePlaylist()
        {
            IsDialogBusy = true;

            var updatedPlaylist = await DataSource.Current.UpdatePlaylist(CurrentPlaylist.Id, PlaylistDialogName, PlaylistDialogDescription, _base64JpegData);
            if (updatedPlaylist != null)
            {
                CurrentPlaylist.Title = updatedPlaylist.Title;
                CurrentPlaylist.Description = updatedPlaylist.Description;
                updatedPlaylist.Image = updatedPlaylist.Image;
                ResetPlaylistDialog();
                Messenger.Default.Send(new DialogManager
                {
                    Type = DialogType.Merge,
                    Action = DialogAction.Hide
                });
            }

            IsDialogBusy = false;
        }

        #endregion

        #region Clone

        private bool _unfollowAfterClone = true;
        public bool UnfollowAfterClone
        {
            get => _unfollowAfterClone;
            set
            {
                _unfollowAfterClone = value;
                RaisePropertyChanged("UnfollowAfterClone");
            }
        }

        private RelayCommand<Playlist> _showCloneDialogCommand;
        public RelayCommand<Playlist> ShowCloneDialogCommand
        {
            get
            {
                if (_showCloneDialogCommand == null)
                {
                    _showCloneDialogCommand = new RelayCommand<Playlist>((item) =>
                    {
                        CurrentPlaylist = item;
                        PlaylistDialogName = CurrentPlaylist.Title;
                        PlaylistDialogDescription = CurrentPlaylist.Description;
                        ShowPlaylistDialog(DialogType.Clone);
                    });
                }
                return _showCloneDialogCommand;
            }
        }

        private RelayCommand<Playlist> _clonePlaylistCommand;
        public RelayCommand<Playlist> ClonePlaylistCommand
        {
            get
            {
                if (_clonePlaylistCommand == null)
                {
                    _clonePlaylistCommand = new RelayCommand<Playlist>(async(item) =>
                    {
                        IsDialogBusy = true;

                        Playlist playlist = null;
                        var trackIds = await DataSource.Current.GetPlaylistTrackUris(CurrentPlaylist.Id, CurrentPlaylist.Count);

                        if (trackIds != null)
                        {
                            playlist = await DataSource.Current.CreateSpotifyPlaylist(PlaylistDialogName, PlaylistDialogDescription, trackIds, _base64JpegData);
                        }

                        if (playlist != null)
                        {
                            //remove from collection

                            //add to first position, scroll to top
                            AdvancedCollectionView.Insert(0, playlist);
                            _playlistCollectionCopy.Insert(0, playlist);
                            UpdateItemPosition();
                        }
                        if (playlist != null && UnfollowAfterClone)
                        {
                            RemoveItems(await DataSource.Current.UnfollowSpotifyPlaylist(new List<string> { CurrentPlaylist.Id }));
                        }

                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.Clone,
                            Action = DialogAction.Hide
                        });
                        ResetPlaylistDialog();

                        IsDialogBusy = false;
                    });
                }
                return _clonePlaylistCommand;
            }
        }

        #endregion

        #region Unfollowing

        private int _unfollowTotalTracks;
        public int UnfollowTotalTracks
        {
            get => _unfollowTotalTracks;
            set
            {
                _unfollowTotalTracks = value;
                RaisePropertyChanged("UnfollowTotalTracks");
            }
        }

        ObservableCollection<Playlist> _unfollowPlaylistCollection = new ObservableCollection<Playlist>();
        public ObservableCollection<Playlist> UnfollowPlaylistCollection
        {
            get => _unfollowPlaylistCollection;
            set
            {
                _unfollowPlaylistCollection = value;
                RaisePropertyChanged("UnfollowPlaylistCollection");
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
                        foreach (var item in SelectedPlaylistCollection) UnfollowPlaylistCollection.Add(item);
                        ShowUnfollowDialog();
                    });
                }
                return _unfollowSelectedCommand;
            }
        }

        private RelayCommand _cancelUnfollowCommand;
        public RelayCommand CancelUnfollowCommand
        {
            get
            {
                if (_cancelUnfollowCommand == null)
                {
                    _cancelUnfollowCommand = new RelayCommand(() =>
                    {
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.Unfollow,
                            Action = DialogAction.Hide
                        });
                        UnfollowPlaylistCollection.Clear();
                        UnfollowTotalTracks = 0;
                    });
                }
                return _cancelUnfollowCommand;
            }
        }

        private RelayCommand<Playlist> _unfollowPlaylistCommand;
        public RelayCommand<Playlist> UnfollowPlaylistCommand
        {
            get
            {
                if (_unfollowPlaylistCommand == null)
                {
                    _unfollowPlaylistCommand = new RelayCommand<Playlist>((item) =>
                    {
                        UnfollowPlaylistCollection = new ObservableCollection<Playlist> { item };
                        ShowUnfollowDialog();
                    });
                }
                return _unfollowPlaylistCommand;
            }
        }

        private RelayCommand<Playlist> _removePlaylistFromUnfollowCommand;
        public RelayCommand<Playlist> RemovePlaylistFromUnfollowCommand
        {
            get
            {
                if (_removePlaylistFromUnfollowCommand == null)
                {
                    _removePlaylistFromUnfollowCommand = new RelayCommand<Playlist>((item) =>
                    {
                        UnfollowPlaylistCollection.Remove(item);
                        var it = SelectedPlaylistCollection.Where(c => c.Id == item.Id).FirstOrDefault();
                        if (it != null) SelectedPlaylistCollection.Remove(it);

                        //update item position
                        foreach (var pl in UnfollowPlaylistCollection)
                        {
                            pl.IndexC = UnfollowPlaylistCollection.IndexOf(pl) + 1;
                        }

                        if (UnfollowPlaylistCollection.Count == 0)
                        {
                            Messenger.Default.Send(new DialogManager
                            {
                                Type = DialogType.Unfollow,
                                Action = DialogAction.Hide
                            });
                            UnfollowTotalTracks = 0;
                        }

                        UnfollowTotalTracks = UnfollowPlaylistCollection.Sum(c => c.Count);
                    });
                }
                return _removePlaylistFromUnfollowCommand;
            }
        }

        private RelayCommand _confirmUnfollowCommand;
        public RelayCommand ConfirmUnfollowCommand
        {
            get
            {
                if (_confirmUnfollowCommand == null)
                {
                    _confirmUnfollowCommand = new RelayCommand(async () =>
                    {
                        await UnfollowPlaylists();
                    });
                }
                return _confirmUnfollowCommand;
            }
        }

        private void ShowUnfollowDialog()
        {
            UnfollowTotalTracks = UnfollowPlaylistCollection.Sum(c => c.Count);
            foreach (var pl in UnfollowPlaylistCollection)
            {
                pl.IndexC = UnfollowPlaylistCollection.IndexOf(pl) + 1;
            }
            Messenger.Default.Send(new DialogManager
            {
                Type = DialogType.Unfollow,
                Action = DialogAction.Show,
            });
        }

        private async Task UnfollowPlaylists()
        {
            IsDialogBusy = true;

            var playlistIds = UnfollowPlaylistCollection.Select(c => c.Id).ToList();
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
                        if (item != null) SelectedPlaylistCollection.Remove(item);

                        item = _playlistCollectionCopy.Where(c => c.Id == id).FirstOrDefault();
                        if (item != null) _playlistCollectionCopy.Remove(item);

                        item = _filteredPlaylistCollection.Where(c => c.Id == id).FirstOrDefault();
                        if (item != null) _filteredPlaylistCollection.Remove(item);
                    }
                }

                using (AdvancedCollectionView.DeferRefresh())
                {
                    foreach (var id in succesfulItems)
                    {
                        var it = AdvancedCollectionView.Where(c => ((Playlist)c).Id == id).FirstOrDefault();
                        if (it != null) AdvancedCollectionView.Remove(it);
                    }
                    UpdateItemPosition();
                }
            }

            if (playlistIds.Count > 0)
            {
                //what to do about items that failed?
            }

            Messenger.Default.Send(new DialogManager
            {
                Type = DialogType.Unfollow,
                Action = DialogAction.Hide
            });
            UnfollowPlaylistCollection.Clear();
            UnfollowTotalTracks = 0;
            IsDialogBusy = false;
        }

        #endregion
    }
}
