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
        private readonly int limit = 20;

        public static DataSource Current = new DataSource();

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

        #region Playback

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

        #endregion

        #region Playlists

        private Paging<SimplePlaylist> _tempPage = null;
        public async Task<int> GetPlaylistsCountAsync()
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
                    var items = await SpotifyApi.GetPlaylistsAsync(startIndex, limit);
                    return ConvertPlaylists(items);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Playlist> GetFullPlaylistAsync(string playlistId)
        {
            try
            {
                var item = await SpotifyApi.GetPlaylistAsync(playlistId);
                return ConvertPlaylists(new List<FullPlaylist> { item }).FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal async Task<bool> AddToPlaylist(IEnumerable<string> trackUris, string playlistId)
        {
            return await SpotifyApi.AddToPlaylist(trackUris, playlistId);
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
        public async Task<Playlist> CreateSpotifyPlaylist(string name, string description, string base64Jpg = null)
        {
            try
            {
                var playlist = await SpotifyApi.CreateSpotifyPlaylist(name, description, base64Jpg);
                var converted = ConvertPlaylists(new List<FullPlaylist> { playlist });
                return converted.FirstOrDefault();
            }
            catch (Exception)
            {
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

        #endregion

        #region Tracks

        public async Task<List<Track>> GetTracksAsync(string playlistId, int startIndex, int limit = 20)
        {
            try
            {
                var page = await SpotifyApi.GetTracksAsync(playlistId, startIndex, limit);
                if (page == null || page.Items == null) return null;
                return ConvertTracks(page.Items);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<string>> GetTrackIdsAsync(string playlistId, int startIndex, int limit = 20)
        {
            try
            {
                var page = await SpotifyApi.GetTracksAsync(playlistId, startIndex, limit);
                if (page == null || page.Items == null) return null;

                List<string> ids = new List<string>();
                foreach (var item in page.Items)
                {
                    if (item.Track is FullTrack track) 
                        ids.Add(track.Id);
                }
                return ids;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<string>> GetTrackUrisAsync(string playlistId, int startIndex, int limit = 20)
        {
            try
            {
                var page = await SpotifyApi.GetTracksAsync(playlistId, startIndex, limit);
                if (page == null || page.Items == null) return null;

                List<string> uris = new List<string>();
                foreach (var item in page.Items)
                {
                    if (item.Track is FullTrack track)
                        uris.Add(track.Uri);
                }
                return uris;
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
                DateTime dateAdded = new DateTime();
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

                            if (playlistTrack.AddedAt.HasValue) dateAdded = playlistTrack.AddedAt.Value;

                            results.Add(new Track(track.Id,
                                track.Name,
                                track.Uri,
                                image,
                                artist,
                                album,
                                track.DurationMs,
                                track.Explicit,
                                dateAdded));
                        }
                    }
                    catch (Exception)
                    {

                        throw;
                    }

                    artist = "";
                    album = "";
                    image = "";
                    dateAdded = new DateTime();
                }

                return results;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}
