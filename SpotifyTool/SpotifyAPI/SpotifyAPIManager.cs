using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyTool.Config;
using SpotifyTool.SpotifyObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyAPI
{
    public class SpotifyAPIManager : ClientManager
    {
        private static SpotifyAPIManager _Instance = null;
        public static new SpotifyAPIManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new SpotifyAPIManager();
                }
                return _Instance;
            }
        }

        private EmbedIOAuthServer _server;
        public event Action OnLogin;

        protected SpotifyAPIManager() : base()
        {
        }

        public async Task LogInRequest()
        {
            // Make sure "http://localhost:5000/callback" is in your spotify application as redirect uri!
            this._server = new EmbedIOAuthServer(new Uri("http://localhost:5000/callback"), 5000);
            Task serverStartTask = this._server.Start();
            Task<KeyValuePair<string, string>> appIDSecretTask = ConfigManager.GetClientIDAndSecretOrFromConsole();
            await Task.WhenAll(serverStartTask, appIDSecretTask);
            this._server.AuthorizationCodeReceived += this.OnAuthorizationCodeReceived;

            LoginRequest request = new LoginRequest(this._server.BaseUri, appIDSecretTask.Result.Key, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> {
                    Scopes.AppRemoteControl,
                    Scopes.PlaylistModifyPrivate,
                    Scopes.PlaylistModifyPublic,
                    Scopes.PlaylistReadCollaborative,
                    Scopes.PlaylistReadPrivate,
                    Scopes.Streaming,
                    Scopes.UserFollowRead,
                    Scopes.UserLibraryRead,
                    Scopes.UserModifyPlaybackState,
                    Scopes.UserReadCurrentlyPlaying,
                    Scopes.UserReadPlaybackPosition,
                    Scopes.UserReadPlaybackState,
                    Scopes.UserReadPrivate,
                    Scopes.UserReadRecentlyPlayed,
                    Scopes.UserTopRead }
            };
            Uri uri = request.ToUri();
            BrowserUtil.Open(uri);
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            Task serverStopTask = this._server.Stop();
            Task<KeyValuePair<string, string>> appIDSecretTask = ConfigManager.GetClientIDAndSecretOrFromConsole();
            await Task.WhenAll(serverStopTask, appIDSecretTask);
            this._server.Dispose();
            KeyValuePair<string, string> appIDAndSecret = appIDSecretTask.Result;
            AuthorizationCodeTokenResponse tokenResponse = await new OAuthClient().RequestToken(
              new AuthorizationCodeTokenRequest(appIDAndSecret.Key, appIDAndSecret.Value, response.Code, new Uri("http://localhost:5000/callback"))
            );
            SpotifyClientConfig spotifyConfig = SpotifyClientConfig
              .CreateDefault()
              .WithAuthenticator(new AuthorizationCodeAuthenticator(appIDAndSecret.Key, appIDAndSecret.Value, tokenResponse));
            SpotifyClient spotifyClient = new SpotifyClient(spotifyConfig);
            this.SetSpotifyClient(spotifyClient);
            OnLogin.Invoke();
        }

        public async Task<IList<SavedTrack>> GetLikedTracks()
        {
            PrivateUser user = await this.GetUser();
            SpotifyClient client = await this.GetSpotifyClient();
            Paging<SavedTrack> firstPage = await client.Library.GetTracks();
            return await this.PaginateAll(firstPage);
        }

        public Task<PrivateUser> GetUser()
        {
            return this.GetUser(0);
        }

        public async Task<PrivateUser> GetUser(int retries)
        {
            try
            {
                SpotifyClient client = await this.GetSpotifyClient();
                return await client.UserProfile.Current();
            }
            catch (Exception ex)
            {
                APIException apiEx = null;
                if (ex is AggregateException aex && aex.InnerExceptions.Count == 1 && aex.InnerExceptions[0] is APIException apiEx2)
                {
                    apiEx = apiEx2;
                }
                else if (ex is APIException apiEx3)
                {
                    apiEx = apiEx3;
                }
                if (apiEx == null || retries > 1)
                {
                    throw ex;
                }
            }
            TaskCompletionSource<PrivateUser> loginTaskCompletionSource = new TaskCompletionSource<PrivateUser>();
            async void onLoginCompetion()
            {
                loginTaskCompletionSource.SetResult(await this.GetUser(retries + 1));
                OnLogin -= onLoginCompetion;
            }

            OnLogin += onLoginCompetion;
            await this.LogInRequest();
            return await loginTaskCompletionSource.Task;
        }

        public async Task<List<SimplePlaylist>> GetPlaylistsFromCurrentUser()
        {
            PrivateUser user = await this.GetUser();
            string userID = user.Id;
            SpotifyClient spotifyClient = await this.GetSpotifyClient();
            Paging<SimplePlaylist> playlistsFirstPage = await spotifyClient.Playlists.GetUsers(userID);
            IList<SimplePlaylist> allPlaylists = await spotifyClient.PaginateAll(playlistsFirstPage);
            return allPlaylists.Where(p => p.Owner.Id == userID).ToList();
        }

        public async Task<IList<T>> PaginateAll<T>(IPaginatable<T> firstPage)
        {
            SpotifyClient client = await this.GetSpotifyClient();
            return await client.PaginateAll(firstPage);
        }

        public async Task<IList<PlaylistTrack<IPlayableItem>>> GetAllItemsFromPlaylist(string plID)
        {
            SpotifyClient client = await this.GetSpotifyClient();
            PrivateUser user = await this.GetUser();
            Paging<PlaylistTrack<IPlayableItem>> firstPage = await client.Playlists.GetItems(plID, new PlaylistGetItemsRequest()
            {
                Market = user.Country
            });
            return await this.PaginateAll(firstPage);
        }

        public async Task BatchAdd(string playlistID, List<string> trackURIs)
        {
            const int spotifyMaxAdd = 100;
            SpotifyClient manager = await this.GetSpotifyClient();
            ICollection<Task> tasks = new LinkedList<Task>();
            for (int i = 0; i < trackURIs.Count; i += spotifyMaxAdd)
            {
                //i equals taken elements
                int toTake = trackURIs.Count < (i + spotifyMaxAdd) ? trackURIs.Count - i : spotifyMaxAdd;
                List<string> toAdd = trackURIs.GetRange(i, toTake);
                Task<SnapshotResponse> task = manager.Playlists.AddItems(playlistID, new PlaylistAddItemsRequest(toAdd));
                tasks.Add(task);
            }
            await Task.WhenAll(tasks.ToArray());
        }

        public async Task<bool> IsCurrentUserOwner(SimplePlaylist playlist)
        {
            PrivateUser currentUser = await this.GetUser();
            return currentUser.Id == playlist.Owner.Id;
        }

        public async Task<bool> IsCurrentUserOwner(string playlistID)
        {
            List<SimplePlaylist> userPlaylists = await this.GetPlaylistsFromCurrentUser();
            return userPlaylists.Any(p => p.Id == playlistID);
        }

        public async Task RemoveFromPlaylist(string playlistID, string spotifyURI)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            await manager.Playlists.RemoveItems(playlistID, new PlaylistRemoveItemsRequest()
            {
                Tracks = new List<PlaylistRemoveItemsRequest.Item>() {
                    new PlaylistRemoveItemsRequest.Item() {
                        Uri = spotifyURI
                    }
                }
            });
        }

        public async Task Unlike(string spotifyID)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            await manager.Library.RemoveTracks(new LibraryRemoveTracksRequest(new List<string>() { spotifyID }));
        }
    }
}
