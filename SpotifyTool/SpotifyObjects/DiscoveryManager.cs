using SpotifyAPI.Web;
using SpotifyTool.SpotifyAPI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyObjects
{
    public static class DiscoveryManager
    {
        public static async Task EnqueueFromArtistAlbums(string artistId, bool onlyLatest)
        {
            //Execute login request first so that other tasks can be executed async which may spawn multiple login requests
            await SpotifyAPIManager.Instance.GetUser();
            //Maybe split libraryTask and artistTracksTask and following functions before foreach loop to await them each for maximum performance
            //Maybe load artist albums page by page while checking the already loaded pages, but this method is questionable because of album order and batch song loading
            Task<List<SavedTrack>> libraryTask = LibraryManager.GetLibraryTracksForCurrentUser();
            Dictionary<FullAlbum, List<SimpleTrack>> artistTracks = await SpotifyAPIManager.Instance.GetAllArtistTracks(artistId, true);
            KeyValuePair<FullAlbum, List<SimpleTrack>>[] orderedArtistTracks = artistTracks.OrderByDescending(kv => kv.Key.ReleaseDate).ToArray();
            int artistTracksCount = artistTracks.Sum(at => at.Value.Count);
            TrackSubset[] seenTracks = new TrackSubset[artistTracksCount];
            List<TrackSubset> seenTracksList = new List<TrackSubset>(artistTracksCount);
            Dictionary<string, TrackSubset> seenTracksDictionary = new Dictionary<string, TrackSubset>(artistTracksCount);
            ICollection<Task> enqueueTasks = new LinkedList<Task>();
            List<SavedTrack> libraryResult = await libraryTask;
            TrackSubset[] libraryTracks = libraryResult.Select(pt => new TrackSubset(pt.Track)).OrderBy(t => t.LowerName).ToArray();
            Dictionary<string, TrackSubset> libraryDictionary = libraryTracks.Distinct(TrackSubsetEqualityComparer.Instance).ToDictionary(t => t.Id, t => t);
            foreach (KeyValuePair<FullAlbum, List<SimpleTrack>> album in orderedArtistTracks)
            {
                foreach (SimpleTrack track in album.Value)
                {
                    bool alreadySeen = false;
                    TrackSubset trackSubset = new TrackSubset(track);
                    if (seenTracksDictionary.ContainsKey(trackSubset.Id))
                    {
                        alreadySeen = true;
                    }
                    if (!alreadySeen)
                    {
                        seenTracksDictionary.Add(trackSubset.Id, trackSubset);
                    }

                    int findResult = -1;
                    if (!alreadySeen)
                    {
                        findResult = trackSubset.SamePropsBinaryFind(seenTracks);
                        if (findResult >= 0)
                        {
                            alreadySeen = true;
                        }
                    }
                    seenTracksList.Add(trackSubset);
                    seenTracks = seenTracksList.OrderBy(t => t == null ? 1 : 0).ThenBy(t => t == null ? "" : t.LowerName).ToArray();

                    //check if in library for onlyLatest early return
                    if ((!alreadySeen || onlyLatest) && libraryDictionary.ContainsKey(trackSubset.Id))
                    {
                        if (onlyLatest)
                        {
                            return;
                        }
                        continue;
                    }
                    if (alreadySeen)
                    {
                        continue;
                    }
                    findResult = trackSubset.SamePropsBinaryFind(libraryTracks);
                    if (findResult >= 0)
                    {
                        continue;
                    }
                    enqueueTasks.Add(SpotifyAPIManager.Instance.QueueTrack(trackSubset.Uri));
                }
            }
            await Task.WhenAll(enqueueTasks.ToArray());
        }
    }
}
