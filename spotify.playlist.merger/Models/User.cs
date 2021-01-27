namespace spotify.playlist.merger.Models
{
    public class User : MediaItemBase
    {
        public User() { }
        public User(string id, string title, string uri, string imgUrl, bool isPremium)
            : base(id, title, uri, imgUrl)
        {
            IsPremium = isPremium;
        }

        private bool _isPremium;
        public bool IsPremium
        {
            get { return _isPremium; }
            set { SetProperty(_isPremium, value, () => _isPremium = value); }
        }
    }
}
