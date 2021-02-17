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
    public partial class MainPageViewModel : ViewModelBase
    {
        #region Default

        Track _tempTrack = null;

        private AdvancedCollectionView _addToPlaylistCollectionView;
        public AdvancedCollectionView AddToPlaylistCollectionView
        {
            get => _addToPlaylistCollectionView;
            set
            {
                _addToPlaylistCollectionView = value;
                RaisePropertyChanged("AddToPlaylistCollectionView");
            }
        }

        private bool _unfollowTracksAfterConfirm;
        public bool UnfollowTracksAfterConfirm
        {
            get => _unfollowTracksAfterConfirm;
            set
            {
                _unfollowTracksAfterConfirm = value;
                RaisePropertyChanged("UnfollowTracksAfterConfirm");
            }
        }

        private bool _hasSelectedAddToPlaylist;
        public bool HasSelectedAddToPlaylist
        {
            get => _hasSelectedAddToPlaylist;
            set
            {
                _hasSelectedAddToPlaylist = value;
                RaisePropertyChanged("HasSelectedAddToPlaylist");
            }
        }

        ObservableCollection<MediaItemBase> _selectedAddToPlaylist = new ObservableCollection<MediaItemBase>();
        public ObservableCollection<MediaItemBase> SelectedAddToPlaylist
        {
            get => _selectedAddToPlaylist;
            set
            {
                _selectedAddToPlaylist = value;
                RaisePropertyChanged("SelectedAddToPlaylist");
            }
        }

        private RelayCommand _addSelectedTracksToPlaylistCommand;
        public RelayCommand AddSelectedTracksToPlaylistCommand
        {
            get
            {
                if (_addSelectedTracksToPlaylistCommand == null)
                {
                    _addSelectedTracksToPlaylistCommand = new RelayCommand(() =>
                    {
                        IsDialogOpen = true;
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.AddToPlaylist,
                            Action = DialogAction.Show,
                        });
                        IsDialogBusy = true;

                        int index = 1;
                        var _items = PlaylistsCollection.ToList();
                        _items = _items.OrderBy(c => c.Title).ToList();
                        List<Playlist> items = new List<Playlist>();
                        Playlist clone = null;
                        foreach (var item in _items)
                        {
                            if (item.Id != ActivePlaylist.Id)
                            {
                                clone = item.Clone();
                                clone.IndexC = index;
                                items.Add(clone);
                                index++;
                            }
                        }
                        AddToPlaylistCollectionView = new AdvancedCollectionView(items, true);

                        IsDialogBusy = false;
                    });
                }
                return _addSelectedTracksToPlaylistCommand;
            }
        }

        private RelayCommand<Track> _addTrackToPlaylistCommand;
        public RelayCommand<Track> AddTrackToPlaylistCommand
        {
            get
            {
                if (_addTrackToPlaylistCommand == null)
                {
                    _addTrackToPlaylistCommand = new RelayCommand<Track>((track) =>
                    {
                        ResetAddToPlaylist();
                        _tempTrack = track;

                        IsDialogOpen = true;
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.AddToPlaylist,
                            Action = DialogAction.Show,
                        });
                        IsDialogBusy = true;

                        int index = 1;
                        var _items = PlaylistsCollection.ToList();
                        _items = _items.OrderBy(c => c.Title).ToList();
                        List<Playlist> items = new List<Playlist>();
                        Playlist clone = null;
                        foreach (var item in _items)
                        {
                            if (item.Id != ActivePlaylist.Id)
                            {
                                clone = item.Clone();
                                clone.IndexC = index;
                                items.Add(clone);
                                index++;
                            }
                        }
                        AddToPlaylistCollectionView = new AdvancedCollectionView(items, true);

                        IsDialogBusy = false;
                    });
                }
                return _addTrackToPlaylistCommand;
            }
        }

        private RelayCommand _confirmAddToPlaylistCommand;
        public RelayCommand ConfirmAddToPlaylistCommand
        {
            get
            {
                if (_confirmAddToPlaylistCommand == null)
                {
                    _confirmAddToPlaylistCommand = new RelayCommand(async () =>
                    {
                        if (IsDialogBusy) return;
                        if (SelectedAddToPlaylist.Count == 0)
                        {
                            //show error message
                        }
                        IsDialogBusy = true;

                        IEnumerable<string> trackUris = null;
                        trackUris = (_tempTrack != null) ? new List<string> { _tempTrack.Uri } : SelectedTracks.Select(c => c.Uri);
                        var playlistIds = SelectedAddToPlaylist.Select(c => c.Id);
                        List<bool> successful = new List<bool>();
                        foreach (var playlistId in playlistIds)
                        {
                            successful.Add(await DataSource.Current.AddToPlaylist(trackUris, playlistId));
                        }

                        if (UnfollowTracksAfterConfirm && ActivePlaylist != null)
                        {
                            if (await DataSource.Current.RemoveFromPlaylist(ActivePlaylist.Id, trackUris) != null)
                            {
                                IsTracksLoading = true;

                                foreach (var uri in trackUris)
                                {
                                    var tr = SelectedTracks.Where(c => c.Uri == uri).FirstOrDefault();
                                    if (tr != null) SelectedTracks.Remove(tr);
                                    tr = TracksCollection.Where(c => c.Uri == uri).FirstOrDefault();
                                    if (tr != null) TracksCollection.Remove(tr);

                                    var _tr = TracksCollectionView.Where(c => ((Track)c).Uri == uri).FirstOrDefault();
                                    using (TracksCollectionView.DeferRefresh()) TracksCollectionView.Remove(_tr);
                                }
                                UpdateItemIndex(TracksCollectionView);

                                IsTracksLoading = false;
                            }
                            //remove tracks from playlists
                        }

                        if (successful.Count() == playlistIds.Count())
                        {
                            ShowNotification(NotificationType.Success, "Successfuly added selected tracks to playlists");
                        }
                        else if (successful.Count() > 0)
                        {
                            ShowNotification(NotificationType.Success, "Successfuly added " + successful.Count() + " to playlists, " + (playlistIds.Count() - successful.Count()) + " failed.");
                        }
                        else
                        {
                            ShowNotification(NotificationType.Error, "An error occured, could not add selected tracks to playlists");
                        }

                        if (_tempTrack == null) SelectedAddToPlaylist.Clear();
                        ResetAddToPlaylist();
                        IsDialogBusy = false;
                    });
                }
                return _confirmAddToPlaylistCommand;
            }
        }

        private RelayCommand<Playlist> _addToPlaylistItemClickCommand;
        public RelayCommand<Playlist> AddToPlaylistItemClickCommand
        {
            get
            {
                if (_addToPlaylistItemClickCommand == null)
                {
                    _addToPlaylistItemClickCommand = new RelayCommand<Playlist>((item) =>
                    {
                        SelectedAddToPlaylist.Add(item);
                    });
                }
                return _addToPlaylistItemClickCommand;
            }
        }

        private RelayCommand _cancelAddToPlaylistCommand;
        public RelayCommand CancelAddToPlaylistCommand
        {
            get
            {
                if (_cancelAddToPlaylistCommand == null)
                {
                    _cancelAddToPlaylistCommand = new RelayCommand(() =>
                    {
                        ResetAddToPlaylist();
                    });
                }
                return _cancelAddToPlaylistCommand;
            }
        }

        private void SelectedAddToPlaylist_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            HasSelectedAddToPlaylist = (SelectedAddToPlaylist.Count > 0);
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is MediaItemBase media) media.IsSelected = !media.IsSelected;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is MediaItemBase media) media.IsSelected = !media.IsSelected;
                }
            }
        }

        private void ResetAddToPlaylist()
        {
            HidePlaylistDialog(DialogType.AddToPlaylist);
            if (AddToPlaylistCollectionView != null) AddToPlaylistCollectionView.Clear();
            UnfollowTracksAfterConfirm = false;
            _tempTrack = null;
        }

        #endregion

        #region Sorting & filtering

        private string _addToPlaylistSearchText;
        public string AddToPlaylistSearchText
        {
            get { return _addToPlaylistSearchText; }
            set
            {
                _addToPlaylistSearchText = value;
                RaisePropertyChanged("AddToPlaylistSearchText");
                FilterAddToPlaylistCollectionView();
            }
        }

        private Sorting _selectedAddToPlaylistSort;
        public Sorting SelectedAddToPlaylistSort
        {
            get { return _selectedAddToPlaylistSort; }
            set
            {
                _selectedAddToPlaylistSort = value;
                RaisePropertyChanged("SelectedAddToPlaylistSort");
                if (value != null) SortAddToPlaylistCollection(value);
            }
        }

        private void SortAddToPlaylistCollection(Sorting sorting)
        {
            if (AddToPlaylistCollectionView == null)
                return;

            IsDialogBusy = true;

            try
            {
                using (AddToPlaylistCollectionView.DeferRefresh())
                {
                    AddToPlaylistCollectionView.SortDescriptions.Clear();
                    AddToPlaylistCollectionView.SortDescriptions.Add(new SortDescription(sorting.Property, sorting.SortDirection));
                }

                using (AddToPlaylistCollectionView.DeferRefresh())
                {
                    foreach (var obj in AddToPlaylistCollectionView)
                    {
                        if (obj is Playlist item)
                        {
                            item.IndexC = AddToPlaylistCollectionView.IndexOf(obj) + 1;
                        }
                    }
                }
            }
            catch (Exception)
            {
                //
            }

            IsDialogBusy = false;
        }

        public void FilterAddToPlaylistCollectionView()
        {
            if (AddToPlaylistCollectionView == null)
                return;

            IsDialogBusy = true;

            //make sure the filtered items are clear
            string searchStr = AddToPlaylistSearchText;
            using (AddToPlaylistCollectionView.DeferRefresh())
            {
                if (!string.IsNullOrEmpty(searchStr))
                {
                    AddToPlaylistCollectionView.Filter = c => (((Playlist)c).Title).Contains(searchStr, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Playlist)c).Owner.Title).Contains(searchStr, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Playlist)c).Description).Contains(searchStr, StringComparison.CurrentCultureIgnoreCase);
                }
                else
                {
                    AddToPlaylistCollectionView.Filter = c => c != null; //bit of a hack to clear filters
                }

                int index = 1;
                foreach (var obj in AddToPlaylistCollectionView)
                {
                    if (obj is Playlist item)
                    {
                        item.IndexC = index;
                        index++;
                    }
                }
            }

            IsDialogBusy = false;
        }

        #endregion
    }
}
