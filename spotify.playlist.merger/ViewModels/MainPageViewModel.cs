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

        public async void Initialize()
        {
            IsLoading = true;
            DataSource.Current.Initialize();
            Profile = await DataSource.Current.GetProfile();
            if (Profile != null && Profile.IsPremium)
            {
                PopulateFilterCollection();
                await DataSource.Current.GetPlaylists();
                SelectedPlaylistCollection.CollectionChanged += SelectedPlaylistCollection_CollectionChanged;
                
            } else
            {
                //show message that this needs a premium account
                Helpers.DisplayDialog("Premium account required", "A premium account is required to use the features provided by this app.");
            }
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

        private void ManageDialogResult(DialogResult result)
        {
            if (result.ResultType == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                switch (result.Type)
                {
                    case DialogType.Merge:
                        MergePlaylist();
                        break;
                }
            }
        }

        private string _base64JpegData;

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                RaisePropertyChanged("IsLoading");
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

        private RelayCommand _mergeCommand;
        public RelayCommand MergeCommand
        {
            get
            {
                if (_mergeCommand == null)
                {
                    _mergeCommand = new RelayCommand(() =>
                    {
                        //merge
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
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.CreatePlaylist,
                            Action = DialogAction.Show,
                        });
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
                            //load Base64JpegData
                            MergeImageFilePath = file.Path;
                            _base64JpegData = await Helpers.ImageToBase64(file);
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
            }

            if(e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is Playlist playlist)
                    {
                        playlist.IsSelected = true;
                        playlist.PositionAlt = SelectedPlaylistCollection.IndexOf(playlist) + 1;
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
                    AddToCollection(new List<Playlist> { playlist });
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
    }
}
