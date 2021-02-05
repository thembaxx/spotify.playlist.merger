using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Windows.Storage;
using static SpotifyAPI.Web.Scopes;

namespace spotify.playlist.merger.Data
{
    public class SpotifyApi
    {

        private static readonly string CredentialsPath = ApplicationData.Current.LocalFolder.Path + "\\credentials.json";
        private static string clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
        private static string clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
        private static readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);

        public static SpotifyClient SpotifyClient { get; set; }
        private static PrivateUser _user;

        public static bool LogOut()
        {
            SpotifyClient = null;
            _user = null;
            File.Delete(CredentialsPath);
            return true;
        }

        #region Authentication

        /// <summary>
        /// Checks if user is authenticated
        /// </summary>
        /// <returns>
        /// Null if failed, Profile if successful
        /// </returns>
        public static async Task<bool> IsAuthenticated()
        {
            //check if file with token exists, if it does not exist, login will be shown
            if (!File.Exists(CredentialsPath))
                return false;

            var json = await File.ReadAllTextAsync(CredentialsPath);
            var token = JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(json);

            CheckCliendSecretId();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                return false;

            var authenticator = new AuthorizationCodeAuthenticator(clientId, clientSecret, token);
            authenticator.TokenRefreshed += (sender, tokenx) => File.WriteAllText(CredentialsPath, JsonConvert.SerializeObject(tokenx));

            //might throw an error if user revoked access to their spotify account
            var config = SpotifyClientConfig.CreateDefault()
              .WithAuthenticator(authenticator);

            SpotifyClient = new SpotifyClient(config);
            //try and get user profile
            _user = await SpotifyClient.UserProfile.Current();
            return (_user != null);
        }

