using SpotifyAPI.Web;
using SpotifyTool.SpotifyAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyObjects
{
    public static class DiscoveryManager
    {
        public static async Task EnqueueArtistTopTracks(string artistId, bool includeAllVariations)
        {
            //Execute login request first so that other tasks can be executed async which may spawn multiple login requests
            await SpotifyAPIManager.Instance.GetUser();
            Task<List<SavedTrack>> libraryTask = LibraryManager.GetLibraryTracksForCurrentUser();
            var topTracks = await SpotifyAPIManager.Instance.GetAllArtistTopTracks(artistId);
            var topTracksAlbum = new IEnumerable<TrackSubset>[] { topTracks.Select(tt => new TrackSubset(tt)) };
            await Enqueue(false, includeAllVariations, libraryTask, topTracksAlbum);
        }

        public static async Task EnqueueFromArtistAlbums(string artistId, bool onlyLatest, bool includeAllVariations)
        {
            //Execute login request first so that other tasks can be executed async which may spawn multiple login requests
            await SpotifyAPIManager.Instance.GetUser();
            //Maybe split libraryTask and artistTracksTask and following functions before foreach loop to await them each for maximum performance
            //Maybe load artist albums page by page while checking the already loaded pages, but this method is questionable because of album order and batch song loading
            Task<List<SavedTrack>> libraryTask = LibraryManager.GetLibraryTracksForCurrentUser();
            Dictionary<FullAlbum, List<SimpleTrack>> artistAlbums = await SpotifyAPIManager.Instance.GetAllArtistTracks(artistId, true);
            var orderedArtistAlbumTracks = artistAlbums.OrderByDescending(kv => kv.Key.ReleaseDate).Select(kv => kv.Value.Select(t =>  new TrackSubset(t))).ToArray();
            await Enqueue(onlyLatest, includeAllVariations, libraryTask, orderedArtistAlbumTracks);
        }

        private static async Task Enqueue(bool stopAtFirstLiked, bool includeAllVariations, Task<List<SavedTrack>> libraryTask, IEnumerable<IEnumerable<TrackSubset>> orderedArtistAlbumTracks)
        {
            int artistTracksCount = orderedArtistAlbumTracks.Sum(at => at.Count());
            TrackSubset[] seenTracks = new TrackSubset[artistTracksCount];
            List<TrackSubset> seenTracksList = new List<TrackSubset>(artistTracksCount);
            Dictionary<string, TrackSubset> seenTracksDictionary = new Dictionary<string, TrackSubset>(artistTracksCount);
            List<SavedTrack> libraryResult = await libraryTask;
            TrackSubset[] libraryTracks = libraryResult.Select(pt => new TrackSubset(pt.Track)).OrderBy(t => t.LowerName).ToArray();
            Dictionary<string, TrackSubset> libraryDictionary = libraryTracks.Distinct(TrackSubsetEqualityComparer.Instance).ToDictionary(t => t.Id, t => t);
            List<TrackSubset> toEnqueue = new List<TrackSubset>();
            foreach (var album in orderedArtistAlbumTracks)
            {
                ICollection<TrackSubset> toAdd = new LinkedList<TrackSubset>();
                bool breakAlbum = false;
                foreach (var trackSubset in album)
                {
                    bool alreadySeen = false;
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
                    if ((!alreadySeen || stopAtFirstLiked) && libraryDictionary.ContainsKey(trackSubset.Id))
                    {
                        if (stopAtFirstLiked)
                        {
                            breakAlbum = true;
                            break;
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
                    //Wait with adding to toEnqueue in case a song from the album is liked and onlyLatest = true
                    toAdd.Add(trackSubset);
                }
                if (breakAlbum)
                {
                    break;
                }
                toEnqueue.AddRange(toAdd);
            }
            if (!includeAllVariations)
            {
                toEnqueue = toEnqueue.Where(ts => !toEnqueue.Any(tso => ts.Id != tso.Id && ts.LowerName.StartsWith(tso.LowerName))).ToList();
            }

            var enqueueResults = new Dictionary<TrackSubset, bool>();
            //Failure to enque ("not found"-Exception) probaly results from enqueuing too much at once, so wait until enqueue finished before enqueuing the next track
            foreach (var toEnqueSingle in toEnqueue)
            {
                var success = await TryQueue(toEnqueSingle);
                enqueueResults.Add(toEnqueSingle, success);
            }
            Console.WriteLine();
            //Print results after possible exception stacktrace from enqueue attempt
            foreach (var enqueueTask in enqueueResults)
            {
                Console.WriteLine(enqueueTask.Key.ToString() + ": " + enqueueTask.Value);
            }
        }

        private static async Task<bool> TryQueue(TrackSubset ts)
        {
            //Enqueueing sometimes throws an exception because "not found", so just catch and print here for now
            //This catching is why this method is async and does not simply return the task
            try
            {
                return await SpotifyAPIManager.Instance.QueueTrack(ts.Uri);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n" + ts.LowerName + ":\n" + ex.ToString());
                return false;
            }
        }
    }
}
