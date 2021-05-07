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
            KeyValuePair<string, string> ids = await GetMainAndSecondPlaylistIDs();
            await Sync(ids.Key, ids.Value);
        }

        public async Task FindDoubleArtists()
        {
            KeyValuePair<string, string> idsDoubleArtists = await GetMainAndSecondPlaylistIDs();
            await FindDoubleArtists(idsDoubleArtists.Value);
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
            KeyValuePair<string, string> idsForAll = await GetMainAndSecondPlaylistIDs();
            await AllAnalytics(idsForAll.Key, idsForAll.Value);
        }

        private async Task<KeyValuePair<string, string>> GetMainAndSecondPlaylistIDs()
        {
            KeyValuePair<string, string> configIDs = await ConfigManager.GetMainAndOneArtistPlaylistID();
            string mainID = configIDs.Key;
            string secondID = configIDs.Value;
            if (String.IsNullOrWhiteSpace(mainID))
            {
                Console.WriteLine("Selecting main playlist:");
                SimplePlaylist mainPL = await MenuHelper.ChoosePlaylistFromUserPlaylists();
                mainID = mainPL.Id;
            }
            if (String.IsNullOrWhiteSpace(secondID))
            {
                Console.WriteLine("Selecting playlist where every artist is present only once:");
                SimplePlaylist secondPL = await MenuHelper.ChoosePlaylistFromUserPlaylists(mainID);
                secondID = secondPL.Id;
            }
            if (mainID == secondID || mainID == null || secondID == null)
            {
                Console.WriteLine("Please select valid ids, and they can not be the same");
                return await GetMainAndSecondPlaylistIDs();
            }
            return new KeyValuePair<string, string>(mainID, secondID);
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

        public Task RefreshAllUserPlaylistsAndLibraryTracks()
        {
            return Task.WhenAll(PlaylistManager.RefreshAllUserPlaylists(), LibraryManager.RefreshLibraryTracksIfCached());
        }

        public async Task CrossCheckLikedAndPlaylist(SimplePlaylist playlist, string playlistID)
        {
            if (playlistID == null)
            {
                playlistID = playlist.Id;
            }
            KeyValuePair<List<SavedTrack>, List<FullPlaylistTrack>> tracks = await Analytics.CrossCheckLikedAndPlaylist(playlist, playlistID);
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
    }
}
