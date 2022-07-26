using SpotifyTool.Logger;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.ConsoleMenu
{
    public class PlaylistEditMenu : LoopMenu, IDisposable
    {
        private readonly PlaylistEditMenuActions Actions;

        private PlaylistEditMenu(PlaylistEditMenuActions playlistEditMenuActions) : base(new List<(string Name, Func<Task> Action)>() {
                    ("Like and add", playlistEditMenuActions.LikeAndAdd),
                    ("Unlike and remove",  playlistEditMenuActions.UnlikeAndRemove),
                    ("Add", playlistEditMenuActions.Add),
                    ("Remove", playlistEditMenuActions.Remove),
                    ("Search in playlist", playlistEditMenuActions.SearchPlaylist),
                }, 0)
        {
            OnExit += Dispose;
            Actions = playlistEditMenuActions;
        }

        public static async Task<PlaylistEditMenu> GetPlaylistEditMenuActions(LogFileManager logFileManager)
        {
            var actions = await PlaylistEditMenuActions.GetUserEditMenuActions(logFileManager);
            return new PlaylistEditMenu(actions);
        }

        #region IDisposable Support
        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposedValue)
            {
                return;
            }

            if (disposing)
            {
                OnExit -= Dispose;
                Actions?.Dispose();
            }
            this.disposedValue = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
        }
        #endregion
    }
}
