using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace spotify.playlist.merger.Model
{
    public class User : MediaItemBase
    {
        public User () { }
        public User (string id, string title, string uri, string imgUrl)
            : base(id, title, uri, imgUrl)
        {
            
        }
    }
}
