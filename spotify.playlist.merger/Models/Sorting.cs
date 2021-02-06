using Microsoft.Toolkit.Uwp.UI;
using System.Collections.Generic;

namespace spotify.playlist.merger.Models
{
    public class Sorting
    {
        public Sorting(string title, string property, SortType type, SortDirection sortDirection)
        {
            Title = title;
            Property = property;
            Type = type;
            SortDirection = sortDirection;
        }

        public static List<Sorting> _playlistSortList = new List<Sorting>
        {
            new Sorting("Title", "Title", SortType.Name, SortDirection.Ascending),
            new Sorting("Owner", "Owner", SortType.Owner, SortDirection.Ascending),
            new Sorting("Tracks", "Count", SortType.Size, SortDirection.Descending),
        };

        public static List<Sorting> _tracksSortList = new List<Sorting>
        {
            new Sorting("Title", "Title", SortType.Name, SortDirection.Ascending),
            new Sorting("Artist", "Artist", SortType.Artist, SortDirection.Ascending),
            new Sorting("Album", "Album", SortType.Album, SortDirection.Ascending),
            new Sorting("Duration", "Duration", SortType.Duration, SortDirection.Descending),
            new Sorting("Date added", "DateAdded", SortType.DateAdded, SortDirection.Descending),
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
            DateAdded,
            Size,
            Duration,
            Owner
        }
    }
}
