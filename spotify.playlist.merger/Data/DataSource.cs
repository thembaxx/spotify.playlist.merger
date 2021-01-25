using spotify.playlist.merger.Model;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace spotify.playlist.merger.Data
{
    public class DataSource
    {
        public static DataSource Current = null;

        public DataSource() { Current = this; }

        public List<Playlist> ConvertPlaylists(IEnumerable<FullPlaylist> playlists)
        {
            try
            {
                List<Playlist> results = new List<Playlist>();
                User owner = null;
                int itemsCount = 0;

                foreach (var item in playlists)
                {
                    if (item.Owner != null)
                    {
                        owner = new User(item.Owner.Id,
                            item.Owner.DisplayName,
                            item.Owner.Uri,
                            item.Owner.Images.FirstOrDefault().Url);
                    }

                    //if (item.Tracks != null) itemsCount = item.Tracks.Total;

                    results.Add(new Playlist(item.Id,
                        item.Name,
                        item.Uri,
                        item.Images.FirstOrDefault().Url,
                        item.Description,
                        itemsCount,
                        "0"));

                    itemsCount = 0;
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

                foreach (var item in playlists)
                {
                    if (item.Owner != null)
                    {
                        owner = new User(item.Owner.Id,
                            item.Owner.DisplayName,
                            item.Owner.Uri,
                            item.Owner.Images.FirstOrDefault().Url);
                    }

                    //if (item.Tracks != null) itemsCount = item.Tracks.Total;

                    results.Add(new Playlist(item.Id,
                        item.Name,
                        item.Uri,
                        item.Images.FirstOrDefault().Url,
                        item.Description,
                        itemsCount,
                        "0"));

                    itemsCount = 0;
                }
                return results;
            }
            catch (Exception)
            {
                return null;
            }
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
                var user = await SpotifyApi.GetProfile();
                return new User(user.Id, user.DisplayName, user.Uri, user.Images.FirstOrDefault().Url);
            }
            catch (Exception e)
            {
                Helpers.DisplayDialog("Error", e.Message);
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
                Helpers.DisplayDialog("Error", "An error occured while creating your playlist");
                return null;
            }
        }

        private int startIndex = 0;
        private readonly int limit = 20;
        private Paging<SimplePlaylist> page;

        /// <summary>
        /// Gets a list of the current users playlists, including following and own playlists.
        /// </summary>
        /// <returns>
        /// The list of playlists.
        /// </returns>
        public async Task<bool> GetPlaylists()
        {
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


            //add to collection
            return true;
        }

        /// <summary>
        /// //Unfollows (Delete if user is owner) a list of playlists that current user follows.
        /// </summary>
        /// <param name="ids">
        /// The list of playlist ids the user wants to unfollow.
        /// </param>
        /// <returns>
        /// A list of ids of the playlists that were succesfuly unfollowed.
        /// </returns>
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

        private Paging<PlaylistTrack<IPlayableItem>> _tracksPage;

        public async Task<List<string>> GetTrackIds(string id)
        {
            try
            {
                List<string> items = new List<string>();
                _tracksPage = await SpotifyApi.GetTracksPage(id);
                foreach (var item in _tracksPage.Items)
                {
                    if (item.Track is FullTrack track)
                    {
                        items.Add(track.Uri);
                    }
                }
                var tracks = await SpotifyApi.GetTracks(_tracksPage);
                foreach (var item in tracks)
                {
                    if (item.Track is FullTrack track)
                    {
                        items.Add(track.Uri);
                    }
                }
                return items;
            }
            catch (Exception)
            {
                _tracksPage = null;
                Helpers.DisplayDialog("Error", "An error occured, please give it another shot and make sure your internet connection is working");
                return null;
            }
        }
    }
}
