﻿using SpotifyAPI.Web;
using SpotifyTool.SpotifyAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyObjects
{
    public static class Analytics
    {
        public static async Task<FullTrack[]> GetNonPlayableTracks(IEnumerable<FullPlaylist> pls, IEnumerable<string> playlistIDs)
        {
            PrivateUser user = await SpotifyAPIManager.Instance.GetUser();
            string country = user.Country;
            List<FullTrack> allPlaylistTracks = await GetAllPlaylistsTracks(pls, playlistIDs);
            FullTrack[] nonPlayableTracks = allPlaylistTracks.Where(t => !t.IsPlayable || t.IsLocal).ToArray();
            return nonPlayableTracks;
        }

        private static async Task<List<FullTrack>> GetAllPlaylistsTracks(IEnumerable<FullPlaylist> pls, IEnumerable<string> playlistIDs)
        {
            List<FullPlaylistTrack> allFullPlaylistTracks;
            if (pls != null)
            {
                allFullPlaylistTracks = await PlaylistManager.GetAllPlaylistsTracks(pls);
            }
            else
            {
                allFullPlaylistTracks = await PlaylistManager.GetAllPlaylistsTracks(playlistIDs);
            }

            return PlaylistManager.GetAllPlaylistTrackInfo(allFullPlaylistTracks);
        }

        public static async Task<FullTrack[]> GetDoubleTracks(IEnumerable<FullPlaylist> pls, IEnumerable<string> playlistIDs)
        {
            List<FullTrack> allPlaylistTracks = await GetAllPlaylistsTracks(pls, playlistIDs);
            return CheckDouble(allPlaylistTracks);
        }

        private static FullTrack[] CheckDouble(List<FullTrack> allPlaylistTracks)
        {
            IEnumerable<FullTrack> doubleTracks = AnalyzeTrackSubsets(allPlaylistTracks, true, ts => ts.Where(t => t.SamePropsBinaryFind(ts) >= 0));
            return doubleTracks.OrderBy(t => t.Name).ToArray();
        }

        private static IEnumerable<FullTrack> AnalyzeTrackSubsets(IEnumerable<FullTrack> fullTracks, bool autoReturnSameId, Func<TrackSubset[], IEnumerable<TrackSubset>> analyzeFunc)
        {
            Dictionary<string, FullTrack> trackDictionary = new Dictionary<string, FullTrack>(fullTracks.Count());
            ICollection<FullTrack> sameIdTracks = new LinkedList<FullTrack>();
            foreach (FullTrack track in fullTracks)
            {
                if (trackDictionary.ContainsKey(track.Id))
                {
                    if (autoReturnSameId)
                    {
                        sameIdTracks.Add(track);
                        sameIdTracks.Add(track);
                    }
                }
                else
                {
                    trackDictionary.Add(track.Id, track);
                }
            }
            TrackSubset[] trackSubsets = fullTracks.Select(t => new TrackSubset(t)).OrderBy(t => t.LowerName).ToArray();
            IEnumerable<TrackSubset> doubleTracks = analyzeFunc(trackSubsets);
            return doubleTracks.Select(dt => trackDictionary[dt.Id]).Concat(sameIdTracks);
        }

        public static async Task<FullTrack[]> GetDoubleLibraryTracks()
        {
            List<SavedTrack> tracks = await LibraryManager.GetLibraryTracksForCurrentUser();
            List<FullTrack> fullTracks = LibraryManager.GetFullTracks(tracks);
            return CheckDouble(fullTracks);
        }

        public static List<FullTrack> GetDoubleArtistsTracks(List<FullTrack> fullTracks)
        {
            IEnumerable<FullTrack> doubleTracks = AnalyzeTrackSubsets(fullTracks, true, trackSubsets =>
            {
                ICollection<TrackSubset> doubleArtistTracks = new LinkedList<TrackSubset>();
                for (int i = 0; i < trackSubsets.Length; i++)
                {
                    TrackSubset trackToCheck = trackSubsets[i];
                    HashSet<string> artistIDsOfTrackToCheck = trackToCheck.ArtistIds;
                    for (int j = i + 1; j < trackSubsets.Length; j++)
                    {
                        TrackSubset trackToCompare = trackSubsets[j];
                        HashSet<string> artistsOfCompareTrack = trackToCompare.ArtistIds;
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
                        List<TrackSubset> differentArtistsTracks = trackSubsets.Where(t => t != trackToCheck && t != trackToCompare && t.ArtistIds.Any(a => differentArtists.Contains(a))).ToList();
                        if (differentArtistsTracks.Any())
                        {
                            doubleArtistTracks.Add(trackToCompare);
                            doubleArtistTracks.Add(trackToCheck);
                            foreach (TrackSubset doubleTrack in differentArtistsTracks)
                            {
                                doubleArtistTracks.Add(doubleTrack);
                            }
                            continue;
                        }
                    }
                }
                return doubleArtistTracks;
            });

            return doubleTracks.ToList();
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

        public static async Task<(List<SavedTrack> MissingFromPlaylist, List<FullPlaylistTrack> MissingFromLibrary)> CrossCheckLikedAndPlaylist(IEnumerable<FullPlaylist> playlists, IEnumerable<string> playlistIds)
        {
            Task<List<FullPlaylistTrack>> playlistTrackTask;
            if (playlists != null)
            {
                playlistTrackTask = PlaylistManager.GetAllPlaylistsTracks(playlists);
            }
            else
            {
                playlistTrackTask = PlaylistManager.GetAllPlaylistsTracks(playlistIds);
            }

            Task<List<SavedTrack>> libraryTracksTask = LibraryManager.GetLibraryTracksForCurrentUser();
            await Task.WhenAll(playlistTrackTask, libraryTracksTask);
            List<FullPlaylistTrack> playlistTracks = playlistTrackTask.Result;
            List<SavedTrack> libraryTracks = libraryTracksTask.Result;
            Dictionary<string, FullPlaylistTrack> playlistDict = playlistTracks.Distinct(FullPlaylistTrackEqualityComparerByTrackInfo.Instance).ToDictionary(fpt => fpt.TrackInfo.Uri, fpt => fpt);
            List<FullTrack> fullTracksFromPL = PlaylistManager.GetAllPlaylistTrackInfo(playlistTracks);

            Dictionary<string, SavedTrack> libraryDict = libraryTracks.Distinct(SavedTrackComparerByUri.Instance).ToDictionary(st => st.Track.Uri, st => st);
            List<FullTrack> fullTracksFromLibrary = LibraryManager.GetFullTracks(libraryTracks);
            List<FullTrack> missingFromPL = fullTracksFromLibrary.Except(fullTracksFromPL, FullTrackEqualityComparer.Instance).ToList();
            if (missingFromPL.Count < 1 && fullTracksFromLibrary.Count == fullTracksFromPL.Count)
            {
                return (new List<SavedTrack>(), new List<FullPlaylistTrack>());
            }
            List<FullTrack> missingFromLibrary = fullTracksFromPL.Except(fullTracksFromLibrary, FullTrackEqualityComparer.Instance).ToList();
            return (missingFromPL.Select(ft => libraryDict[ft.Uri]).ToList(), missingFromLibrary.Select(ft => playlistDict[ft.Uri]).ToList());
        }

        public static async Task<List<FullPlaylistTrack>> GetTracksInSecondaryButNotInPrimary(IEnumerable<string> primaryPlaylistIds, string secondaryPlaylistId)
        {
            var primaryTracks = await PlaylistManager.GetAllPlaylistsTracks(primaryPlaylistIds);
            var secondaryTracksTask = PlaylistManager.GetAllPlaylistTracks(secondaryPlaylistId);
            var primaryIds = primaryTracks.Select(fpt => fpt.TrackInfo.Uri).Distinct().ToHashSet();
            var secondaryTracks = await secondaryTracksTask;
            return secondaryTracks.Where(fpt => !primaryIds.Contains(fpt.TrackInfo.Uri)).ToList();
        }
    }
}
