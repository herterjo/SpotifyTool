using SpotifyAPI.Web;
using SpotifyTool.Logger;
using SpotifyTool.SpotifyAPI;
using SpotifyTool.SpotifyObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.ConsoleMenu
{
    public class PlaylistEditMenuActions : LogFileManagerContainer, IDisposable
    {
        private readonly PlaylistEditor PlaylistEditor;
        protected PlaylistEditMenuActions(LogFileManager logFileManager, PlaylistEditor playlistEditor) : base(logFileManager)
        {
            this.PlaylistEditor = playlistEditor ?? throw new ArgumentNullException(nameof(playlistEditor));
        }

        public static async Task<PlaylistEditMenuActions> GetUserEditMenuActions(LogFileManager logFileManager)
        {
            SimplePlaylist playlist = await MenuHelper.ChoosePlaylistFromUserPlaylists();
            PlaylistEditor editor = await PlaylistEditor.GetPlaylistEditor(playlist);
            await logFileManager.WriteToLog("Chosen playlist to edit: " + playlist.Id);
            return new PlaylistEditMenuActions(logFileManager, editor);
        }

        public async Task LikeAndAdd()
        {
            List<string> uris = await MenuHelper.GetTrackUris("like and add", LogFileManager);
            await this.PlaylistEditor.AddAndLike(uris);
        }

        public async Task UnlikeAndRemove()
        {
            List<string> uris = await MenuHelper.GetTrackUris("unlike and remove", LogFileManager);
            await this.PlaylistEditor.RemoveAndUnlike(uris);
        }

        public async Task Add()
        {
            List<string> uris = await MenuHelper.GetTrackUris("add", LogFileManager);
            await this.PlaylistEditor.Add(uris);
        }

        public async Task Remove()
        {
            List<string> uris = await MenuHelper.GetTrackUris("remove", LogFileManager);
            await this.PlaylistEditor.Remove(uris);
        }

        public Task SearchPlaylist()
        {
            return MenuHelper.Search(async s =>
            {
                List<FullPlaylistTrack> playlist = await PlaylistManager.GetAllPlaylistTracks(this.PlaylistEditor.PlaylistID);
                return PlaylistManager.GetAllPlaylistTrackInfo(playlist);
            });
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
                this.PlaylistEditor?.Dispose();
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
