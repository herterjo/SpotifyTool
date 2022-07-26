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

        private MainMenu(LogFileManager logFileManager, MainMenuActions mainMenuActions) : base(new List<(string Name, Func<Task> Action)>() {
                ("Log in", SpotifyAPIManager.Instance.GetUser),
                ("Refresh all cached playlists and current library", MainMenuActions.RefreshAllUserPlaylistsAndLibraryTracks),
                ("All analytics", mainMenuActions.AllAnalytics),
                ("Get non playble tracks",  mainMenuActions.PrintNonPlayableTracks),
                ("Get double tracks in playlist", mainMenuActions.CheckDoubleTracks),
                ("Get double artists in playlist (one time transitive)", mainMenuActions.FindDoubleArtists),
                ("Remove items from ArtistOnlyOnce playlist missing from main playlist", mainMenuActions.CheckSecondaryToPrimaryPlaylist),
                ("Sync main playlist with ArtistOnlyOnce playlist", mainMenuActions.SyncMainAndSecond),
                ("Get double tracks in library", mainMenuActions.CheckDoubleLibraryTracks),
                ("Cross check users library with playlist", mainMenuActions.CrossCheckLikedAndPlaylist),
                ("Edit playlist", async() => {
                    PlaylistEditMenu menu = await PlaylistEditMenu.GetPlaylistEditMenuActions(logFileManager);
                    await menu.UseMenu();
                }),
                ("Edit library", async() => {
                    LibraryEditMenu menu = new LibraryEditMenu(logFileManager);
                    await menu.UseMenu();
                }),
                ("Enqueue more tracks from artist", MainMenuActions.EnqueueArtistTracks),
                ("Enqueue top tracks from artist", MainMenuActions.EnqueueArtistTopTracks)
            }, 0)
        {
        }
    }
}
