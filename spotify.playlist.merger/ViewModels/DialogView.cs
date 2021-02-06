using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using spotify.playlist.merger.Models;
using System;

namespace spotify.playlist.merger.ViewModels
{
    public partial class MainPageViewModel : ViewModelBase
    {
        #region Dialog

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

        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set
            {
                _isDialogOpen = value;
                RaisePropertyChanged("IsDialogOpen");
            }
        }

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
                        HidePlaylistDialog(DialogType.Merge);
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

            IsDialogOpen = true;
        }

        private void HidePlaylistDialog(DialogType dialogType)
        {
            Messenger.Default.Send(new DialogManager
            {
                Type = dialogType,
                Action = DialogAction.Hide
            });

            IsDialogOpen = false;
            IsMergeMode = false;
            IsEditMode = false;
            IsCloneMode = false;
            ResetPlaylistDialog();
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
    }
}
