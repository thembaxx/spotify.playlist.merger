using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using spotify.playlist.merger.Data;
using spotify.playlist.merger.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace spotify.playlist.merger.ViewModels
{
    public partial class MainPageViewModel : ViewModelBase
    {
        private Views.SelectedPlaylistView _selectedPlaylistControl;
        public Views.SelectedPlaylistView SelectedPlaylistControl
        {
            get
            {
                if (_selectedPlaylistControl == null)
                {
                    _selectedPlaylistControl = new Views.SelectedPlaylistView();
                }
                return _selectedPlaylistControl;
            }
        }

        ObservableCollection<Playlist> _selectedPlaylistCollection = new ObservableCollection<Playlist>();
        public ObservableCollection<Playlist> SelectedPlaylistCollection
        {
            get => _selectedPlaylistCollection;
            set
            {
                _selectedPlaylistCollection = value;
                RaisePropertyChanged("SelectedPlaylistCollection");
            }
        }
        private void SelectedPlaylistCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            HasSelectedPlaylists = (SelectedPlaylistCollection.Count > 0);
            CanPlay = (SelectedPlaylistCollection.Count > 0);
            CanMerge = (SelectedPlaylistCollection.Count > 1);
            TotalSelectedPlaylistTracks = SelectedPlaylistCollection.Sum(c => c.Count);

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is Playlist playlist)
                    {
                        playlist.IsSelected = false;
                        playlist.IndexB = 0;
                    }
                }

                //update position
                //foreach (var item in e.OldItems)
                //{
                //    if (item is Playlist playlist)
                //        playlist.PositionAlt = SelectedPlaylistCollection.IndexOf(playlist) + 1;
                //}
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is Playlist playlist)
                    {
                        playlist.IsSelected = true;

                    }
                }
            }
        }

        private bool _isSelectedPlaylistViewOpen = true;
        public bool IsSelectedPlaylistViewOpen
        {
            get => _isSelectedPlaylistViewOpen;
            set
            {
                _isSelectedPlaylistViewOpen = value;
                RaisePropertyChanged("IsSelectedPlaylistViewOpen");
                if (value) IsTracksViewOpen = !value;
            }
        }

        private bool _isRightBarBusy;
        public bool IsRightBarBusy
        {
            get => _isRightBarBusy;
            set
            {
                _isRightBarBusy = value;
                RaisePropertyChanged("IsRightBarBusy");
            }
        }

        private bool _canPlay;
        public bool CanPlay
        {
            get => _canPlay;
            set
            {
                _canPlay = value;
                RaisePropertyChanged("CanPlay");
            }
        }

        private bool _canMerge;
        public bool CanMerge
        {
            get => _canMerge;
            set
            {
                _canMerge = value;
                RaisePropertyChanged("CanMerge");
            }
        }

        private int _totalTracks;
        public int TotalTracks
        {
            get => _totalTracks;
            set
            {
                _totalTracks = value;
                RaisePropertyChanged("TotalTracks");
            }
        }

        private int _totalSelectedPlaylistTracks;
        public int TotalSelectedPlaylistTracks
        {
            get => _totalSelectedPlaylistTracks;
            set
            {
                _totalSelectedPlaylistTracks = value;
                RaisePropertyChanged("TotalSelectedPlaylistTracks");
            }
        }

        private RelayCommand _clearSelectedCommand;
        public RelayCommand ClearSelectedCommand
        {
            get
            {
                if (_clearSelectedCommand == null)
                {
                    _clearSelectedCommand = new RelayCommand(() =>
                    {
                        var selected = SelectedPlaylistCollection.ToList();
                        foreach (var item in selected)
                        {
                            item.IsSelected = false;
                            item.IndexB = 0;
                            SelectedPlaylistCollection.Remove(item);
                        }
                    });
                }
                return _clearSelectedCommand;
            }
        }

        private RelayCommand _playSelectedCommand;
        public RelayCommand PlaySelectedCommand
        {
            get
            {
                if (_playSelectedCommand == null)
                {
                    _playSelectedCommand = new RelayCommand(async () =>
                    {
                        IsRightBarBusy = true;

                        var playlistIds = SelectedPlaylistCollection.Select(c => c.Id).ToList();
                        var tracks = new List<string>();
                        foreach (var item in playlistIds)
                        {
                            var items = await DataSource.Current.GetTrackIds(item);
                            if (items != null)
                            {
                                foreach (var id in items)
                                {
                                    if (tracks.Find(c => c == id) == null) tracks.Add(id);
                                }
                            }
                        }
                        await DataSource.Current.PlaySpotifyMedia(tracks);


                        IsRightBarBusy = false;
                    });
                }
                return _playSelectedCommand;
            }
        }
    }
}
