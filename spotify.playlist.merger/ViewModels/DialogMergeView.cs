using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Toolkit.Uwp.UI;
using spotify.playlist.merger.Data;
using spotify.playlist.merger.Models;
using System;
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

                    HidePlaylistDialog(DialogType.Merge);

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

                    ShowNotification(NotificationType.Success, "Successfuly merged selected playlists.");
                }
                else
                    ShowNotification(NotificationType.Error, "An error occured, failed to merge playlists.");
            }

            IsDialogBusy = false;
        }

        #endregion
    }
}
