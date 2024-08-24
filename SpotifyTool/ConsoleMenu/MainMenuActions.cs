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
            List<FullPlaylist> ccPL = await MenuHelper.ChoosePlaylistsFromUserPlaylists();
            await CrossCheckLikedAndPlaylist(ccPL, null);
        }

        public async Task SyncMainAndSecond()
        {
            (string[] mainIds, string secondId) = await GetMainAndSecondPlaylistIDs();
            await Sync(mainIds, secondId);
        }

        public async Task FindDoubleArtists()
        {
            (string[] _, string secondId) = await GetMainAndSecondPlaylistIDs();
            await FindDoubleArtists(secondId);
        }

        public async Task CheckDoubleTracks()
        {
            List<FullPlaylist> dPL = await MenuHelper.ChoosePlaylistsFromUserPlaylists();
            await CheckDoubleTracks(dPL, null);
        }

        public async Task PrintNonPlayableTracks()
        {
            List<FullPlaylist> npPL = await MenuHelper.ChoosePlaylistsFromUserPlaylists();
            await PrintNonPlayableTracks(npPL, null);
        }

        public async Task AllAnalytics()
        {
            (string[] mainIds, string secondId) = await GetMainAndSecondPlaylistIDs();
            await AllAnalytics(mainIds, secondId);
        }

        private async Task<(string[] MainIds, string SecondId)> GetMainAndSecondPlaylistIDs()
        {
            (string[] mainIds, string secondId) = await ConfigManager.GetMainAndOneArtistPlaylistID();
            mainIds = mainIds?.Where(m => !String.IsNullOrWhiteSpace(m)).ToArray();
            if (mainIds == null || !mainIds.Any())
            {
                Console.WriteLine("Selecting main playlist:");
                List<FullPlaylist> mainPLs = await MenuHelper.ChoosePlaylistsFromUserPlaylists();
                mainIds = mainPLs.Select(pl => pl.Id).ToArray();
            }
            if (String.IsNullOrWhiteSpace(secondId))
            {
                Console.WriteLine("Selecting playlist where every artist is present only once:");
                FullPlaylist secondPL = await MenuHelper.ChoosePlaylistFromUserPlaylists(mainIds);
                secondId = secondPL.Id;
            }
            if (!mainIds.Any() || secondId == null || mainIds.Any(id => id == secondId))
            {
                Console.WriteLine("Please select valid ids, and they can not be the same");
                return await GetMainAndSecondPlaylistIDs();
            }
            return (mainIds, secondId);
        }

        private async Task PrintNonPlayableTracks(IEnumerable<FullPlaylist> pls, IEnumerable<string> playlistIDs)
        {
            if (playlistIDs == null)
            {
                playlistIDs = pls.Select(pl => pl.Id).ToList();
            }
            await LogFileManager.WriteToLogAndConsole("Nonplayable tracks for playlist " + StringConverter.GetPrintablePlaylistIds(playlistIDs, pls) + ":");
            FullTrack[] nonPlayableTracks = await Analytics.GetNonPlayableTracks(pls, playlistIDs);
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

        private async Task CheckDoubleTracks(IEnumerable<FullPlaylist> pls, IEnumerable<string> playlistIDs)
        {
            await LogFileManager.WriteToLogAndConsole("Double tracks for playlist " + StringConverter.GetPrintablePlaylistIds(playlistIDs, pls) + ":");
            FullTrack[] allSameTracks = await Analytics.GetDoubleTracks(pls, playlistIDs);
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

        private async Task AllAnalytics(IEnumerable<string> mainIDs, string secondID)
        {
            foreach (var mainID in mainIDs)
            {
                await PlaylistManager.RefreshSinglePlaylist(mainID);
            }
            await PlaylistManager.RefreshSinglePlaylist(secondID);
            await LibraryManager.RefreshLibraryTracksForCurrentUser();
            await PrintNonPlayableTracks(null, mainIDs);
            await PrintNonPlayableTracks(null, new string[] { secondID });
            await CheckDoubleTracks(null, mainIDs);
            await CheckDoubleTracks(null, new string[] { secondID });
            await CheckDoubleLibraryTracks();
            await CrossCheckLikedAndPlaylist(null, mainIDs);
            await FindDoubleArtists(secondID);
            await CheckSecondaryToPrimaryPlaylist(mainIDs, secondID);
            await Sync(mainIDs, secondID);
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

        private async Task Sync(IEnumerable<string> mainPLIDs, string secondPLID)
        {
            Task<List<FullPlaylistTrack>> mainTracksTask = PlaylistManager.GetAllPlaylistsTracks(mainPLIDs);
            Task<List<FullPlaylistTrack>> secondaryTracksTask = PlaylistManager.GetAllPlaylistTracks(secondPLID);
            await Task.WhenAll(mainTracksTask, secondaryTracksTask);
            List<FullPlaylistTrack> mainTracks = mainTracksTask.Result;
            List<FullTrack> secondaryTracks = PlaylistManager.GetAllPlaylistTrackInfo(secondaryTracksTask.Result);
            await LogFileManager.WriteToLogAndConsole("Tracks to add from main playlist " + StringConverter.GetPrintablePlaylistIds(mainPLIDs) + " to secondary playlist " + secondPLID + ":");
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

        public async Task CrossCheckLikedAndPlaylist(IEnumerable<FullPlaylist> playlists, IEnumerable<string> playlistIDs)
        {
            if (playlistIDs == null)
            {
                playlistIDs = playlists.Select(pl => pl.Id).ToList();
            }
            (List<SavedTrack> missingFromPlaylist, List<FullPlaylistTrack> missingFromLibrary) = await Analytics.CrossCheckLikedAndPlaylist(playlists, playlistIDs);
            await LogFileManager.WriteToLogAndConsole("Tracks not in library but in playlist " + StringConverter.GetPrintablePlaylistIds(playlistIDs) + ":");
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

            await LogFileManager.WriteToLogAndConsole("Tracks not in playlist " + StringConverter.GetPrintablePlaylistIds(playlistIDs) + " but in library:");
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
            (string[] primaryIds, string secondaryId) = await GetMainAndSecondPlaylistIDs();
            await CheckSecondaryToPrimaryPlaylist(primaryIds, secondaryId);
        }

        public async Task CheckSecondaryToPrimaryPlaylist(IEnumerable<string> primaryIds, string secondaryId)
        {
            var writeTask = LogFileManager.WriteToLogAndConsole("Tracks in secondary playlist " + secondaryId + " but not in primary playlist " + StringConverter.GetPrintablePlaylistIds(primaryIds) + ":");
            var notInMain = await Analytics.GetTracksInSecondaryButNotInPrimary(primaryIds, secondaryId);
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
