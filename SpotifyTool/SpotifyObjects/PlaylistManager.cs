using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyTool.SpotifyAPI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyObjects
{
    public static class PlaylistManager
    {
        public const string JSONFileEnding = ".json";

        private static string GetPlaylistFileName(SimplePlaylist pl)
        {
            return GetPlaylistFileName(pl.Id);
        }

        private static string GetPlaylistFileName(string plID)
        {
            return plID + JSONFileEnding;
        }

        public static async Task RefreshAllUserPlaylists()
        {
            SpotifyAPIManager spotifyAPIManager = SpotifyAPIManager.Instance;
            List<SimplePlaylist> playlists = await spotifyAPIManager.GetPlaylistsFromCurrentUser();
            List<SimplePlaylist> toRefresh = playlists.Where(pl => File.Exists(GetPlaylistFileName(pl))).ToList();
            Task<List<FullTrack>>[] allRefreshTasks = toRefresh.Select(RefreshSinglePlaylist).ToArray();
            await Task.WhenAll(allRefreshTasks);
        }

        public static Task<List<FullTrack>> RefreshSinglePlaylist(string playlistID)
        {
            return RefreshSinglePlaylist(null, playlistID);
        }

        public static Task<List<FullTrack>> RefreshSinglePlaylist(SimplePlaylist simplePlaylist)
        {
            return RefreshSinglePlaylist(simplePlaylist, null);
        }

        private static async Task<List<FullTrack>> RefreshSinglePlaylist(SimplePlaylist simplePlaylist, string playlistID)
        {
            string path;
            IList<PlaylistTrack<IPlayableItem>> allItems;
            if (simplePlaylist != null)
            {
                playlistID = simplePlaylist.Id;
            }
            if (HasFirstTrackPageLoaded(simplePlaylist))
            {
                allItems = await SpotifyAPIManager.Instance.PaginateAll(simplePlaylist.Tracks);
                path = GetPlaylistFileName(simplePlaylist);
            }
            else
            {
                allItems = await SpotifyAPIManager.Instance.GetAllItemsFromPlaylist(playlistID);
                path = GetPlaylistFileName(playlistID);
            }

            List<FullTrack> allPlaylistTracks = GetFullTracks(allItems);
            string playlistJSON = JsonConvert.SerializeObject(allPlaylistTracks);
            await File.WriteAllTextAsync(path, playlistJSON);
            return allPlaylistTracks;
        }

        private static bool HasFirstTrackPageLoaded(SimplePlaylist simplePlaylist)
        {
            return simplePlaylist?.Tracks?.Items != null;
        }

        public static List<FullTrack> GetFullTracks(IList<PlaylistTrack<IPlayableItem>> allItems)
        {
            return allItems.Select(i => i.Track).Where(t => t.GetType() == typeof(FullTrack)).Cast<FullTrack>().ToList();
        }

        public static Dictionary<PlaylistTrack<IPlayableItem>, FullTrack> GetFullTracksDict(IList<PlaylistTrack<IPlayableItem>> allItems)
        {
            return allItems.Where(i => i.Track.GetType() == typeof(FullTrack)).ToDictionary(i => i, i => (FullTrack)i.Track);
        }

        public static Task<List<FullTrack>> GetAllPlaylistTracks(SimplePlaylist pl)
        {
            return GetAllPlaylistTracks(pl, null);
        }

        public static Task<List<FullTrack>> GetAllPlaylistTracks(string plID)
        {
            return GetAllPlaylistTracks(null, plID);
        }

        private static async Task<List<FullTrack>> GetAllPlaylistTracks(SimplePlaylist pl, string playlistID)
        {
            string fn;
            if (pl != null)
            {
                fn = GetPlaylistFileName(pl);
            }
            else
            {
                fn = GetPlaylistFileName(playlistID);
            }
            if (!File.Exists(fn))
            {
                string content = await File.ReadAllTextAsync(fn);
                return JsonConvert.DeserializeObject<List<FullTrack>>(content);
            }
            if (pl != null)
            {
                return await RefreshSinglePlaylist(pl);
            }
            else
            {
                return await RefreshSinglePlaylist(playlistID);
            }

        }
    }
}
