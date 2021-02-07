using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using spotify.playlist.merger.Data;
using spotify.playlist.merger.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace spotify.playlist.merger.ViewModels
{
    public partial class MainPageViewModel : ViewModelBase
    {
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

        private string _mergeProgressText;
        public string MergeProgressText
        {
            get => _mergeProgressText;
            set
            {
                _mergeProgressText = value;
                RaisePropertyChanged("MergeProgressText");
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
                        MergePlaylist(SelectedPlaylistCollection);
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
                        MergePlaylist(SelectedPlaylistCollection);
                    });
                }
                return _mergeSelectedPlaylistCommand;
            }
        }

        private async void MergePlaylist(IEnumerable<Playlist> playlists)
        {
            if (string.IsNullOrEmpty(PlaylistDialogName) || playlists.Count() == 0)
                return;

            MergeProgressText = string.Concat("Preparing to merge ", playlists.Count(), " playlists...");

            IsDialogBusy = true;
            List<string> trackUris = new List<string>();
            int currentIndex = 1;

            foreach (var playlist in playlists)
            {
                int total = playlist.Count;
                int startIndex = 0;
                List<string> uris;

                MergeProgressText = string.Concat("Processing playlist ", currentIndex, " of ", playlists.Count(), ": getting tracks...");
                while (startIndex < total - 1)
                {
                    uris = await DataSource.Current.GetTrackUrisAsync(playlist.Id, startIndex);

                    if (uris == null || uris.Count == 0) break;
                    startIndex += uris.Count - 1;
                    foreach (var uri in uris)
                    {
                        if (!trackUris.Contains(uri)) trackUris.Add(uri);
                    }

                    
                }

                currentIndex++;
            }

            MergeProgressText = string.Concat("Successfully processed ", trackUris.Count, " of ", playlists.Sum(c => c.Count), " tracks.");

            var newPlaylist = await DataSource.Current.CreateSpotifyPlaylist(PlaylistDialogName, PlaylistDialogDescription, _base64JpegData);
            if (newPlaylist != null)
            {
                MergeProgressText = string.Concat("Playlist(", newPlaylist.Title, " created, adding tracks to new playlist.");
                if (trackUris.Count > 100)
                {
                    int startIndex = 0;
                    int endIndex = 0;
                    while (startIndex < trackUris.Count - 1)
                    {
                        endIndex = ((startIndex + 100) < trackUris.Count - 1) ? 100 : ((trackUris.Count - startIndex));
                        var batch = trackUris.GetRange(startIndex, endIndex);
                        startIndex += batch.Count - 1;

                        MergeProgressText = string.Concat("Playlist(", newPlaylist.Title, " created, adding tracks(", ((startIndex + 1) / trackUris.Count)*100, "%)");

                        if (!await DataSource.Current.AddToPlaylist(batch, newPlaylist.Id))
                            break;
                    }
                }
                else
                    await DataSource.Current.AddToPlaylist(trackUris, newPlaylist.Id);


                MergeProgressText = string.Concat("Mergin playlists complete.");

                IsPlaylistsLoading = true;

                HidePlaylistDialog(DialogType.Merge);
                ResetPlaylistDialog();
                IsDialogBusy = false;

                newPlaylist = await DataSource.Current.GetFullPlaylistAsync(newPlaylist.Id);

                AdvancedCollectionView.Insert(0, newPlaylist);
                _playlistCollectionCopy.Add(newPlaylist);
                if (UnfollowAfterMerge)
                {
                    var unfollowed = await DataSource.Current.UnfollowSpotifyPlaylist(SelectedPlaylistCollection.Select(c => c.Id));
                    RemoveItems(unfollowed);
                }

                var items = SelectedPlaylistCollection;
                foreach (var item in items)
                {
                    item.IsSelected = false;
                    SelectedPlaylistCollection.Remove(item);
                }

                using (AdvancedCollectionView.DeferRefresh())
                {
                    UpdateItemIndex(AdvancedCollectionView);
                    Messenger.Default.Send(new MessengerHelper
                    {
                        Item = AdvancedCollectionView.FirstOrDefault(),
                        Action = MessengerAction.ScrollToItem,
                        Target = TargetView.Playlist
                    });
                }

                ShowNotification(NotificationType.Success, "Successfuly merged selected playlists.");

                IsPlaylistsLoading = false;
            }
            else
                ShowNotification(NotificationType.Error, "An error occured, failed to merge playlists.");

            IsDialogBusy = false;
        }

        #endregion
    }
}
