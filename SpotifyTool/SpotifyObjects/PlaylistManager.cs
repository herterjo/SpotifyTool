using SpotifyAPI.Web;
using SpotifyTool.Config;
using SpotifyTool.SpotifyAPI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyObjects
{
    public static class PlaylistManager
    {
        public const string PlaylistFileEnding = ".playlist.json";

        private static string GetPlaylistFileName(SimplePlaylist pl)
        {
            return GetPlaylistFileName(pl.Id);
        }

        private static string GetPlaylistFileName(string plID)
        {
            return plID + PlaylistFileEnding;
        }

        public static async Task RefreshAllUserPlaylists()
        {
            SpotifyAPIManager spotifyAPIManager = SpotifyAPIManager.Instance;
            List<SimplePlaylist> playlists = await spotifyAPIManager.GetPlaylistsFromCurrentUser();
            List<SimplePlaylist> toRefresh = playlists.Where(pl => File.Exists(GetPlaylistFileName(pl))).ToList();
            Task[] allRefreshTasks = toRefresh.Select(RefreshSinglePlaylist).ToArray();
            await Task.WhenAll(allRefreshTasks);
        }

        public static Task<List<FullPlaylistTrack>> RefreshSinglePlaylist(string playlistID)
        {
            return RefreshSinglePlaylist(null, playlistID);
        }

        public static Task<List<FullPlaylistTrack>> RefreshSinglePlaylist(SimplePlaylist simplePlaylist)
        {
            return RefreshSinglePlaylist(simplePlaylist, null);
        }

        private static async Task<List<FullPlaylistTrack>> RefreshSinglePlaylist(SimplePlaylist simplePlaylist, string playlistID)
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

            List<FullPlaylistTrack> allPlaylistTracks = GetPlaylistTracks(allItems);
            //string playlistJSON = JsonConvert.SerializeObject(allPlaylistTracks);
            //await File.WriteAllTextAsync(path, playlistJSON);
            await Serialization.SerializeJson(allPlaylistTracks, path, false);
            return allPlaylistTracks;
        }

        private static bool HasFirstTrackPageLoaded(SimplePlaylist simplePlaylist)
        {
            return simplePlaylist?.Tracks?.Items != null;
        }

        public static List<FullPlaylistTrack> GetPlaylistTracks(IList<PlaylistTrack<IPlayableItem>> allItems)
        {
            return allItems.Where(i => i.Track.GetType() == typeof(FullTrack)).Select(i => new FullPlaylistTrack(i, (FullTrack)i.Track)).ToList();
        }

        public static Task<List<FullPlaylistTrack>> GetAllPlaylistTracks(SimplePlaylist pl)
        {
            return GetAllPlaylistTracks(pl, null);
        }

        public static Task<List<FullPlaylistTrack>> GetAllPlaylistTracks(string plID)
        {
            return GetAllPlaylistTracks(null, plID);
        }

        public static List<FullTrack> GetAllPlaylistTrackInfo(List<FullPlaylistTrack> playlist)
        {
            return playlist.Select(fpt => fpt.TrackInfo).ToList();
        }

        private static Task<List<FullPlaylistTrack>> GetAllPlaylistTracks(SimplePlaylist pl, string playlistID)
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
            if (File.Exists(fn))
            {
                return Serialization.DeserializeJson<List<FullPlaylistTrack>>(fn, false);
                //string content = await File.ReadAllTextAsync(fn);
                //return JsonConvert.DeserializeObject<List<FullPlaylistTrack>>(content);
            }
            if (pl != null)
            {
                return RefreshSinglePlaylist(pl);
            }
            else
            {
                return RefreshSinglePlaylist(playlistID);
            }

        }
    }
}
