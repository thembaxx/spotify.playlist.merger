using System;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace spotify.playlist.merger.Models
{
    public class MediaItemBase : NotificationBase
    {
        public MediaItemBase() { }
        public MediaItemBase(string id, string title, string uri, string imgUrl)
        {
            Id = id;
            Title = title;
            Uri = uri;
            SetImage(imgUrl);
        }

        private string _id;
        public string Id
        {
            get => _id;
            set => _ = SetProperty(_id, value, () => _id = value);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => _ = SetProperty(_title, value, () => _title = value);
        }

        private string _uri;
        public string Uri
        {
            get => _uri;
            set => _ = SetProperty(_uri, value, () => _uri = value);
        }

        private ImageSource _image;
        public ImageSource Image
        {
            get => _image;
            set
            {
                if (_image != value)
                    _image = value;
                _ = SetProperty(_image, value, () => _image = value);
            }
        }

        private void SetImage(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                Uri uri = new Uri(url);
                _image = new BitmapImage(uri);
            }
        }

        private int _indexA;
        public int IndexA
        {
            get => _indexA;
            set => _ = SetProperty(_indexA, value, () => _indexA = value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(this._isSelected, value, () => this._isSelected = value); }
        }
    }
}
