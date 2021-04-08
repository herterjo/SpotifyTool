using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyTool.Config;
using SpotifyTool.Logger;
using SpotifyTool.SpotifyAPI;
using SpotifyTool.SpotifyObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool
{
    public class Program
    {
        public static LogFileManager LogFileManager;


        public static void Main(string[] args)
        {
            LogFileManager = LogFileManager.GetNewManager("logfile.txt").Result;
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
                Console.WriteLine("4) Get double artists in playlist (one time transitive)");
                Console.WriteLine("5) Sync main playlist with ArtistOnlyOnce playlist");
                Console.WriteLine("6) Cross check users library with playlist");
                Console.WriteLine("7) Refresh all cached playlists and current library");
                Console.WriteLine("8) Log in");
                Console.WriteLine("9) Edit Playlist");
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
                        KeyValuePair<string, string> idsDoubleArtists = await GetMainAndSecondPlaylistIDs();
                        await FindDoubleArtists(idsDoubleArtists.Value);
                        break;
                    case 5:
                        KeyValuePair<string, string> ids = await GetMainAndSecondPlaylistIDs();
                        await Sync(ids.Key, ids.Value);
                        break;
                    case 6:
                        SimplePlaylist ccPL = await ChoosePlaylistFromUserPlaylists();
                        await CrossCheckLikedAndPlaylist(ccPL, null);
                        break;
                    case 7:
                        await RefreshAllUserPlaylistsAndLibraryTracks();
                        break;
                    case 8:
                        await SpotifyAPIManager.Instance.LogInRequest();
                        break;
                    case 9:

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
                userPlaylists = userPlaylists.Where(pl => !excludeIDs.Contains(pl.Id)).ToList();
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
            await LogFileManager.WriteToLogAndConsole("Nonplayable tracks for playlist " + (pl == null ? playlistID : StringConverter.PlaylistToString(pl)) + ":");
            FullTrack[] nonPlayableTracks = await Analytics.GetNonPlayableTracks(pl, playlistID);
            if (nonPlayableTracks.Any())
            {
                string nonPlayableString = StringConverter.AllTracksToString("\n", nonPlayableTracks);
                await LogFileManager.WriteToLogAndConsole(nonPlayableString);
            }
            else
            {
                await LogFileManager.WriteToLogAndConsole("None");
            }
            await LogFileManager.WriteToLogAndConsole("\n");

        }

        private static async Task CheckDoubleTracks(SimplePlaylist pl, string playlistID)
        {
            await LogFileManager.WriteToLogAndConsole("Double tracks for playlist " + (pl == null ? playlistID : StringConverter.PlaylistToString(pl)) + ":");
            FullTrack[] allSameTracks = await Analytics.GetDoubleTracks(pl, playlistID);
            if (allSameTracks.Any())
            {
                string sameTracksString = StringConverter.AllTracksToString("\n", allSameTracks);
                await LogFileManager.WriteToLogAndConsole(sameTracksString);
            }
            else
            {
                await LogFileManager.WriteToLogAndConsole("None");
            }
            await LogFileManager.WriteToLogAndConsole("\n");
        }

        private static async Task AllAnalytics(string mainID, string secondID)
        {
            await PlaylistManager.RefreshSinglePlaylist(mainID);
            await PlaylistManager.RefreshSinglePlaylist(secondID);
            await LibraryManager.RefreshLibraryTracksForCurrentUser();
            await PrintNonPlayableTracks(null, mainID);
            await PrintNonPlayableTracks(null, secondID);
            await CheckDoubleTracks(null, mainID);
            await CheckDoubleTracks(null, secondID);
            await CrossCheckLikedAndPlaylist(null, mainID);
            await FindDoubleArtists(secondID);
            await Sync(mainID, secondID);
        }

        private static async Task FindDoubleArtists(string secondPLID)
        {
            await LogFileManager.WriteToLogAndConsole("Double artists in secondary playlist " + secondPLID + ":");
            List<FullPlaylistTrack> secondPL = await PlaylistManager.GetAllPlaylistTracks(secondPLID);
            var tracks = PlaylistManager.GetAllPlaylistTrackInfo(secondPL);
            FullTrack[] doubleArtistTracks = Analytics.GetDoubleArtistsTracks(tracks).OrderBy(t => t.Artists.OrderBy(a => a.Name).First().Name).ToArray();
            if (doubleArtistTracks.Any())
            {
                string doubleArtistsString = StringConverter.AllTracksToString("\n", doubleArtistTracks);
                await LogFileManager.WriteToLogAndConsole(doubleArtistsString);
            }
            else
            {
                await LogFileManager.WriteToLogAndConsole("None");
            }
            await LogFileManager.WriteToLogAndConsole("\n");
        }

        private static async Task Sync(string mainPLID, string secondPLID)
        {
            Task<List<FullPlaylistTrack>> mainTracksTask = PlaylistManager.GetAllPlaylistTracks(mainPLID);
            Task<List<FullPlaylistTrack>> secondaryTracksTask = PlaylistManager.GetAllPlaylistTracks(secondPLID);
            await Task.WhenAll(mainTracksTask, secondaryTracksTask);
            List<FullPlaylistTrack> mainTracks = mainTracksTask.Result;
            List<FullTrack> secondaryTracks = PlaylistManager.GetAllPlaylistTrackInfo(secondaryTracksTask.Result);
            await LogFileManager.WriteToLogAndConsole("Tracks to add from main playlist " + mainPLID + " to secondary playlist " + secondPLID + ":");
            ICollection<FullTrack> toAdd = Analytics.GetTracksToAddToSecondary(mainTracks, secondaryTracks);
            if (toAdd.Any())
            {
                string toAddLinkedString = StringConverter.AllTracksToString("\n", toAdd.ToArray());
                await LogFileManager.WriteToLogAndConsole(toAddLinkedString);
            }
            else
            {
                await LogFileManager.WriteToLogAndConsole("None");
            }
            await LogFileManager.WriteToLogAndConsole("\n");

            if (!toAdd.Any())
            {
                return;
            }

            Console.WriteLine("Would you like to add the playlist items now to playlist " + secondPLID + "? (y/n)");
            string answer = Console.ReadLine();
            if (answer != "y")
            {
                return;
            }
            string addedJSON = JsonConvert.SerializeObject(toAdd);
            await File.WriteAllTextAsync("allBatchAddedTracks.json", addedJSON);
            try
            {
                using (var plManager = await PlaylistEditor.GetPlaylistEditor(secondPLID))
                {
                    await plManager.BatchAdd(toAdd.ToList());
                }
            }
            catch (APIUnauthorizedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static Task RefreshAllUserPlaylistsAndLibraryTracks()
        {
            return Task.WhenAll(PlaylistManager.RefreshAllUserPlaylists(), LibraryManager.RefreshLibraryTracksIfCached());
        }

        public static async Task CrossCheckLikedAndPlaylist(SimplePlaylist playlist, string playlistID)
        {
            if (playlistID == null)
            {
                playlistID = playlist.Id;
            }
            var tracks = await Analytics.CrossCheckLikedAndPlaylist(playlist, playlistID);
            await LogFileManager.WriteToLogAndConsole("Tracks not in library but in playlist " + playlistID + ":");
            if (tracks.Value.Any())
            {
                string notInLib = StringConverter.AllTracksToString("\n", tracks.Value.Select(fpt => fpt.TrackInfo).ToArray());
                await LogFileManager.WriteToLogAndConsole(notInLib);
            }
            else
            {
                await LogFileManager.WriteToLogAndConsole("None");
            }

            await LogFileManager.WriteToLogAndConsole("\n");

            await LogFileManager.WriteToLogAndConsole("Tracks not in playlist " + playlistID + " but in library:");
            if (tracks.Key.Any())
            {
                string notInPl = StringConverter.AllTracksToString("\n", tracks.Key.Select(fpt => fpt.Track).ToArray());
                await LogFileManager.WriteToLogAndConsole(notInPl);
            }
            else
            {
                await LogFileManager.WriteToLogAndConsole("None");
            }

        }
    }
}
