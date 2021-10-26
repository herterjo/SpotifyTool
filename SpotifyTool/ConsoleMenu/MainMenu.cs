using SpotifyTool.Logger;
using SpotifyTool.SpotifyAPI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpotifyTool.ConsoleMenu
{
    public class MainMenu : LoopMenu
    {
        public MainMenu(LogFileManager logFileManager) : this(logFileManager, new MainMenuActions(logFileManager))
        {
        }

        private MainMenu(LogFileManager logFileManager, MainMenuActions mainMenuActions) : base(new List<KeyValuePair<string, Func<Task>>>() {
                    new KeyValuePair<string, Func<Task>>("Log in", SpotifyAPIManager.Instance.GetUser),
                    new KeyValuePair<string, Func<Task>>("Refresh all cached playlists and current library", mainMenuActions.RefreshAllUserPlaylistsAndLibraryTracks),
                    new KeyValuePair<string, Func<Task>>("All analytics", mainMenuActions.AllAnalytics),
                    new KeyValuePair<string, Func<Task>>("Get non playble tracks",  mainMenuActions.PrintNonPlayableTracks),
                    new KeyValuePair<string, Func<Task>>("Get double tracks in playlist", mainMenuActions.CheckDoubleTracks),
                    new KeyValuePair<string, Func<Task>>("Get double artists in playlist (one time transitive)", mainMenuActions.FindDoubleArtists),
                    new KeyValuePair<string, Func<Task>>("Sync main playlist with ArtistOnlyOnce playlist", mainMenuActions.SyncMainAndSecond),
                    new KeyValuePair<string, Func<Task>>("Get double tracks in library", mainMenuActions.CheckDoubleLibraryTracks),
                    new KeyValuePair<string, Func<Task>>("Cross check users library with playlist", mainMenuActions.CrossCheckLikedAndPlaylist),
                    new KeyValuePair<string, Func<Task>>("Edit playlist", async() => {
                        PlaylistEditMenu menu = await PlaylistEditMenu.GetPlaylistEditMenuActions(logFileManager);
                        await menu.UseMenu();
                    }),
                    new KeyValuePair<string, Func<Task>>("Edit library", async() => {
                        LibraryEditMenu menu = new LibraryEditMenu(logFileManager);
                        await menu.UseMenu();
                    }),
                    new KeyValuePair<string, Func<Task>>("Enqueue more tracks from artist", mainMenuActions.EnqueueArtistTracks),
                    new KeyValuePair<string, Func<Task>>("Enqueue top tracks from artist", mainMenuActions.EnqueueArtistTopTracks)
                }, 0)
        {
        }
    }
}
