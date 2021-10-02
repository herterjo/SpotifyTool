using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyTool.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyAPI
{
    public class SpotifyAPIManager : ClientManager
    {
        public const int MaxPlaylistTrackModify = 100;
        public const int MaxLibraryTrackModify = 50;

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

        public Task<PrivateUser> GetUser()
        {
            return this.GetUser(0);
        }

        private async Task<PrivateUser> GetUser(int retries)
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

        public async Task AddToPlaylist(string playlistID, List<string> trackURIs)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            await this.BatchOperate(trackURIs, MaxPlaylistTrackModify, items => manager.Playlists.AddItems(playlistID, new PlaylistAddItemsRequest(items)));
        }

        public async Task RemoveFromPlaylist(string playlistID, List<string> spotifyUris)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            List<PlaylistRemoveItemsRequest.Item> toRemove = spotifyUris.Select(uri => new PlaylistRemoveItemsRequest.Item() { Uri = uri }).ToList();
            await this.BatchOperate(toRemove, MaxPlaylistTrackModify, items => manager.Playlists.RemoveItems(playlistID, new PlaylistRemoveItemsRequest() { Tracks = items }));
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

        public async Task<IList<SavedTrack>> GetLikedTracks()
        {
            PrivateUser user = await this.GetUser();
            SpotifyClient client = await this.GetSpotifyClient();
            Paging<SavedTrack> firstPage = await client.Library.GetTracks(new LibraryTracksRequest()
            {
                Market = user.Country
            });
            return await this.PaginateAll(firstPage);
        }

        public async Task UnlikeTracks(List<string> spotifyIDs)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            await this.BatchOperate(spotifyIDs, MaxLibraryTrackModify, items => manager.Library.RemoveTracks(new LibraryRemoveTracksRequest(items)));
        }

        public async Task LikeTracks(List<string> spotifyIDs)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            await this.BatchOperate(spotifyIDs, MaxLibraryTrackModify, items => manager.Library.SaveTracks(new LibrarySaveTracksRequest(items)));
        }

        public async Task<List<FullTrack>> GetAllArtistTopTracks(string spotifyId)
        {
            Task<SpotifyClient> managerTask = this.GetSpotifyClient();
            Task<PrivateUser> userTask = this.GetUser();
            await Task.WhenAll(managerTask, userTask);
            SpotifyClient manager = managerTask.Result;
            PrivateUser user = userTask.Result;
            ArtistsTopTracksResponse response = await manager.Artists.GetTopTracks(spotifyId, new ArtistsTopTracksRequest(user.Country));
            return response.Tracks;
        }

        public async Task<Dictionary<FullAlbum, List<SimpleTrack>>> GetAllArtistTracks(string spotifyId, bool userMarket)
        {
            Task<SpotifyClient> managerTask = this.GetSpotifyClient();
            Task<PrivateUser> userTask = this.GetUser();
            
            await Task.WhenAll(managerTask, userTask);
            SpotifyClient manager = managerTask.Result;
            ArtistsAlbumsRequest.IncludeGroups groups = ArtistsAlbumsRequest.IncludeGroups.Album | ArtistsAlbumsRequest.IncludeGroups.AppearsOn | ArtistsAlbumsRequest.IncludeGroups.Single;
            ArtistsAlbumsRequest artistsAlbumsRequest;
            if (userMarket)
            {
                //Market here is not for track relinking, but for restricting albums to market
                artistsAlbumsRequest = new ArtistsAlbumsRequest() { Market = userTask.Result.Country, IncludeGroupsParam = groups };
            }
            else
            {
                artistsAlbumsRequest = new ArtistsAlbumsRequest() { IncludeGroupsParam = groups };
            }
            Paging<SimpleAlbum> simpleAlbums = await manager.Artists.GetAlbums(spotifyId, artistsAlbumsRequest);
            IList<SimpleAlbum> allSimpleAlbums = await this.PaginateAll(simpleAlbums);
            List<string> albumIds = allSimpleAlbums.Select(a => a.Id).Distinct().ToList();
            AlbumsRequest albumsRequest = new AlbumsRequest(albumIds) { Market = userTask.Result.Country };
            AlbumsResponse fullAlbumsResponse = await manager.Albums.GetSeveral(albumsRequest);
            IEnumerable<KeyValuePair<FullAlbum, Task<IList<SimpleTrack>>>> allTracksTasks = fullAlbumsResponse.Albums.Select(a => new KeyValuePair<FullAlbum, Task<IList<SimpleTrack>>>(a, this.PaginateAll(a.Tracks)));
            await Task.WhenAll(allTracksTasks.Select(kv => kv.Value));
            return allTracksTasks.ToDictionary(kv => kv.Key, kv => kv.Value.Result.Where(t => t.Artists.Any(a => a.Id == spotifyId)).ToList());
        }

        public async Task<List<FullTrack>> GetMultipleTracks(IEnumerable<string> spotifyIds)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            TracksResponse response = await manager.Tracks.GetSeveral(new TracksRequest(spotifyIds.ToList()));
            return response.Tracks;
        }

        public async Task<bool> QueueTrack(string spotifyUri)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            var request = new PlayerAddToQueueRequest(spotifyUri);
            return await manager.Player.AddToQueue(request);
        }

        private async Task BatchOperate<T>(List<T> items, int maxPerRequest, Func<List<T>, Task> executeFunction)
        {
            ICollection<Task> tasks = new LinkedList<Task>();
            for (int i = 0; i < items.Count; i += MaxPlaylistTrackModify)
            {
                //i equals taken elements
                int toTake = items.Count < (i + maxPerRequest) ? items.Count - i : maxPerRequest;
                List<T> toUseInRequest = items.GetRange(i, toTake);
                Task task = executeFunction(items);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks.ToArray());
        }
    }
}
