namespace spotify.playlist.merger.Models
{
    public class MessengerHelper
    {
        public object Item { get; set; }
        public MessengerAction Action { get; set; }
        public TargetView Target { get; set; }
    }

    public enum MessengerAction
    {
        ScrollToItem,
        IsLoading,
        ShowSettings,
    }

    public enum TargetView
    {
        Playlist,
        Tracks,
    }
}
