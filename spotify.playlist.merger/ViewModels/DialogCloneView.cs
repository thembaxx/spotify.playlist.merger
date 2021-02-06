using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Toolkit.Uwp.UI;
using spotify.playlist.merger.Data;
using spotify.playlist.merger.Models;
using System.Collections.Generic;

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
                    _clonePlaylistCommand = new RelayCommand<Playlist>(async (item) =>
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
                            UpdateItemIndex(AdvancedCollectionView);
                        }
                        if (playlist != null && UnfollowAfterClone)
                        {
                            RemoveItems(await DataSource.Current.UnfollowSpotifyPlaylist(new List<string> { CurrentPlaylist.Id }));
                        }

                        HidePlaylistDialog(DialogType.Clone);
                        ResetPlaylistDialog();

                        IsDialogBusy = false;
                    });
                }
                return _clonePlaylistCommand;
            }
        }

        #endregion
    }
}
