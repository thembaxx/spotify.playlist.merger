using System.Collections.Generic;

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

        private int _count;
        public int Count
        {
            get { return _count; }
            set { SetProperty(this._count, value, () => this._count = value); }
        }

        private bool _hasResults;
        public bool HasResults
        {
            get { return _hasResults; }
            set { SetProperty(this._hasResults, value, () => this._hasResults = value); }
        }

        public static List<PlaylistCategory> GetCategoryItems()
        {
            return new List<PlaylistCategory>
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
