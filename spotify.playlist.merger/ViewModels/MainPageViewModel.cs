using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using spotify.playlist.merger.Data;
using spotify.playlist.merger.Model;
using System.Collections.Generic;

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
            Profile = await DataSource.Current.GetProfile();
            await DataSource.Current.GetPlaylists();
            //if (sourceItems != null) _playlistCollectionCopy.AddRange(sourceItems);           
        }

        private void RegisterMessenger()
        {
            Messenger.Default.Register<DialogResult>(this, ManageDialogResult);
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

        private void MergePlaylist()
        {
            IsLoading = true;

            

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
        }


    }
}
