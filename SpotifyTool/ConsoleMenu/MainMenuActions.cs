using SpotifyAPI.Web;
using SpotifyTool.Config;
using SpotifyTool.Logger;
using SpotifyTool.SpotifyAPI;
using SpotifyTool.SpotifyObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.ConsoleMenu
{
    public class MainMenuActions : LogFileManagerContainer
    {
        public MainMenuActions(LogFileManager logFileManager) : base(logFileManager)
        {
        }

        public async Task CrossCheckLikedAndPlaylist()
        {
            SimplePlaylist ccPL = await MenuHelper.ChoosePlaylistFromUserPlaylists();
            await CrossCheckLikedAndPlaylist(ccPL, null);
        }

        public async Task SyncMainAndSecond()
        {
            (string mainId, string secondId) = await GetMainAndSecondPlaylistIDs();
            await Sync(mainId, secondId);
        }

        public async Task FindDoubleArtists()
        {
            (string _, string secondId) = await GetMainAndSecondPlaylistIDs();
            await FindDoubleArtists(secondId);
        }

        public async Task CheckDoubleTracks()
        {
            SimplePlaylist dPL = await MenuHelper.ChoosePlaylistFromUserPlaylists();
            await CheckDoubleTracks(dPL, null);
        }

        public async Task PrintNonPlayableTracks()
        {
            SimplePlaylist npPL = await MenuHelper.ChoosePlaylistFromUserPlaylists();
            await PrintNonPlayableTracks(npPL, null);
        }

        public async Task AllAnalytics()
        {
            (string mainId, string secondId) = await GetMainAndSecondPlaylistIDs();
            await AllAnalytics(mainId, secondId);
        }

        private async Task<(string MainId, string SecondId)> GetMainAndSecondPlaylistIDs()
        {
            (string mainId, string secondId) = await ConfigManager.GetMainAndOneArtistPlaylistID();
            if (String.IsNullOrWhiteSpace(mainId))
            {
                Console.WriteLine("Selecting main playlist:");
                SimplePlaylist mainPL = await MenuHelper.ChoosePlaylistFromUserPlaylists();
                mainId = mainPL.Id;
            }
            if (String.IsNullOrWhiteSpace(secondId))
            {
                Console.WriteLine("Selecting playlist where every artist is present only once:");
                SimplePlaylist secondPL = await MenuHelper.ChoosePlaylistFromUserPlaylists(mainId);
                secondId = secondPL.Id;
            }
            if (mainId == secondId || mainId == null || secondId == null)
            {
                Console.WriteLine("Please select valid ids, and they can not be the same");
                return await GetMainAndSecondPlaylistIDs();
            }
            return (mainId, secondId);
        }

        private async Task PrintNonPlayableTracks(SimplePlaylist pl, string playlistID)
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

        private async Task CheckDoubleTracks(SimplePlaylist pl, string playlistID)
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

        private async Task AllAnalytics(string mainID, string secondID)
        {
            await PlaylistManager.RefreshSinglePlaylist(mainID);
            await PlaylistManager.RefreshSinglePlaylist(secondID);
            await LibraryManager.RefreshLibraryTracksForCurrentUser();
            await PrintNonPlayableTracks(null, mainID);
            await PrintNonPlayableTracks(null, secondID);
            await CheckDoubleTracks(null, mainID);
            await CheckDoubleTracks(null, secondID);
            await CheckDoubleLibraryTracks();
            await CrossCheckLikedAndPlaylist(null, mainID);
            await FindDoubleArtists(secondID);
            await CheckSecondaryToPrimaryPlaylist(mainID, secondID);
            await Sync(mainID, secondID);
        }

        private async Task FindDoubleArtists(string secondPLID)
        {
            await LogFileManager.WriteToLogAndConsole("Double artists in secondary playlist " + secondPLID + ":");
            List<FullPlaylistTrack> secondPL = await PlaylistManager.GetAllPlaylistTracks(secondPLID);
            List<FullTrack> tracks = PlaylistManager.GetAllPlaylistTrackInfo(secondPL);
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

        private async Task Sync(string mainPLID, string secondPLID)
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
            await LogFileManager.WriteToLogAndConsole("\n");
            if (answer != "y")
            {
                return;
            }
            await Serialization.SerializeJson(toAdd, "allBatchAddedTracks.json");
            try
            {
                using (PlaylistEditor plManager = await PlaylistEditor.GetPlaylistEditor(secondPLID))
                {
                    await plManager.Add(toAdd.ToList());
                }
            }
            catch (APIUnauthorizedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async Task RefreshAllUserPlaylistsAndLibraryTracks()
        {
            //Do this one after another to not generate too many requests
            await PlaylistManager.RefreshAllUserPlaylists();
            Console.WriteLine("Done with refreshing all cached playlists");
            await LibraryManager.RefreshLibraryTracksIfCached();
            Console.WriteLine("Done with refreshing cached library");
        }

        public async Task CrossCheckLikedAndPlaylist(SimplePlaylist playlist, string playlistID)
        {
            if (playlistID == null)
            {
                playlistID = playlist.Id;
            }
            (List<SavedTrack> missingFromPlaylist, List<FullPlaylistTrack> missingFromLibrary) = await Analytics.CrossCheckLikedAndPlaylist(playlist, playlistID);
            await LogFileManager.WriteToLogAndConsole("Tracks not in library but in playlist " + playlistID + ":");
            if (missingFromLibrary.Any())
            {
                string notInLib = StringConverter.AllTracksToString("\n", missingFromLibrary.Select(fpt => fpt.TrackInfo).ToArray());
                await LogFileManager.WriteToLogAndConsole(notInLib);
            }
            else
            {
                await LogFileManager.WriteToLogAndConsole("None");
            }

            await LogFileManager.WriteToLogAndConsole("\n");

            await LogFileManager.WriteToLogAndConsole("Tracks not in playlist " + playlistID + " but in library:");
            if (missingFromPlaylist.Any())
            {
                string notInPl = StringConverter.AllTracksToString("\n", missingFromPlaylist.Select(fpt => fpt.Track).ToArray());
                await LogFileManager.WriteToLogAndConsole(notInPl);
            }
            else
            {
                await LogFileManager.WriteToLogAndConsole("None");
            }
            await LogFileManager.WriteToLogAndConsole("\n");
        }

        public async Task CheckDoubleLibraryTracks()
        {
            FullTrack[] doubleTracks = await Analytics.GetDoubleLibraryTracks();
            await LogFileManager.WriteToLogAndConsole("Tracks double in library:");
            if (doubleTracks.Any())
            {
                string doubleTracksString = StringConverter.AllTracksToString("\n", doubleTracks);
                await LogFileManager.WriteToLogAndConsole(doubleTracksString);
            }
            else
            {
                await LogFileManager.WriteToLogAndConsole("None");
            }
            await LogFileManager.WriteToLogAndConsole("\n");
        }

        public static async Task EnqueueArtistTracks()
        {
            var artistId = await MenuHelper.GetArtistId();
            Console.WriteLine("Please write 0 for all tracks and 1 for the tracks since the last liked one");
            var chosenInt = MenuHelper.GetInt(0, 1);
            var onlyLatest = chosenInt != 0;
            Console.WriteLine("Please write 0 to exclude variants (remixes etc) for new found tracks and 1 to include all variants");
            chosenInt = MenuHelper.GetInt(0, 1);
            var includeAllVariants = chosenInt != 0;
            await DiscoveryManager.EnqueueFromArtistAlbums(artistId, onlyLatest, includeAllVariants);
        }

        public static async Task EnqueueArtistTopTracks()
        {
            var artistId = await MenuHelper.GetArtistId();
            Console.WriteLine("Please write 0 to exclude variants (remixes etc) for new found tracks and 1 to include all variants");
            var chosenInt = MenuHelper.GetInt(0, 1);
            var includeAllVariants = chosenInt != 0;
            await DiscoveryManager.EnqueueArtistTopTracks(artistId, includeAllVariants);
        }

        public async Task CheckSecondaryToPrimaryPlaylist()
        {
            (string primaryId, string secondaryId) = await GetMainAndSecondPlaylistIDs();
            await CheckSecondaryToPrimaryPlaylist(primaryId, secondaryId);
        }

        public async Task CheckSecondaryToPrimaryPlaylist(string primaryId, string secondaryId)
        {
            var writeTask = LogFileManager.WriteToLogAndConsole("Tracks in secondary playlist " + primaryId + " but not in primary playlist " + secondaryId + ":");
            var notInMain = await Analytics.GetTracksInSecondaryButNotInPrimary(primaryId, secondaryId);
            await writeTask;
            if (notInMain.Any())
            {
                string toAddLinkedString = StringConverter.AllPlalistTracksToString("\n", notInMain.ToArray());
                await LogFileManager.WriteToLogAndConsole(toAddLinkedString);
            }
            else
            {
                await LogFileManager.WriteToLogAndConsole("None");
            }
            await LogFileManager.WriteToLogAndConsole("\n");

            if (!notInMain.Any())
            {
                return;
            }

            Console.WriteLine("Would you like to remove the tracks from secondary playlist " + secondaryId + "? (y/n)");
            string answer = Console.ReadLine();
            await LogFileManager.WriteToLogAndConsole("\n");
            if (answer != "y")
            {
                return;
            }
            try
            {
                using (PlaylistEditor plManager = await PlaylistEditor.GetPlaylistEditor(secondaryId))
                {
                    await plManager.Remove(notInMain.Select(fpt => fpt.TrackInfo).ToList());
                }
            }
            catch (APIUnauthorizedException ex)
            {
                Console.WriteLine(ex.Message);
            }
            await PlaylistManager.RefreshSinglePlaylist(secondaryId);
        }
    }
}
