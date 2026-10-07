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
        private const string UserMarket = "from_token";
        private static SpotifyAPIManager _Instance = null;
        public static new SpotifyAPIManager Instance
        {
            get
            {
                _Instance ??= new SpotifyAPIManager();
                return _Instance;
            }
        }

        protected SpotifyAPIManager() : base()
        {
        }

        public async Task<List<FullPlaylist>> GetPlaylistsFromCurrentUser()
        {
            PrivateUser user = await this.GetUser();
            string userID = user.Id;
            SpotifyClient spotifyClient = await this.GetSpotifyClient();
            var playlistsFirstPage = await spotifyClient.Playlists.CurrentUsers();
            IList<FullPlaylist> allPlaylists = await spotifyClient.PaginateAll(playlistsFirstPage);
            return allPlaylists.Where(p => p.Owner.Id == userID).ToList();
        }

        public async Task<FullPlaylist> GetPlaylist(string playlistId)
        {
            SpotifyClient spotifyClient = await this.GetSpotifyClient();
            return await spotifyClient.Playlists.Get(playlistId);
        }

        public async Task<IList<T>> PaginateAll<T>(IPaginatable<T> firstPage)
        {
            SpotifyClient client = await this.GetSpotifyClient();
            return await client.PaginateAll(firstPage);
        }

        public async Task<IList<PlaylistTrack<IPlayableItem>>> GetAllItemsFromPlaylist(string plID)
        {
            SpotifyClient client = await this.GetSpotifyClient();
            Paging<PlaylistTrack<IPlayableItem>> firstPage = await client.Playlists.GetPlaylistItems(plID, new PlaylistGetItemsRequest()
            {
                Market = UserMarket
            });
            return await this.PaginateAll(firstPage);
        }

        public async Task AddToPlaylist(string playlistID, List<string> trackURIs)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            await BatchOperate(trackURIs, MaxPlaylistTrackModify, items => manager.Playlists.AddPlaylistItems(playlistID, new PlaylistAddItemsRequest(items)));
        }

        public async Task RemoveFromPlaylist(string playlistID, List<string> spotifyUris)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            List<PlaylistRemoveItemsRequestV2.Item> toRemove = spotifyUris.Select(uri => new PlaylistRemoveItemsRequestV2.Item() { Uri = uri }).ToList();
            await BatchOperate(toRemove, MaxPlaylistTrackModify, items => manager.Playlists.RemovePlaylistItems(playlistID, new PlaylistRemoveItemsRequestV2() { Items = items }));
        }

        public async Task<bool> IsCurrentUserOwner(FullPlaylist playlist)
        {
            PrivateUser currentUser = await this.GetUser();
            return currentUser.Id == playlist.Owner.Id;
        }

        public async Task<bool> IsCurrentUserOwner(string playlistID)
        {
            List<FullPlaylist> userPlaylists = await this.GetPlaylistsFromCurrentUser();
            return userPlaylists.Any(p => p.Id == playlistID);
        }

        public async Task<IList<SavedTrack>> GetLikedTracks()
        {
            SpotifyClient client = await this.GetSpotifyClient();
            Paging<SavedTrack> firstPage = await client.Library.GetTracks(new LibraryTracksRequest()
            {
                Market = UserMarket
            });
            return await this.PaginateAll(firstPage);
        }

        public async Task UnlikeTracks(List<string> spotifyIDs)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            await BatchOperate(spotifyIDs, MaxLibraryTrackModify, items => manager.Library.RemoveItems(new LibraryRemoveItemsRequest(items)));
        }

        public async Task LikeTracks(List<string> spotifyIDs)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            await BatchOperate(spotifyIDs, MaxLibraryTrackModify, items => manager.Library.SaveItems(new LibrarySaveItemsRequest(items)));
        }

        //Currently depecrated, maybe use the api from https://spotify.checkleaked.cc/ to replace https://api.spotify.com/v1/artists/{id}/top-tracks
        //public async Task<List<FullTrack>> GetAllArtistTopTracks(string spotifyId)
        //{
        //    Task<SpotifyClient> managerTask = this.GetSpotifyClient();
        //    Task<PrivateUser> userTask = this.GetUser();
        //    await Task.WhenAll(managerTask, userTask);
        //    SpotifyClient manager = managerTask.Result;
        //    PrivateUser user = userTask.Result;
        //    ArtistsTopTracksResponse response = await manager.Artists.GetTopTracks(spotifyId, new ArtistsTopTracksRequest(user.Country));
        //    return response.Tracks;
        //}

        public async Task<Dictionary<FullAlbum, List<SimpleTrack>>> GetAllArtistTracks(string spotifyId, bool userMarket)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            PrivateUser user = await this.GetUser();

            ArtistsAlbumsRequest.IncludeGroups groups = ArtistsAlbumsRequest.IncludeGroups.Album | ArtistsAlbumsRequest.IncludeGroups.AppearsOn | ArtistsAlbumsRequest.IncludeGroups.Single;
            ArtistsAlbumsRequest artistsAlbumsRequest;
            if (userMarket)
            {
                //Market here is not for track relinking, but for restricting albums to market
                artistsAlbumsRequest = new ArtistsAlbumsRequest() { Market = UserMarket, IncludeGroupsParam = groups };
            }
            else
            {
                artistsAlbumsRequest = new ArtistsAlbumsRequest() { IncludeGroupsParam = groups };
            }
            Paging<SimpleAlbum> simpleAlbums = await manager.Artists.GetAlbums(spotifyId, artistsAlbumsRequest);
            IList<SimpleAlbum> allSimpleAlbums = await this.PaginateAll(simpleAlbums);
            List<string> albumIds = allSimpleAlbums.Select(a => a.Id).Distinct().ToList();
            var albums = new Dictionary<FullAlbum, List<SimpleTrack>>(albumIds.Count);
            foreach (var albumId in albumIds)
            {
                var album = await manager.Albums.Get(albumId, new AlbumRequest() { Market = UserMarket });
                var tracks = await this.PaginateAll(album.Tracks);
                albums.Add(album, tracks.Where(t => t.Artists.Any(a => a.Id == spotifyId)).ToList());
            }
            return albums;
        }

        public async Task<FullArtist> GetArtist(string artistId)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            return await manager.Artists.Get(artistId);
        }

        public async Task<List<FullTrack>> GetMultipleTracks(List<string> spotifyIds)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            var tracks = new List<FullTrack>(spotifyIds.Count);
            foreach (var spotifyId in spotifyIds)
            {
                var track = await manager.Tracks.Get(spotifyId);
                tracks.Add(track);
            }
            return tracks;
        }

        public async Task<bool> QueueTrack(string spotifyUri)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            PlayerAddToQueueRequest request = new PlayerAddToQueueRequest(spotifyUri);
            return await manager.Player.AddToQueue(request);
        }

        private static Task<bool[]> BatchOperate<T>(List<T> items, int maxPerRequest, Func<List<T>, Task> executeFunction)
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

        public async Task<FullPlaylist> CreatePlaylist(string playlistName, string description)
        {
            SpotifyClient manager = await this.GetSpotifyClient();
            return await manager.Playlists.Create(new PlaylistCreateRequest(playlistName) { Public = false, Collaborative = false, Description = description });
        }
    }
}
