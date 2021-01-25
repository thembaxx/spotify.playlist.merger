using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spotify.playlist.merger.Model
{
    public class Playlist : MediaItemBase
    {
        public Playlist () { }
        public Playlist(string id, string title, string uri, string imgUrl, string description, int count, string duration)
            :base(id, title, uri, imgUrl)
        {
            Description = Helpers.CleanString(description);
            Count = count;
            Duration = duration;
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { SetProperty(_description, value, () => _description = value); }
        }

        private int _count;
        public int Count
        {
            get => _count;
            set => _ = SetProperty(_count, value, () => _count = value);
        }

        private string _duration = "0";
        public string Duration
        {
            get { return _duration; }
            set { SetProperty(_duration, value, () => _duration = value); }
        }
    }
}
