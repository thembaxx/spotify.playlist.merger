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
                if(_current == null)
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
                if(await SpotifyApi.IsAuthenticated())
                {
                    //get playlists
                } else
                {
                    await SpotifyApi.Authenticate();
                }
            }
            catch (Exception)
            {
                Helpers.DisplayDialog("Authentication Error", "An error occured, please restart app.");
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
                Helpers.DisplayDialog("Error", e.Message);
                return null;
            }
        }

        public async Task<bool> GetPlaylists()
        {
            Messenger.Default.Send(new MessengerHelper
            {
                Item = true,
                Action = MessengerAction.IsLoading,
            });

            if (startIndex == 0)
            {
                page = await SpotifyApi.GetInitialPlaylist(limit);
                startIndex += page.Items.Count;
                var items = ConvertPlaylists(page.Items);

                //add items to collection
                ViewModels.MainPageViewModel.Current.AddToCollection(items);
            }

            //get the rest of the playlists
            while (startIndex < page.Total)
            {
                var results = await SpotifyApi.GetPlaylists(startIndex, limit);
                startIndex += results.Count;
                var items = ConvertPlaylists(results);

                ViewModels.MainPageViewModel.Current.AddToCollection(items);
                //ViewModels.MainPageViewModel.Current.AddToCollection(files);

                //delay to avoid api limit
                //await Task.Delay(3000);
            }

            startIndex = 0;

            Messenger.Default.Send(new MessengerHelper
            {
                Item = false,
                Action = MessengerAction.IsLoading,
            });
            return true;
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
        private async Task<Playlist> CreateSpotifyPlaylist(string name, string description, IEnumerable<string> trackIds, string base64Jpg = null)
        {
            try
            {
                var playlist = await SpotifyApi.CreateSpotifyPlaylist(name, description, trackIds, base64Jpg);
                var converted = ConvertPlaylists(new List<FullPlaylist> { playlist });
                return converted.FirstOrDefault();
            }
            catch (Exception)
            {
                Helpers.DisplayDialog("Error", "An error occured while creating your playlist");
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
                Helpers.DisplayDialog("Error", "An error occured while creating your playlist");
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

        public async Task<bool> PlaySpotifyMedia(List<string> uris, int index = 0, bool shuffle = false)
        {
            try
            {
                if (await SpotifyApi.PlayMedia(uris, index, shuffle))
                {
                    return true;
                }
                else
                {
                    return await Helpers.OpenSpotifyAppAsync(uris[index], null);
                }
            }
            catch (Exception)
            {
                return await Helpers.OpenSpotifyAppAsync(uris[index], null);
            }
        }

        public List<Playlist> ConvertPlaylists(IEnumerable<FullPlaylist> playlists)
        {
            try
            {
                List<Playlist> results = new List<Playlist>();
                User owner = null;
                int itemsCount = 0;
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

                    results.Add(new Playlist(item.Id,
                        item.Name,
                        item.Uri,
                        item.Images.FirstOrDefault().Url,
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

                    results.Add(new Playlist(item.Id,
                        item.Name,
                        item.Uri,
                        item.Images.FirstOrDefault().Url,
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
                        if (items.Find(c => c == track.Id) == null) items.Add(track.Uri);
                    }
                }

                if (items.Count < _tracksPage.Total)
                {
                    var tracks = await SpotifyApi.GetTracks(_tracksPage);
                    foreach (var item in tracks)
                    {
                        if (item.Track is FullTrack track)
                        {
                            if (items.Find(c => c == track.Id) == null) items.Add(track.Uri);
                        }
                    }
                }
                return items;
            }
            catch (Exception)
            {
                Helpers.DisplayDialog("Error", "An error occured, please give it another shot and make sure your internet connection is working");
                return null;
            }
        }

        public async Task GetSavedTracks()
        {
            try
            {
                var tracks = await SpotifyApi.GetSavedTracks(0, 20);
                if (tracks != null && tracks.Items != null)
                {
                    foreach (var item in tracks.Items)
                    {

                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
