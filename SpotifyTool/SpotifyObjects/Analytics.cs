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
            FullTrack[] nonPlayableTracks = allPlaylistTracks.Where(t => !t.IsPlayable || t.IsLocal).ToArray();
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

        public static async Task<KeyValuePair<List<SavedTrack>, List<FullPlaylistTrack>>> CrossCheckLikedAndPlaylist(SimplePlaylist playlist, string playlistId)
        {
            Task<List<FullPlaylistTrack>> playlistTrackTask;
            if (playlist != null)
            {
                playlistTrackTask = PlaylistManager.GetAllPlaylistTracks(playlist);
            }
            else
            {
                playlistTrackTask = PlaylistManager.GetAllPlaylistTracks(playlistId);
            }

            Task<List<SavedTrack>> libraryTracksTask = LibraryManager.GetLibraryTracksForCurrentUser();
            await Task.WhenAll(playlistTrackTask, libraryTracksTask);
            List<FullPlaylistTrack> playlistTracks = playlistTrackTask.Result;
            List<SavedTrack> libraryTracks = libraryTracksTask.Result;
            Dictionary<string, FullPlaylistTrack> playlistDict = playlistTracks.Distinct(FullPlaylistTrackEqualityComparerByTrackInfo.Instance).ToDictionary(fpt => fpt.TrackInfo.Uri, fpt => fpt);
            List<FullTrack> fullTracksFromPL = PlaylistManager.GetAllPlaylistTrackInfo(playlistTracks);

            Dictionary<string, SavedTrack> libraryDict = libraryTracks.ToDictionary(st => st.Track.Uri, st => st);
            List<FullTrack> fullTracksFromLibrary = LibraryManager.GetFullTracks(libraryTracks);
            List<FullTrack> missingFromPL = fullTracksFromLibrary.Except(fullTracksFromPL, FullTrackEqualityComparer.Instance).ToList();
            if (missingFromPL.Count < 1 && fullTracksFromLibrary.Count == fullTracksFromPL.Count)
            {
                return new KeyValuePair<List<SavedTrack>, List<FullPlaylistTrack>>(new List<SavedTrack>(), new List<FullPlaylistTrack>());
            }
            List<FullTrack> missingFromLibrary = fullTracksFromPL.Except(fullTracksFromLibrary, FullTrackEqualityComparer.Instance).ToList();
            return new KeyValuePair<List<SavedTrack>, List<FullPlaylistTrack>>(
                missingFromLibrary.Select(ft => libraryDict[ft.Uri]).ToList(),
                missingFromPL.Select(ft => playlistDict[ft.Uri]).ToList());
        }
    }
}
