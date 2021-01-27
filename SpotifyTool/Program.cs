using SpotifyAPI.Web;
using SpotifyTool.Config;
using SpotifyTool.SpotifyAPI;
using SpotifyTool.SpotifyObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool
{
    public class Program
    {
        private const bool refresh = false;

        public static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
            MainMenu().Wait();
#if !DEBUG
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
                Console.ReadLine();
            }
#endif
        }

        public static async Task MainMenu()
        {
            while (true)
            {
                Console.WriteLine("\n");
                Console.WriteLine("Please choose a menu point: ");
                Console.WriteLine("0) Exit");
                Console.WriteLine("1) All analytics");
                Console.WriteLine("2) Get non playble tracks");
                Console.WriteLine("3) Get double tracks in playlist");
                Console.WriteLine("4) Sync main playlist with ArtistOnlyOnce playlist");
                Console.WriteLine("5) Refresh all user playlists");
                string option = Console.ReadLine();
                uint optionInt;
                if (!UInt32.TryParse(option, out optionInt))
                {
                    Console.WriteLine("Please write a valid number");
                    continue;
                }
                bool exit = false;
                switch (optionInt)
                {
                    case 0:
                        exit = true;
                        break;
                    case 1:
                        KeyValuePair<string, string> idsForAll = await GetMainAndSecondPlaylistIDs();
                        await AllAnalytics(idsForAll.Key, idsForAll.Value);
                        break;
                    case 2:
                        SimplePlaylist npPL = await ChoosePlaylistFromUserPlaylists();
                        await PrintNonPlayableTracks(npPL, null);
                        break;
                    case 3:
                        SimplePlaylist dPL = await ChoosePlaylistFromUserPlaylists();
                        await CheckDoubleTracks(dPL, null);
                        break;
                    case 4:
                        KeyValuePair<string, string> ids = await GetMainAndSecondPlaylistIDs();
                        await Sync(ids.Key, ids.Value);
                        break;
                    case 5:
                        await PlaylistManager.RefreshAllUserPlaylists();
                        break;
                    default:
                        Console.WriteLine("Number not recognized for menu");
                        break;
                }
                Console.WriteLine("\n");
                if (exit)
                {
                    break;
                }
            }
        }

        private static async Task AllAnalytics(string mainID, string secondID)
        {
            await PlaylistManager.RefreshSinglePlaylist(mainID);
            await PlaylistManager.RefreshSinglePlaylist(secondID);
            await PrintNonPlayableTracks(null, mainID);
            await PrintNonPlayableTracks(null, secondID);
            await CheckDoubleTracks(null, mainID);
            await CheckDoubleTracks(null, secondID);
            await Sync(mainID, secondID);
        }

        private static async Task<KeyValuePair<string, string>> GetMainAndSecondPlaylistIDs()
        {
            KeyValuePair<string, string> configIDs = await ConfigManager.GetMainAndOneArtistPlaylistID();
            string mainID = configIDs.Key;
            string secondID = configIDs.Value;
            if (String.IsNullOrWhiteSpace(mainID))
            {
                Console.WriteLine("Selecting main playlist:");
                SimplePlaylist mainPL = await ChoosePlaylistFromUserPlaylists();
                mainID = mainPL.Id;
            }
            if (String.IsNullOrWhiteSpace(secondID))
            {
                Console.WriteLine("Selecting playlist where every artist is present only once:");
                SimplePlaylist secondPL = await ChoosePlaylistFromUserPlaylists(mainID);
                secondID = secondPL.Id;
            }
            if (mainID == secondID || mainID == null || secondID == null)
            {
                Console.WriteLine("Please select valid ids, and they can not be the same");
                return await GetMainAndSecondPlaylistIDs();
            }
            return new KeyValuePair<string, string>(mainID, secondID);
        }

        private static async Task<SimplePlaylist> ChoosePlaylistFromUserPlaylists(params string[] excludeIDs)
        {
            List<SimplePlaylist> userPlaylists = await SpotifyAPIManager.Instance.GetPlaylistsFromCurrentUser();
            if (excludeIDs != null)
            {
                userPlaylists = userPlaylists.Where(pl => excludeIDs.Contains(pl.Id)).ToList();
            }
            if (!userPlaylists.Any())
            {
                return null;
            }
            if (userPlaylists.Count == 1)
            {
                return userPlaylists[0];
            }
            Console.WriteLine("Choose a playlist:");
            for (int i = 0; i < userPlaylists.Count; i++)
            {
                SimplePlaylist pl = userPlaylists[i];
                Console.WriteLine((i + 1) + ") " + StringConverter.PlaylistToString(pl));
            }
            uint chosenInt;
            do
            {
                string chosen = Console.ReadLine();
                if (UInt32.TryParse(chosen, out chosenInt) && chosenInt > 0 && chosenInt < userPlaylists.Count + 1)
                {
                    break;
                }
                Console.WriteLine("Please write a valid number");
            } while (true);
            return userPlaylists[(int)(chosenInt - 1)];
        }

        private static async Task PrintNonPlayableTracks(SimplePlaylist pl, string playlistID)
        {
            if (playlistID == null)
            {
                playlistID = pl.Id;
            }
            Console.WriteLine("Nonplayable tracks for playlist " + (pl == null ? playlistID : StringConverter.PlaylistToString(pl)) + ":");
            List<FullTrack> allPlaylistTracks = await GetAllPlaylistTracks(pl, playlistID);
            FullTrack[] nonPlayableTracks = allPlaylistTracks.Where(t => t.Restrictions?.Any() ?? false || t.IsLocal).ToArray();
            if (nonPlayableTracks.Any())
            {
                string nonPlayableString = StringConverter.AllTracksToString("\n", nonPlayableTracks);
                Console.WriteLine(nonPlayableString);
            }
            else
            {
                Console.WriteLine("None");
            }
            Console.WriteLine("\n");

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

        private static async Task CheckDoubleTracks(SimplePlaylist pl, string playlistID)
        {
            List<FullTrack> allPlaylistTracks = await GetAllPlaylistTracks(pl, playlistID);
            FullTrack[] allSameTracks = allPlaylistTracks
                .Where(t1 => allPlaylistTracks
                    .Any(t2 => t2.Id != t1.Id && t1.Uri != t2.Uri && t1.Name.ToLower().StartsWith(t2.Name.ToLower()) && t1.Artists
                        .Select(a => a.Id).Any(aID => t2.Artists.Select(a => a.Id).Contains(aID))))
                .Distinct().OrderBy(t => t.Name).ToArray();
            Console.WriteLine("Double tracks for playlist " + (pl == null ? playlistID : StringConverter.PlaylistToString(pl)) + ":");
            if (allSameTracks.Any())
            {
                string sameTracksString = StringConverter.AllTracksToString("\n", allSameTracks);
                Console.WriteLine(sameTracksString);
            }
            else
            {
                Console.WriteLine("None");
            }
            Console.WriteLine("\n");
        }

        private static async Task Sync(string mainPLID, string secondPLID)
        {
            var mainTracksTask = GetAllTrackInfo(mainPLID);
            Task<List<FullTrack>> secondaryTracksTask = PlaylistManager.GetAllPlaylistTracks(secondPLID);
            await Task.WhenAll(mainTracksTask, secondaryTracksTask);
            var mainTracks = mainTracksTask.Result;
            List<FullTrack> secondaryTracks = secondaryTracksTask.Result;
            FullTrack[] doubleArtistTracks = secondaryTracks.Where(t => IsArtistPresent(t, secondaryTracks)).ToArray();
            Console.WriteLine("Double artists in secondary playlist:");
            if (doubleArtistTracks.Any())
            {
                string doubleArtistsString = StringConverter.AllTracksToString("\n", doubleArtistTracks);
                Console.WriteLine(doubleArtistsString);
            }
            else
            {
                Console.WriteLine("None");
            }
            Console.WriteLine("\n");

            ICollection<FullTrack> toAddLinked = new LinkedList<FullTrack>();
            var presentArtists = secondaryTracks.SelectMany(t => t.Artists.Select(a => a.Id));
            var orderedFirst = mainTracks.Where(kv => !kv.Value.Artists.Any(a => presentArtists.Contains(a.Id))).OrderBy(kv => kv.Key.AddedAt).Select(kv => kv.Value).ToList();
            for (var i = 0; i < orderedFirst.Count; i++)
            {
                var currentTrack = orderedFirst[i];
                var currentTrackArtitsIDs = currentTrack.Artists.Select(a => a.Id);
                if (currentTrackArtitsIDs.Any(aid => presentArtists.Contains(aid)))
                {
                    continue;
                }
                toAddLinked.Add(currentTrack);
                presentArtists = presentArtists.Concat(currentTrackArtitsIDs);
            }
            Console.WriteLine("Tracks to add:");
            if (toAddLinked.Any())
            {
                string toAddLinkedString = StringConverter.AllTracksToString("\n", toAddLinked.ToArray());
                Console.WriteLine(toAddLinkedString);
            }
            else
            {
                Console.WriteLine("None");
            }
            Console.WriteLine("\n");
        }

        private async static  Task<Dictionary<PlaylistTrack<IPlayableItem>, FullTrack>> GetAllTrackInfo(string plID)
        {
            var items = await SpotifyAPIManager.Instance.GetAllItemsFromPlaylist(plID);
            return PlaylistManager.GetFullTracksDict(items);
        }

        private static bool IsArtistPresent(FullTrack trackToCheck, List<FullTrack> tracks)
        {
            IEnumerable<string> artistIDsOfTrackToCheck = trackToCheck.Artists.Select(a => a.Id);
            IEnumerable<FullTrack> warningTracks = tracks.Where(t => t.Id != trackToCheck.Id && artistIDsOfTrackToCheck.Any(aid => t.Artists.Any(a => aid.Contains(a.Id))));
            if (!warningTracks.Any())
            {
                return false;
            }
            if (warningTracks.Any(t => t.Artists.Count < 2))
            {
                return true;
            }
            IEnumerable<string> furtherArtistIDs = warningTracks.SelectMany(t => t.Artists.Select(a => a.Id)).Where(aid => artistIDsOfTrackToCheck.Contains(aid)).Distinct();
            List<FullTrack> trackCopy = new List<FullTrack>();
            trackCopy.RemoveAll(t => warningTracks.Contains(t));
            IEnumerable<string> allTracksArtistIds = trackCopy.SelectMany(t => t.Artists.Select(a => a.Id)).Distinct();
            return furtherArtistIDs.Any(aid1 => allTracksArtistIds.Contains(aid1));
        }
    }
}
