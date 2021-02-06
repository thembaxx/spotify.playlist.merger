using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
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
    public partial class MainPageViewModel : ViewModelBase
    {
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

        readonly List<Playlist> _playlistCollectionCopy = new List<Playlist>();

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

        private bool _hasSelectedPlaylists;
        public bool HasSelectedPlaylists
        {
            get { return _hasSelectedPlaylists; }
            set { _hasSelectedPlaylists = value; RaisePropertyChanged("HasSelectedPlaylists"); }
        }

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                RaisePropertyChanged("SearchText");
                FilterPlaylistView(AdvancedCollectionView, SelectedPlaylistCategory.Type, value);
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
                if (value != null) FilterPlaylistView(AdvancedCollectionView, value.Type, SearchText, true);
            }
        }

        private RelayCommand<Playlist> _playlistItemClickCommand;
        public RelayCommand<Playlist> PlaylistItemClickCommand
        {
            get
            {
                if (_playlistItemClickCommand == null)
                {
                    _playlistItemClickCommand = new RelayCommand<Playlist>(async (item) =>
                    {
                        if (item == null) return;
                        await LoadTracks(item);
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
                            it.IndexB = SelectedPlaylistCollection.IndexOf(it) + 1;
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
                            it.IndexB = SelectedPlaylistCollection.IndexOf(it) + 1;
                        }
                    });
                }
                return _unselectPlaylistCommand;
            }
        }

        private async Task LoadPlaylistsAsync(int pageSizeLimit = 20)
        {
            IsPlaylistsLoading = true;

            int playlistsCount = await DataSource.Current.GetUsersPlaylistsCount();

            if (playlistsCount > 0)
            {
                int startIndex = 0;
                List<Playlist> items;
                while (startIndex < playlistsCount)
                {
                    items = await DataSource.Current.GetPlaylistsAsync(startIndex, pageSizeLimit);
                    if (items == null || items.Count == 0) break;
                    startIndex += items.Count;
                    _playlistCollectionCopy.AddRange(items);
                    if (AdvancedCollectionView == null || AdvancedCollectionView.Count == 0)
                    {
                        AdvancedCollectionView = new AdvancedCollectionView(items, true);
                        UpdateItemIndex(AdvancedCollectionView);
                    }
                    else
                    {
                        int index = AdvancedCollectionView.Count;
                        using (AdvancedCollectionView.DeferRefresh())
                        {
                            for (int i = 0; i < items.Count; i++)
                            {
                                index++;
                                items[i].IndexA = index;
                                AdvancedCollectionView.Add(items[i]);
                            }
                        }
                    }

                    TotalTracks = _playlistCollectionCopy.Sum(c => c.Count);
                }
            }

            IsPlaylistsLoading = false;
        }

        private void PopulateFilterCollection()
        {
            PlaylistCategoryCollection = PlaylistCategory.GetCategoryItems();
            var item = PlaylistCategoryCollection.Where(c => c.Type == PlaylistCategoryType.MyPlaylist).FirstOrDefault();
            item.Title = Profile.Title;
            SelectedPlaylistCategory = PlaylistCategoryCollection.Where(c => c.Type == PlaylistCategoryType.All).FirstOrDefault();
        }

        private void FilterPlaylistView(AdvancedCollectionView collectionView, PlaylistCategoryType categoryType, string searchText, bool isSwitchingCategory = false)
        {
            if (collectionView == null) return;

            IsPlaylistsLoading = true;

            if (isSwitchingCategory)
            {
                using (collectionView.DeferRefresh())
                    FilterPlaylist(collectionView, categoryType, searchText);
            }
            else
            {
                FilterPlaylist(collectionView, categoryType, searchText);
                ///AdvancedCollectionView.RefreshFilter();
            }

            TotalTracks = AdvancedCollectionView.Sum(c => ((Playlist)c).Count);
            UpdateItemIndex(AdvancedCollectionView);

            IsPlaylistsLoading = false;
        }

        private void FilterPlaylist(AdvancedCollectionView collectionView, PlaylistCategoryType categoryType, string searchText)
        {
            if (collectionView == null) return;

            if (!string.IsNullOrEmpty(searchText))
            {
                if (categoryType == PlaylistCategoryType.All)
                {
                    collectionView.Filter = c => (((Playlist)c).Title).Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                    (((Playlist)c).Description).Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                    (((Playlist)c).Owner.Title).Contains(searchText, StringComparison.CurrentCultureIgnoreCase);
                }
                else
                {
                    collectionView.Filter = c => ((((Playlist)c).Title).Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                    (((Playlist)c).Description).Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                    (((Playlist)c).Owner.Title).Contains(searchText, StringComparison.CurrentCultureIgnoreCase)) &&
                    ((Playlist)c).Type == categoryType;
                }
            }
            else
            {
                if (categoryType == PlaylistCategoryType.All)
                    collectionView.Filter = c => c != null;
                else
                    collectionView.Filter = c => ((Playlist)c).Type == categoryType;
            }
        }
    }
}
