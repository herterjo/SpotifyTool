using SpotifyTool.Logger;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.ConsoleMenu
{
    public class UserEditMenu : LoopMenu, IDisposable
    {
        private readonly UserEditMenuActions Actions;

        private UserEditMenu(UserEditMenuActions playlistEditMenuActions) : base(new List<KeyValuePair<string, Func<Task>>>() {
                    new KeyValuePair<string, Func<Task>>("Like and add", playlistEditMenuActions.LikeAndAdd),
                    new KeyValuePair<string, Func<Task>>("Unlike and remove",  playlistEditMenuActions.UnlikeAndRemove),
                    new KeyValuePair<string, Func<Task>>("Like", playlistEditMenuActions.Like),
                    new KeyValuePair<string, Func<Task>>("Add", playlistEditMenuActions.Add),
                    new KeyValuePair<string, Func<Task>>("Unlike", playlistEditMenuActions.Unlike),
                    new KeyValuePair<string, Func<Task>>("Remove", playlistEditMenuActions.Remove),
                    new KeyValuePair<string, Func<Task>>("Search in playlist", playlistEditMenuActions.SearchPlaylist),
                    new KeyValuePair<string, Func<Task>>("Search in library", playlistEditMenuActions.SearchLibrary)
                }, 0)
        {
            OnExit += Dispose;
            Actions = playlistEditMenuActions;
        }

        public static async Task<UserEditMenu> GetUserEditMenuActions(LogFileManager logFileManager)
        {
            var actions = await UserEditMenuActions.GetUserEditMenuActions(logFileManager);
            return new UserEditMenu(actions);
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
