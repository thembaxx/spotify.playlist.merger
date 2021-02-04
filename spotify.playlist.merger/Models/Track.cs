namespace spotify.playlist.merger.Models
{
    public class Track : MediaItemBase
    {
        public Track() { }

        public Track(string id, string title, string uri, string imgUrl, string artist, string album, int duration, bool isExplicit)
            : base(id, title, uri, imgUrl)
        {
            Artist = artist;
            Album = album;
            Duration = duration;
            IsExplicit = isExplicit;
        }

        public string Artist { get; set; }
        public string Album { get; set; }
        public int Duration { get; set; }
        public bool IsExplicit { get; set; }
        public string DurationFormated
        {
            get
            {
                return Helpers.MillisecondsToStringAlt(Duration);
            }
        }
    }
}