        private static async Task<bool> IsClientValid()
        {
            try
            {
                _user = await SpotifyClient.UserProfile.Current();
                return (_user != null);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static async Task<SpotifyClient> GetSpotifyClientAsync()
        {
            if (SpotifyClient == null)
                await Authenticate();

            if (clientId == null || clientSecret == null)
                return null;

            return SpotifyClient;
        }

        public static async Task<SpotifyClient> Authenticate()
        {
            CheckCliendSecretId();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                return null;

            var request = new LoginRequest(_server.BaseUri, clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { UserReadPrivate, PlaylistReadPrivate, UserModifyPlaybackState,
                    UserLibraryModify, UserLibraryRead, PlaylistModifyPrivate,
                    PlaylistModifyPublic, UgcImageUpload }
            };

            Uri uri = request.ToUri();

            Uri StartUri = uri;
            Uri EndUri = new Uri("http://localhost:5000/callback");

            WebAuthenticationResult WebAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(
                                                    WebAuthenticationOptions.None,
                                                    StartUri,
                                                    EndUri);
            if (WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.Success)
            {
                var index = WebAuthenticationResult.ResponseData.IndexOf("code=");
                string code = WebAuthenticationResult.ResponseData.Substring(index + 5);

                var config = SpotifyClientConfig.CreateDefault();
                var tokenResponse = await new OAuthClient(config).RequestToken(
              new AuthorizationCodeTokenRequest(
                clientId, clientSecret, code, EndUri));

                SpotifyClient = new SpotifyClient(tokenResponse.AccessToken);

                try
                {
                    if (!File.Exists(CredentialsPath))
                    {
                        await ApplicationData.Current.LocalFolder.CreateFileAsync("credentials.json");
                    }
                    await File.WriteAllTextAsync(CredentialsPath, JsonConvert.SerializeObject(tokenResponse));
                }
                catch (Exception)
                {

                }
                return SpotifyClient;
            }
            else
                return null;
        }

        private static void CheckCliendSecretId()
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_ID", "de354ca4295141c6ad3a7a07086fbd32");
                    clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
                }

                if (string.IsNullOrEmpty(clientSecret))
                {
                    Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_SECRET", "474efaae7656470b81a4266bebbfc4ad");
                    clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets current users profile.
        /// </summary>
        /// <returns>
        /// The current users profile.
        /// </returns>
        public static async Task<PrivateUser> GetProfile()
        {
            try
            {
                if (_user != null)
                {
                    return _user;
                }
                else
                {
                    _user = await SpotifyClient.UserProfile.Current();
                    return _user;
                }
            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }

                if (SpotifyClient != null && _user != null)
                    return _user;
                else if (SpotifyClient != null && _user == null)
                {
                    _user = await SpotifyClient.UserProfile.Current();
                    return _user;
                }
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
        public static async Task<FullPlaylist> CreateSpotifyPlaylist(string name, string description, IEnumerable<string> trackUris, string base64Jpg = null)
        {
            FullPlaylist playlist;
            PlaylistCreateRequest request = new PlaylistCreateRequest(name);
            if (!string.IsNullOrEmpty(description)) request.Description = description;

            try
            {
                playlist = await SpotifyClient.Playlists.Create(_user.Id, request);
            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }
                playlist = await SpotifyClient.Playlists.Create(_user.Id, request);
            }

            if (SpotifyClient != null && playlist != null)
            {
                var plRequest = new PlaylistAddItemsRequest(trackUris.ToList());
                try
                {
                    await SpotifyClient.Playlists.AddItems(playlist.Id, plRequest);
                }
                catch (Exception)
                {

                }
            }

            try
            {
                if (!string.IsNullOrEmpty(base64Jpg))
                    await SpotifyClient.Playlists.UploadCover(playlist.Id, base64Jpg); //how to handle image data thats > 256kb?
            }
            catch (Exception)
            {

            }
            return await SpotifyClient.Playlists.Get(playlist.Id);
        }

        public static async Task<FullPlaylist> UpdatePlaylist(string id, string name, string description, string base64Jpg = null)
        {
            bool success;
            PlaylistChangeDetailsRequest request = new PlaylistChangeDetailsRequest();

            if (!string.IsNullOrEmpty(name)) request.Name = name;

            if (!string.IsNullOrEmpty(description)) request.Description = description;

            try
            {
                success = await SpotifyClient.Playlists.ChangeDetails(id, request);
            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }

                try
                {
                    if (!string.IsNullOrEmpty(base64Jpg))
                        await SpotifyClient.Playlists.UploadCover(id, base64Jpg); //how to handle image data thats > 256kb?
                }
                catch (Exception)
                {

                }

                success = await SpotifyClient.Playlists.ChangeDetails(id, request);
            }

            return (success) ? await SpotifyClient.Playlists.Get(id) : null;
        }

        public static async Task<Paging<SimplePlaylist>> GetInitialPlaylist(int limit)
        {
            PlaylistCurrentUsersRequest request = new PlaylistCurrentUsersRequest
            {
                Limit = limit
            };

            try
            {
                return await SpotifyClient.Playlists.CurrentUsers(request);
            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }

                return await SpotifyClient.Playlists.CurrentUsers(request);
            }
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
        public static async Task<List<string>> UnfollowSpotifyPlaylist(IEnumerable<string> ids)
        {
            List<string> success = new List<string>();
            var spotify = await GetSpotifyClientAsync();
            foreach (var id in ids)
            {
                try
                {
                    if (await spotify.Follow.UnfollowPlaylist(id))
                        success.Add(id);
                }
                catch (Exception)
                {

                }
            }
            return success;
        }

        /// <summary>
        /// Gets a list of the current users playlists, including following and own playlists.
        /// </summary>
        /// <returns>
        /// The list of playlists.
        /// </returns>
        public static async Task<List<SimplePlaylist>> GetPlaylists(int startIndex, int limit)
        {
            PlaylistCurrentUsersRequest request = new PlaylistCurrentUsersRequest
            {
                Limit = limit,
                Offset = startIndex
            };
           
            try
            {
                return (await SpotifyClient.Playlists.CurrentUsers(request)).Items;
            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }
                return (SpotifyClient != null) ? (await SpotifyClient.Playlists.CurrentUsers(request)).Items : null;
            }
        }

