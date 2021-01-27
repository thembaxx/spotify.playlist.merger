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
        public static SpotifyClient SpotifyClient { get; set; }
        private static PrivateUser _user;

        #region Authentication

        private static readonly string CredentialsPath = ApplicationData.Current.LocalFolder.Path + "\\credentials.json";
        private static string clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
        private static string clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");
        private static readonly EmbedIOAuthServer _server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);

        /// <summary>
        /// Checks if user is authenticated
        /// </summary>
        /// <returns>
        /// Null if failed, Profile if successful
        /// </returns>
        public static async Task<PrivateUser> IsAuthenticated()
        {
            //check if file with token exists, if it does not exist, login will be shown
            if (!File.Exists(CredentialsPath))
                return null;

            var json = await File.ReadAllTextAsync(CredentialsPath);
            var token = JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(json);

            CheckCliendSecretId();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                return null;

            var authenticator = new AuthorizationCodeAuthenticator(clientId, clientSecret, token);
            authenticator.TokenRefreshed += (sender, tokenx) => File.WriteAllText(CredentialsPath, JsonConvert.SerializeObject(tokenx));

            //might throw an error if user revoked access to their spotify account
            var config = SpotifyClientConfig.CreateDefault()
              .WithAuthenticator(authenticator);

            SpotifyClient = new SpotifyClient(config);
            //try and get user profile
            return await SpotifyClient.UserProfile.Current();
        }

        public static async Task<SpotifyClient> GetSpotifyClientAsync()
        {
            if (SpotifyClient == null)
                await Authenticate();

            //check if client is valid
            try
            {
                await SpotifyClient.UserProfile.Current();
                return SpotifyClient;
            }
            catch (Exception)
            {
                //error, try to re-authenticate
                return await Authenticate();
            }
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
                    Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_ID", "");
                    clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
                }

                if (string.IsNullOrEmpty(clientSecret))
                {
                    Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_SECRET", "");
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
            var spotify = await GetSpotifyClientAsync();
            _user = await spotify.UserProfile.Current();
            return _user;
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
        public static async Task<FullPlaylist> CreateSpotifyPlaylist(string name, string description, IEnumerable<string> trackIds, string base64Jpg = null)
        {
            var spotify = await GetSpotifyClientAsync();
            PlaylistCreateRequest request = new PlaylistCreateRequest(name);
            if (!string.IsNullOrEmpty(description)) request.Description = description;
            var playlist = await spotify.Playlists.Create(_user.Id, request);

            var plRequest = new PlaylistAddItemsRequest(trackIds.ToList());
            await spotify.Playlists.AddItems(playlist.Id, plRequest);

            try
            {
                if (!string.IsNullOrEmpty(base64Jpg))
                    await spotify.Playlists.UploadCover(playlist.Id, base64Jpg); //how to handle image data thats > 256kb?
            }
            catch (Exception)
            {

            }
            return await spotify.Playlists.Get(playlist.Id);
        }

        public static async Task<Paging<SimplePlaylist>> GetInitialPlaylist(int limit)
        {
            PlaylistCurrentUsersRequest request = new PlaylistCurrentUsersRequest
            {
                Limit = limit
            };

            var spotify = await GetSpotifyClientAsync();
            return await spotify.Playlists.CurrentUsers(request);
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

            var spotify = await GetSpotifyClientAsync();
            var result = await spotify.Playlists.CurrentUsers(request);

            return result.Items;
        }

        public static async Task<Paging<PlaylistTrack<IPlayableItem>>> GetTracksPage(string id)
        {
            var spotify = await GetSpotifyClientAsync();
            var fullPlaylist = await spotify.Playlists.Get(id);
            return fullPlaylist.Tracks;
        }

        public static async Task<IList<PlaylistTrack<IPlayableItem>>> GetTracks(Paging<PlaylistTrack<IPlayableItem>> page)
        {
            var spotify = await GetSpotifyClientAsync();
            return await spotify.PaginateAll(page);
        }
    }

}
