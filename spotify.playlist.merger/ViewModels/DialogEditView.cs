using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using spotify.playlist.merger.Data;
using spotify.playlist.merger.Models;

namespace spotify.playlist.merger.ViewModels
{
    public partial class MainPageViewModel : ViewModelBase
    {
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

                HidePlaylistDialog(DialogType.Merge);
            }

            IsDialogBusy = false;
        }

        #endregion
    }
}
