using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyObjects
{
    public static class Analytics
    {
        public static async Task<FullTrack[]> GetNonPlayableTracks(SimplePlaylist pl, string playlistID)
        {
            List<FullTrack> allPlaylistTracks = await GetAllPlaylistTracks(pl, playlistID);
            FullTrack[] nonPlayableTracks = allPlaylistTracks.Where(t => t.Restrictions?.Any() ?? false || t.IsLocal).ToArray();
            return nonPlayableTracks;
        }

        private static async Task<List<FullTrack>> GetAllPlaylistTracks(SimplePlaylist pl, string playlistID)
        {
            List<FullTrack> allPlaylistTracks;
            if (pl != null)
            {
                allPlaylistTracks = await PlaylistManager.GetAllPlaylistTracks(pl);
            }
            else
            {
                allPlaylistTracks = await PlaylistManager.GetAllPlaylistTracks(playlistID);
            }

            return allPlaylistTracks;
        }

        public static async Task<FullTrack[]> GetDoubleTracks(SimplePlaylist pl, string playlistID)
        {
            List<FullTrack> allPlaylistTracks = await GetAllPlaylistTracks(pl, playlistID);
            FullTrack[] allSameTracks = allPlaylistTracks
                .Where(t1 => allPlaylistTracks
                    .Any(t2 => t2.Id != t1.Id && t1.Uri != t2.Uri && t1.Name.ToLower().StartsWith(t2.Name.ToLower()) && t1.Artists
                        .Select(a => a.Id).Any(aID => t2.Artists.Select(a => a.Id).Contains(aID))))
                .Distinct().OrderBy(t => t.Name).ToArray();
            return allSameTracks;
        }
    }
}
