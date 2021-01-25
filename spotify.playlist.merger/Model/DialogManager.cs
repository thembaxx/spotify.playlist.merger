using Windows.UI.Xaml.Controls;

namespace spotify.playlist.merger.Model
{
    public class DialogManager
    {
        public DialogManager() { }

        public DialogManager(DialogType type, DialogAction action)
        {
            Type = type;
            Action = action;
        }

        public DialogManager(string title, string message, string primaryButtonText, DialogType type, DialogAction action)
        {
            Title = title;
            Message = message;
            PrimaryButtonText = primaryButtonText;
            Type = type;
            Action = action;
        }

        public object Item { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string PrimaryButtonText { get; set; }
        public string SecondaryButtonText { get; set; }
        public DialogAction Action { get; set; }
        public DialogType Type { get; set; }
    }

    public class DialogResult
    {
        public DialogResult(DialogType type, ContentDialogResult resultType)
        {
            Type = type;
            ResultType = resultType;
        }

        public DialogResult(DialogType type, ContentDialogResult resultType, object item)
        {
            Type = type;
            ResultType = resultType;
            Item = item;
        }

        public object Item { get; set; }
        public DialogType Type { get; set; }
        public ContentDialogResult ResultType { get; set; }
    }

    public enum DialogAction
    {
        Show,
        Hide
    }

    public enum DialogType
    {
        Merge,
        CreatePlaylist,
        Default,
        Unfollow,
        AddToPlaylist,
        EditPlaylist
    }
}
