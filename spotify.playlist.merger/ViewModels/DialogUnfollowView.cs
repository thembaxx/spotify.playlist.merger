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
using Windows.UI.Xaml;

namespace spotify.playlist.merger.ViewModels
{
    public partial class MainPageViewModel : ViewModelBase
    {
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
                        HidePlaylistDialog(DialogType.Unfollow);
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

                        if (UnfollowPlaylistCollection.Count == 0)
                        {
                            HidePlaylistDialog(DialogType.Unfollow);
                            UnfollowTotalTracks = 0;
                        }

                        UnfollowTotalTracks = UnfollowPlaylistCollection.Sum(c => c.Count);
                        foreach (var pl in SelectedPlaylistCollection)
                        {
                            pl.IndexB = SelectedPlaylistCollection.IndexOf(pl) + 1;
                        }
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

            IsDialogOpen = true;
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

                    if (ActivePlaylist != null && ActivePlaylist.Id == id && IsTracksViewOpen)
                    {
                        IsTracksViewOpen = false;
                        ResetTracksView();
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

                ShowNotification(NotificationType.Success, "Successfuly unfollowed selected playlists.");
            }
            else
            {
                ShowNotification(NotificationType.Error, "An error occured, could not unfollow selected playlists");
            }

            if (playlistIds.Count > 0)
            {
                //what to do about items that failed?
            }

            HidePlaylistDialog(DialogType.Unfollow);
            UnfollowPlaylistCollection.Clear();
            UnfollowTotalTracks = 0;
            IsDialogBusy = false;
        }

        #endregion
    }
}
