using GalaSoft.MvvmLight.Messaging;
using spotify.playlist.merger.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace spotify.playlist.merger.Data
{
    public class DataSource
    {
        private User _loggedInUserProfile;
        private int startIndex = 0;
        private readonly int limit = 20;
        private Paging<SimplePlaylist> page;

        public static DataSource _current;
        public static DataSource Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new DataSource();
                }
                return _current;
            }
            set { _current = value; }
        }

        public DataSource() { Current = this; }

        public async Task Initialize()
        {
            try
            {
                if (await SpotifyApi.IsAuthenticated())
                {
                    //get playlists
                }
                else
                {
                    await SpotifyApi.Authenticate();
                }
            }
            catch (Exception)
            {
                await Helpers.DisplayDialog("Authentication Error", "An error occured, please restart app.");
            }
        }

        public void Logout()
        {
            _loggedInUserProfile = null;
            startIndex = 0;
            page = null;
            SpotifyApi.LogOut();
        }

        /// <summary>
        /// Gets Logged-in users profile.
        /// </summary>
        /// <returns>
        /// Logged-in users profile.
        /// </returns>
        public async Task<User> GetProfile()
        {
            try
            {
                if (_loggedInUserProfile == null)
                {
                    var user = await SpotifyApi.GetProfile();
                    if (user == null) return null;
                    bool isPremium = (user.Product == "premium");
                    var imgUrl = (user.Images != null && user.Images.FirstOrDefault() != null) ? user.Images.FirstOrDefault().Url : "";
                    _loggedInUserProfile = new User(user.Id, user.DisplayName, user.Uri, imgUrl, isPremium);
                }
                return _loggedInUserProfile;
            }
            catch (Exception e)
            {
                await Helpers.DisplayDialog("Error", e.Message);
                return null;
            }
        }

        private Paging<SimplePlaylist> _tempPage = null;
        public async Task<int> GetUsersPlaylistsCount()
        {
            try
            {
                _tempPage = await SpotifyApi.GetInitialPlaylist(limit);
                return _tempPage.Total.Value;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public async Task<List<Playlist>> GetPlaylistsAsync(int startIndex, int limit = 20)
        {
            try
            {
                if (startIndex == 0 && _tempPage != null)
                    return ConvertPlaylists(_tempPage.Items);
                else
                {
                    var items = await SpotifyApi.GetPlaylists(startIndex, limit);
                    return ConvertPlaylists(items);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new playlist for current user.
        /// </summary>
        /// <param name="name">
        /// The name of the new playlist.
        /// </param>
        /// <param name="tracks">
        /// The tracks to add to the newly created playlist (Optional).
        /// </param>
        /// <returns></returns>
        public async Task<Playlist> CreateSpotifyPlaylist(string name, string description, IEnumerable<string> trackIds, string base64Jpg = null)
        {
            try
            {
                var playlist = await SpotifyApi.CreateSpotifyPlaylist(name, description, trackIds, base64Jpg);
                var converted = ConvertPlaylists(new List<FullPlaylist> { playlist });
                return converted.FirstOrDefault();
            }
            catch (Exception)
            {
                await Helpers.DisplayDialog("Error", "An error occured while creating your playlist");
                return null;
            }
        }

        public async Task<Playlist> UpdatePlaylist(string id, string name, string description, string base64Jpg)
        {
            try
            {
                var updatedPlaylist = await SpotifyApi.UpdatePlaylist(id, name, description, base64Jpg);
                return (ConvertPlaylists(new List<FullPlaylist> { updatedPlaylist })).FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<string>> UnfollowSpotifyPlaylist(IEnumerable<string> ids)
        {
            try
            {
                return await SpotifyApi.UnfollowSpotifyPlaylist(ids);
            }
            catch (Exception)
            {
                await Helpers.DisplayDialog("Error", "An error occured while creating your playlist");
                return null;
            }
        }

        /// <summary>
        /// Merges 2 or more playlists into 1 new playlist.
        /// </summary>
        /// <param name="name">
        /// The name of the new playlist.
        /// </param>
        /// <param name="playlists">
        /// The list of playlists to merge.
        /// </param>
        /// <param name="base64Jpg">
        /// The playlist cover in base64 string format (Optional). 
        /// </param>
        /// <param name="img">
        /// The cover image if using a custom cover image for the playlist.
        /// </param>
        /// <returns>
        /// The newly create playlist.
        /// </returns>
        public async Task<Playlist> MergeSpotifyPlaylists(string name, string description, IEnumerable<Playlist> playlists, string base64Jpg = null)
        {
            try
            {
                if (playlists.Count() <= 1)
                    return null;

                List<string> trackIds = new List<string>();
                foreach (var playlist in playlists)
                {
                    var items = await GetTrackIds(playlist.Id);
                    if (items != null)
                    {
                        foreach (var trackId in items)
                        {
                            if (!trackIds.Contains(trackId))
                            {
                                trackIds.Add(trackId);
                            }
                        }
                    }
                }
                return await CreateSpotifyPlaylist(name, description, trackIds, base64Jpg);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> PlaySpotifyMedia(IEnumerable<string> uris, int index = 0)
        {
            try
            {
                if (await SpotifyApi.PlayMedia(uris.ToList(), index))
                {
                    return true;
                }
                else
                {
                    return await Helpers.OpenSpotifyAppAsync(uris.ToList()[index], null);
                }
            }
            catch (Exception)
            {
                return await Helpers.OpenSpotifyAppAsync(uris.ToList()[index], null);
            }
        }

        public List<Playlist> ConvertPlaylists(IEnumerable<FullPlaylist> playlists)
        {
            try
            {
                List<Playlist> results = new List<Playlist>();
                User owner = null;
                int itemsCount = 0;
                string imgUrl = null;
                PlaylistCategoryType type = PlaylistCategoryType.Default;

                foreach (var item in playlists)
                {
                    if (item.Owner != null)
                    {
                        owner = new User(item.Owner.Id,
                            item.Owner.DisplayName,
                            item.Owner.Uri,
                            "",
                            false);

                        if (_loggedInUserProfile != null)
                        {
                            if (item.Owner.Id == _loggedInUserProfile.Id)
                                type = PlaylistCategoryType.MyPlaylist;
                            else if (item.Owner.DisplayName.ToLower() == "spotify")
                                type = PlaylistCategoryType.Spotify;
                            else
                                type = PlaylistCategoryType.Following;
                        }
                    }

                    if (item.Tracks != null && item.Tracks.Total.HasValue) itemsCount = item.Tracks.Total.Value;
                    if (item.Images != null && item.Images.FirstOrDefault() != null) imgUrl = item.Images.FirstOrDefault().Url;

                    results.Add(new Playlist(item.Id,
                        item.Name,
                        item.Uri,
                        imgUrl,
                        item.Description,
                        itemsCount,
                        "0",
                        owner,
                        type));

                    itemsCount = 0;
                    type = PlaylistCategoryType.Default;
                }
                return results;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public List<Playlist> ConvertPlaylists(IEnumerable<SimplePlaylist> playlists)
        {
            try
            {
                List<Playlist> results = new List<Playlist>();
                User owner = null;
                int itemsCount = 0;
                string imgUrl = null;
                PlaylistCategoryType type = PlaylistCategoryType.Default;

                foreach (var item in playlists)
                {
                    if (item.Owner != null)
                    {
                        owner = new User(item.Owner.Id,
                            item.Owner.DisplayName,
                            item.Owner.Uri,
                            "", false);

                        if (_loggedInUserProfile != null)
                        {
                            if (item.Owner.Id == _loggedInUserProfile.Id)
                                type = PlaylistCategoryType.MyPlaylist;
                            else if (item.Owner.DisplayName.ToLower() == "spotify")
                                type = PlaylistCategoryType.Spotify;
                            else
                                type = PlaylistCategoryType.Following;
                        }
                    }
                    if (item.Tracks != null && item.Tracks.Total.HasValue) itemsCount = item.Tracks.Total.Value;
                    if (item.Images != null && item.Images.FirstOrDefault() != null) imgUrl = item.Images.FirstOrDefault().Url;

                    results.Add(new Playlist(item.Id,
                        item.Name,
                        item.Uri,
                        imgUrl,
                        item.Description,
                        itemsCount,
                        "0",
                        owner,
                        type));

                    itemsCount = 0;
                    type = PlaylistCategoryType.Default;
                }
                return results;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<string>> GetTrackIds(string id)
        {
            try
            {
                List<string> items = new List<string>();
                Paging<PlaylistTrack<IPlayableItem>> _tracksPage = await SpotifyApi.GetTracksPage(id);
                foreach (var item in _tracksPage.Items)
                {
                    if (item.Track is FullTrack track)
                    {
                        if (items.Find(c => c == track.Uri) == null) items.Add(track.Uri);
                    }
                }

                if (items.Count < _tracksPage.Total)
                {
                    var tracks = await SpotifyApi.GetTracks(_tracksPage);
                    foreach (var item in tracks)
                    {
                        if (item.Track is FullTrack track)
                        {
                            if (items.Find(c => c == track.Uri) == null) items.Add(track.Uri);
                        }
                    }
                }
                return items;
            }
            catch (Exception)
            {
                await Helpers.DisplayDialog("Error", "An error occured, please give it another shot and make sure your internet connection is working");
                return null;
            }
        }

        public async Task<List<string>> GetPlaylistTrackUris(string id, int total)
        {
            try
            {
                startIndex = 0;
                var page = await SpotifyApi.GetPlaylistTrackUris(id, startIndex, limit);
                if (page != null && page.Items != null)
                {
                    startIndex += page.Items.Count;
                    List<string> items = new List<string>();
                    foreach (var item in page.Items)
                    {
                        if (item.Track is FullTrack track)
                        {
                            if (!items.Contains(track.Uri)) items.Add(track.Uri);
                        }
                    }

                    while (startIndex < total)
                    {
                        var _page = await SpotifyApi.GetPlaylistTrackUris(id, startIndex, limit);
                        startIndex += page.Items.Count;

                        foreach (var item in _page.Items)
                        {
                            if (item.Track is FullTrack track)
                            {
                                if (!items.Contains(track.Uri)) items.Add(track.Uri);
                            }
                        }
                    }
                    return items;
                }
                return null;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<Track>> GetPlaylistTracks(string id, int total)
        {
            try
            {
                startIndex = 0;
                var page = await SpotifyApi.GetPlaylistTrackUris(id, startIndex, limit);
                if (page != null && page.Items != null)
                {
                    startIndex += page.Items.Count;
                    List<Track> items = new List<Track>();
                    items.AddRange(ConvertTracks(page.Items));

                    while (startIndex < total)
                    {
                        var _page = await SpotifyApi.GetPlaylistTrackUris(id, startIndex, limit);

                        if (_page == null) break;
                        startIndex += page.Items.Count;

                        var _items = ConvertTracks(_page.Items);
                        if (_items != null)
                        {
                            foreach (var item in _items)
                            {
                                if (items.Find(c => c.Id == item.Id) == null) 
                                    items.Add(item);
                            }
                        }
                    }
                    return items;
                }
                return null;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<Track>> GetTracksPaged(string id, int startIndex, int limit = 20)
        {
            try
            {
                var page = await SpotifyApi.GetPlaylistTrackUris(id, startIndex, limit);
                if (page != null && page.Items != null)
                {
                    return ConvertTracks(page.Items);
                }
                return null;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<SnapshotResponse> RemoveFromPlaylist(string playlistId, IEnumerable<string> uris)
        {
            try
            {
                return await SpotifyApi.RemoveFromPlaylist(playlistId, uris);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private List<Track> ConvertTracks(IEnumerable<PlaylistTrack<IPlayableItem>> tracks)
        {
            try
            {
                List<Track> results = new List<Track>();
                string artist = "";
                string album = "";
                string image = "";
                foreach (var playlistTrack in tracks)
                {
                    try
                    {
                        if (playlistTrack.Track is FullTrack track)
                        {
                            if (track.Album != null)
                            {
                                album = track.Album.Name;
                                if (track.Album.Images != null && track.Album.Images.Count > 0)
                                    image = track.Album.Images.FirstOrDefault().Url;
                            }

                            if (track.Artists != null && track.Artists.Count > 0)
                            {
                                if (track.Artists.Count == 1)
                                    artist = track.Artists.FirstOrDefault().Name;
                                else
                                {
                                    for (int i = 0; i < track.Artists.Count; i++)
                                    {
                                        if (i != (track.Artists.Count - 1))
                                            artist += track.Artists[i].Name + ", ";
                                        else
                                            artist += track.Artists[i].Name;
                                    }
                                }
                            }
                            else
                                artist = "Unknown";
                           
                            results.Add(new Track(track.Id,
                                track.Name,
                                track.Uri,
                                image,
                                artist,
                                album,
                                track.DurationMs,
                                track.Explicit));
                        }
                    }
                    catch (Exception)
                    {

                        throw;
                    }

                    artist = "";
                    album = "";
                    image = "";
                }

                return results;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> PlaybackMediaItem(MediaItemBase item, int index = 0)
        {
            return await SpotifyApi.PlaybackMediaItem(item.Uri, index);
        }

        public static async Task<bool> PlaybackItems(List<string> uris, int index = 0)
        {
            return await SpotifyApi.PlaybackItems(uris, index);
        }

        public async Task<bool> AddToQueue(string uri)
        {
            return await SpotifyApi.AddToQueue(uri);
        }

        internal async Task<bool> AddToPlaylist(IEnumerable<string> trackUris, string playlistId)
        {
            return await SpotifyApi.AddToPlaylist(trackUris, playlistId);
        }
    }
}
