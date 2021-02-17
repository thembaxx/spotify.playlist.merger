using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using spotify.playlist.merger.Data;
using spotify.playlist.merger.Models;
using System.Collections.Generic;
using System.Linq;

namespace spotify.playlist.merger.ViewModels
{
    public partial class MainPageViewModel : ViewModelBase
    {
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
                    _clonePlaylistCommand = new RelayCommand<Playlist>(async (playlist) =>
                    {
                        if (playlist == null) return;
                        IsDialogBusy = true;

                        int total = playlist.Count;
                        int startIndex = 0;
                        List<string> uris;
                        List<string> trackUris = new List<string>();

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

                        if (trackUris != null && trackUris.Count > 0)
                        {
                            Playlist newPlaylist = await DataSource.Current.CreateSpotifyPlaylist(PlaylistDialogName, PlaylistDialogDescription, _base64JpegData);

                            if (newPlaylist != null)
                            {
                                if (trackUris.Count > 100)
                                {
                                    startIndex = 0;
                                    int endIndex = 0;
                                    while (startIndex < trackUris.Count - 1)
                                    {
                                        endIndex = ((startIndex + 100) < trackUris.Count - 1) ? 100 : ((trackUris.Count - startIndex));
                                        var batch = trackUris.GetRange(startIndex, endIndex);
                                        startIndex += batch.Count - 1;

                                        if (!await DataSource.Current.AddToPlaylist(batch, newPlaylist.Id))
                                            break;
                                    }
                                }
                                else
                                    await DataSource.Current.AddToPlaylist(trackUris, newPlaylist.Id);

                                IsPlaylistsLoading = true;

                                HidePlaylistDialog(DialogType.Clone);
                                ResetPlaylistDialog();

                                //add to first position, scroll to top
                                if (UnfollowAfterClone)
                                    RemoveItems(await DataSource.Current.UnfollowSpotifyPlaylist(new List<string> { CurrentPlaylist.Id }));

                                PlaylistsCollection.Insert(0, newPlaylist);
                                UpdateItemIndex(AdvancedCollectionView);
                                Messenger.Default.Send(new MessengerHelper
                                {
                                    Item = AdvancedCollectionView.FirstOrDefault(),
                                    Action = MessengerAction.ScrollToItem,
                                    Target = TargetView.Playlist
                                });

                                var items = SelectedPlaylistCollection;
                                foreach (var it in items)
                                {
                                    it.IsSelected = false;
                                    SelectedPlaylistCollection.Remove(it);
                                }

                                ShowNotification(NotificationType.Success, "Successfuly cloned playlist.");
                                IsPlaylistsLoading = false;
                            }
                            else
                                ShowNotification(NotificationType.Error, "An error occured, failed to clone playlist.");
                        }

                        IsDialogBusy = false;
                    });
                }
                return _clonePlaylistCommand;
            }
        }

        #endregion
    }
}