        public static async Task<Paging<PlaylistTrack<IPlayableItem>>> GetTracksPage(string id)
        {
            try
            {
                return (await SpotifyClient.Playlists.Get(id)).Tracks;
            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }
                return (SpotifyClient != null) ? (await SpotifyClient.Playlists.Get(id)).Tracks : null;
            }
        }

        public static async Task<IList<PlaylistTrack<IPlayableItem>>> GetTracks(Paging<PlaylistTrack<IPlayableItem>> page)
        {
            try
            {
                //    PlaylistGetItemsRequest r = new PlaylistGetItemsRequest
                //{
                //    Offset
                //}
                return await SpotifyClient.PaginateAll(page);
            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }
                return (SpotifyClient != null) ? await SpotifyClient.PaginateAll(page) : null;
            }
        }

        public static async Task<Paging<PlaylistTrack<IPlayableItem>>> GetPlaylistTrackUris(string id, int startIndex, int limit)
        {
            PlaylistGetItemsRequest r = new PlaylistGetItemsRequest
            {
                Offset = startIndex,
                Limit = limit,
            };

            try
            {
                r.Fields.Add("items(track)");
                return await SpotifyClient.Playlists.GetItems(id, r);
            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }
                return (SpotifyClient != null) ? await SpotifyClient.Playlists.GetItems(id, r) : null;
            }
        }

        public static async Task<bool> PlayMedia(List<string> uris, int index = 0)
        {
            PlayerResumePlaybackRequest request = new PlayerResumePlaybackRequest
            {
                Uris = uris,
                OffsetParam = new PlayerResumePlaybackRequest.Offset { Position = index },
            };

            try
            {
                return await SpotifyClient.Player.ResumePlayback(request);

            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }
                return (SpotifyClient != null) && await SpotifyClient.Player.ResumePlayback(request);

            }
        }

        public static async Task<Paging<SavedTrack>> GetSavedTracks(int startIndex, int limit)
        {
            var spotify = await GetSpotifyClientAsync();
            LibraryTracksRequest request = new LibraryTracksRequest
            {
                Offset = startIndex,
                Limit = limit,
            };

            try
            {
                return await spotify.Library.GetTracks(request);
            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }
                return (SpotifyClient != null) ? await spotify.Library.GetTracks(request) : null;

            }
        }

        public static async Task<SnapshotResponse> RemoveFromPlaylist(string playlistId, IEnumerable<string> uris)
        {
            List<PlaylistRemoveItemsRequest.Item> items = new List<PlaylistRemoveItemsRequest.Item>();
            foreach (var uri in uris)
            {
                items.Add(new PlaylistRemoveItemsRequest.Item { Uri = uri });
            }
            PlaylistRemoveItemsRequest request = new PlaylistRemoveItemsRequest
            {
                Tracks = items
            };

            try
            {
                return await SpotifyClient.Playlists.RemoveItems(playlistId, request);

            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }
                return (SpotifyClient != null) ? await SpotifyClient.Playlists.RemoveItems(playlistId, request) : null;

            }
        }

        internal static async Task<bool> AddToPlaylist(IEnumerable<string> trackUris, string playlistId)
        {
            PlaylistAddItemsRequest request = new PlaylistAddItemsRequest(trackUris.ToList());
            try
            {
                return (await SpotifyClient.Playlists.AddItems(playlistId, request) != null);

            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }
                return (await SpotifyClient.Playlists.AddItems(playlistId, request) != null);

            }
        }

        public static async Task<bool> PlaybackMediaItem(string uri, int index = 0)
        {
            try
            {
                PlayerResumePlaybackRequest request = new PlayerResumePlaybackRequest
                {
                    ContextUri = uri,
                    OffsetParam = new PlayerResumePlaybackRequest.Offset { Position = index },
                };
               

                return await SpotifyClient.Player.ResumePlayback(request);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<bool> PlaybackItems(List<string> uris, int index = 0)
        {
            try
            {
                PlayerResumePlaybackRequest request = new PlayerResumePlaybackRequest
                {
                    Uris = uris,
                    OffsetParam = new PlayerResumePlaybackRequest.Offset { Position = index },
                };


                return await SpotifyClient.Player.ResumePlayback(request);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<bool> AddToQueue(string uri)
        {
            PlayerAddToQueueRequest request = new PlayerAddToQueueRequest(uri);

            try
            {
                return await SpotifyClient.Player.AddToQueue(request);

            }
            catch (Exception)
            {
                if (SpotifyClient != null && SpotifyClient.LastResponse != null &&
                    SpotifyClient.LastResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                    !await IsClientValid())
                {
                    SpotifyClient = await Authenticate();
                }
                else if (SpotifyClient == null && !await IsAuthenticated())
                {
                    SpotifyClient = await Authenticate();
                }
                return (SpotifyClient != null) && await SpotifyClient.Player.AddToQueue(request);

            }
        }
    }

}
