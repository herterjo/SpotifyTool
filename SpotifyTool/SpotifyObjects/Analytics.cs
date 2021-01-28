using SpotifyAPI.Web;
using SpotifyTool.SpotifyAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyObjects
{
    public static class Analytics
    {
        public static async Task<FullTrack[]> GetNonPlayableTracks(SimplePlaylist pl, string playlistID)
        {
            PrivateUser user = await SpotifyAPIManager.Instance.GetUser();
            string country = user.Country;
            List<FullTrack> allPlaylistTracks = await GetAllPlaylistTracks(pl, playlistID);
            FullTrack[] nonPlayableTracks = allPlaylistTracks.Where(t => t.Restrictions?.Any() ?? false || t.IsLocal || !t.AvailableMarkets.Contains(country)).ToArray();
            return nonPlayableTracks;
        }

        private static async Task<List<FullTrack>> GetAllPlaylistTracks(SimplePlaylist pl, string playlistID)
        {
            List<FullPlaylistTrack> allFullPlaylistTracks;
            if (pl != null)
            {
                allFullPlaylistTracks = await PlaylistManager.GetAllPlaylistTracks(pl);
            }
            else
            {
                allFullPlaylistTracks = await PlaylistManager.GetAllPlaylistTracks(playlistID);
            }

            return PlaylistManager.GetAllPlaylistTrackInfo(allFullPlaylistTracks);
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

        public static List<FullTrack> GetDoubleArtistsTracks(List<FullTrack> tracksToAnalyze)
        {
            ICollection<FullTrack> doubleArtistTracks = new LinkedList<FullTrack>();
            for (int i = 0; i < tracksToAnalyze.Count; i++)
            {
                FullTrack trackToCheck = tracksToAnalyze[i];
                List<string> artistIDsOfTrackToCheck = trackToCheck.Artists.Select(a => a.Id).ToList();
                for (int j = i + 1; j < tracksToAnalyze.Count; j++)
                {
                    FullTrack trackToCompare = tracksToAnalyze[j];
                    List<string> artistsOfCompareTrack = trackToCompare.Artists.Select(a => a.Id).ToList();
                    List<string> sameArtists = artistIDsOfTrackToCheck.Where(aid => artistsOfCompareTrack.Contains(aid)).ToList();
                    if (!sameArtists.Any())
                    {
                        continue;
                    }
                    if (sameArtists.Count == artistIDsOfTrackToCheck.Count + artistsOfCompareTrack.Count)
                    {
                        doubleArtistTracks.Add(trackToCompare);
                        doubleArtistTracks.Add(trackToCheck);
                        continue;
                    }
                    //Maybe use recursion to get more depth
                    IEnumerable<string> differentArtists = artistsOfCompareTrack.Concat(artistIDsOfTrackToCheck).Where(aid => !sameArtists.Contains(aid));
                    List<FullTrack> differentArtistsTracks = tracksToAnalyze.Where(t => t != trackToCheck && t != trackToCompare && t.Artists.Any(a => differentArtists.Contains(a.Id))).ToList();
                    if (differentArtistsTracks.Any())
                    {
                        doubleArtistTracks.Add(trackToCompare);
                        doubleArtistTracks.Add(trackToCheck);
                        foreach (FullTrack doubleTrack in differentArtistsTracks)
                        {
                            doubleArtistTracks.Add(doubleTrack);
                        }
                        continue;
                    }
                }
            }
            return doubleArtistTracks.Distinct().ToList();
        }

        public static ICollection<FullTrack> GetTracksToAddToSecondary(List<FullPlaylistTrack> mainTracks, List<FullTrack> secondaryTracks)
        {
            ICollection<FullTrack> toAddLinked = new LinkedList<FullTrack>();
            IEnumerable<string> presentArtists = secondaryTracks.SelectMany(t => t.Artists.Select(a => a.Id));
            List<FullTrack> orderedFirst = mainTracks.Where(kv => !kv.TrackInfo.Artists.Any(a => presentArtists.Contains(a.Id))).OrderBy(kv => kv.PlaylistInfo.AddedAt).Select(kv => kv.TrackInfo).ToList();
            for (int i = 0; i < orderedFirst.Count; i++)
            {
                FullTrack currentTrack = orderedFirst[i];
                IEnumerable<string> currentTrackAristIDs = currentTrack.Artists.Select(a => a.Id);
                if (currentTrackAristIDs.Any(aid => presentArtists.Contains(aid)))
                {
                    continue;
                }
                toAddLinked.Add(currentTrack);
                presentArtists = presentArtists.Concat(currentTrackAristIDs);
            }
            return toAddLinked;
        }
    }
}
