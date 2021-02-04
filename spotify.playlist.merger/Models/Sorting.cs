using Microsoft.Toolkit.Uwp.UI;
using System.Collections.Generic;

namespace spotify.playlist.merger.Models
{
    public class Sorting
    {
        public Sorting(string title, string property, SortType type, SortDirection sortDirection)
        {
            this.Title = title;
            this.Property = property;
            this.Type = type;
            this.SortDirection = sortDirection;
        }


        public static List<Sorting> _tracksSortList = new List<Sorting>
        {
            new Sorting("Title", "Title", SortType.Name, SortDirection.Ascending),
            new Sorting("Artist", "Artist", SortType.Artist, SortDirection.Ascending),
            new Sorting("Album", "Album", SortType.Album, SortDirection.Ascending),
        };

        public string Title { get; set; }
        public string Property { get; set; }
        public SortType Type { get; set; }
        public SortDirection SortDirection { get; set; }

        public enum SortType
        {
            Default,
            Artist,
            Album,
            Name,
            Date,
            Size,
            Duration
        }
    }
}
