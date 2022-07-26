using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyAPI
{
    public class SpotifyAPIManager : UserManager
    {
        public const int MaxPlaylistTrackModify = 100;
        public const int MaxLibraryTrackModify = 50;
        public const int MaxAlbums = 20;

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

        protected SpotifyAPIManager() : base()
        {
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
            SpotifyClient manager = await this.GetSpotifyClient();
            PrivateUser user = await this.GetUser();

            ArtistsAlbumsRequest.IncludeGroups groups = ArtistsAlbumsRequest.IncludeGroups.Album | ArtistsAlbumsRequest.IncludeGroups.AppearsOn | ArtistsAlbumsRequest.IncludeGroups.Single;
            ArtistsAlbumsRequest artistsAlbumsRequest;
            if (userMarket)
            {
                //Market here is not for track relinking, but for restricting albums to market
                artistsAlbumsRequest = new ArtistsAlbumsRequest() { Market = user.Country, IncludeGroupsParam = groups };
            }
            else
            {
                artistsAlbumsRequest = new ArtistsAlbumsRequest() { IncludeGroupsParam = groups };
            }
            Paging<SimpleAlbum> simpleAlbums = await manager.Artists.GetAlbums(spotifyId, artistsAlbumsRequest);
            IList<SimpleAlbum> allSimpleAlbums = await this.PaginateAll(simpleAlbums);
            List<string> albumIds = allSimpleAlbums.Select(a => a.Id).Distinct().ToList();
            AlbumsResponse[] fullAlbumsResponse = await BatchOperateReturns(albumIds, MaxAlbums, items => manager.Albums.GetSeveral(new AlbumsRequest(items) { Market = user.Country }));
            List<FullAlbum> albums = fullAlbumsResponse.SelectMany(ar => ar.Albums).ToList();
            var allTracksTasks = albums.Select(a => (Album: a, Tracks: this.PaginateAll(a.Tracks)));
            await Task.WhenAll(allTracksTasks.Select(kv => kv.Tracks));
            return allTracksTasks.ToDictionary(kv => kv.Album, kv => kv.Tracks.Result.Where(t => t.Artists.Any(a => a.Id == spotifyId)).ToList());
        }

        public async Task<FullArtist> GetArtist(string artistId)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            return await manager.Artists.Get(artistId);
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
            PlayerAddToQueueRequest request = new PlayerAddToQueueRequest(spotifyUri);
            return await manager.Player.AddToQueue(request);
        }

        private Task BatchOperate<T>(List<T> items, int maxPerRequest, Func<List<T>, Task> executeFunction)
        {
            return BatchOperateReturns(items, maxPerRequest, async batchItems =>
            {
                await executeFunction(batchItems);
                return true;
            });
        }

        private static async Task<Tout[]> BatchOperateReturns<Tin, Tout>(List<Tin> items, int maxPerRequest, Func<List<Tin>, Task<Tout>> executeFunction)
        {
            ICollection<Task<Tout>> tasks = new LinkedList<Task<Tout>>();
            for (int i = 0; i < items.Count; i += maxPerRequest)
            {
                //i equals taken elements
                int toTake = items.Count < (i + maxPerRequest) ? items.Count - i : maxPerRequest;
                List<Tin> toUseInRequest = items.GetRange(i, toTake);
                Task<Tout> task = executeFunction(toUseInRequest);
                tasks.Add(task);
            }
            return await Task.WhenAll(tasks.ToArray());
        }
    }
}
