using System.Collections.ObjectModel;

namespace spotify.playlist.merger.Models
{
    public class PlaylistCategory : NotificationBase
    {
        public PlaylistCategory(string title, PlaylistCategoryType type)
        {
            this.Title = title;
            this.Type = type;
        }

        public PlaylistCategoryType Type { get; set; }


        private string _title;
        public string Title
        {
            get { return this._title; }
            set { SetProperty(this._title, value, () => this._title = value); }
        }

        public static ObservableCollection<PlaylistCategory> GetCategoryItems()
        {
            return new ObservableCollection<PlaylistCategory>
            {
                new PlaylistCategory("All playlists", PlaylistCategoryType.All),
                new PlaylistCategory("My playlists", PlaylistCategoryType.MyPlaylist),
                new PlaylistCategory("Spotify", PlaylistCategoryType.Spotify),
                new PlaylistCategory("Following", PlaylistCategoryType.Following)
            };
        }
    }

    public enum PlaylistCategoryType
    {
        Spotify,
        MyPlaylist,
        Following,
        All,
        Default,
    };
}
