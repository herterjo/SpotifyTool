using SpotifyAPI.Web;
using SpotifyTool.Config;
using SpotifyTool.SpotifyAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyObjects
{
    public static class PlaylistManager
    {
        public const string PlaylistFileEnding = ".playlist.json";

        private static string GetPlaylistFileName(FullPlaylist pl)
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
            var currentDirectory = Directory.GetCurrentDirectory();
            var playlistIds = Directory.EnumerateFiles(currentDirectory)
                .Select(p => Path.GetFileName(p))
                .Where(f => f.EndsWith(PlaylistFileEnding))
                .Select(f => f[..^PlaylistFileEnding.Length])
                .ToList();
            //Do this one after another to not generate too many requests
            foreach (var playlistId in playlistIds)
            {
                FullPlaylist playlist;
                try
                {
                    playlist = await spotifyAPIManager.GetPlaylist(playlistId);
                }
                catch (Exception)
                {
                    continue;
                }
                if (playlist != null)
                {
                    await RefreshSinglePlaylist(playlist);
                }
            }
        }

        public static Task<List<FullPlaylistTrack>> RefreshSinglePlaylist(string playlistID)
        {
            return RefreshSinglePlaylist(null, playlistID);
        }

        public static Task<List<FullPlaylistTrack>> RefreshSinglePlaylist(FullPlaylist fullPlaylist)
        {
            return RefreshSinglePlaylist(fullPlaylist, null);
        }

        private static async Task<List<FullPlaylistTrack>> RefreshSinglePlaylist(FullPlaylist fullPlaylist, string playlistID)
        {
            string path;
            IList<PlaylistTrack<IPlayableItem>> allItems;
            if (fullPlaylist != null)
            {
                playlistID = fullPlaylist.Id;
            }
            if (HasFirstTrackPageLoaded(fullPlaylist))
            {
                allItems = await SpotifyAPIManager.Instance.PaginateAll(fullPlaylist.Items);
                path = GetPlaylistFileName(fullPlaylist);
            }
            else
            {
                allItems = await SpotifyAPIManager.Instance.GetAllItemsFromPlaylist(playlistID);
                path = GetPlaylistFileName(playlistID);
            }

            List<FullPlaylistTrack> allPlaylistTracks = GetPlaylistTracks(allItems);
            await Serialization.SerializeJson(allPlaylistTracks, path, false);
            return allPlaylistTracks;
        }

        private static bool HasFirstTrackPageLoaded(FullPlaylist fullPlaylist)
        {
            return fullPlaylist?.Items?.Items != null;
        }

        public static List<FullPlaylistTrack> GetPlaylistTracks(IEnumerable<PlaylistTrack<IPlayableItem>> allItems)
        {
            return allItems.Where(i => i.Track.GetType() == typeof(FullTrack)).Select(i => new FullPlaylistTrack(i, (FullTrack)i.Track)).ToList();
        }

        public static Task<List<FullPlaylistTrack>> GetAllPlaylistTracks(FullPlaylist pl)
        {
            return GetAllPlaylistTracks(pl, null);
        }

        public static Task<List<FullPlaylistTrack>> GetAllPlaylistTracks(string plID)
        {
            return GetAllPlaylistTracks(null, plID);
        }

        public static async Task<List<FullPlaylistTrack>> GetAllPlaylistsTracks(IEnumerable<FullPlaylist> pls)
        {
            var fullList = new List<FullPlaylistTrack>();
            foreach (var pl in pls)
            {
                var playlistTracks = await GetAllPlaylistTracks(pl, null);
                fullList.AddRange(playlistTracks);
            }
            return fullList;
        }

        public static async Task<List<FullPlaylistTrack>> GetAllPlaylistsTracks(IEnumerable<string> plIDs)
        {
            var fullList = new List<FullPlaylistTrack>();
            foreach (var plId in plIDs)
            {
                var playlistTracks = await GetAllPlaylistTracks(null, plId);
                fullList.AddRange(playlistTracks);
            }
            return fullList;
        }

        public static List<FullTrack> GetAllPlaylistTrackInfo(List<FullPlaylistTrack> playlist)
        {
            return playlist.Select(fpt => fpt.TrackInfo).ToList();
        }

        private static Task<List<FullPlaylistTrack>> GetAllPlaylistTracks(FullPlaylist pl, string playlistID)
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
