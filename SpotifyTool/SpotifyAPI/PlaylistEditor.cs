using SpotifyAPI.Web;
using SpotifyTool.SpotifyObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyAPI
{
    public class PlaylistEditor : IDisposable
    {
        public string PlaylistID { get; }
        public bool Valid { get; private set; }
        public static readonly APIUnauthorizedException UserNotOwnerException = new APIUnauthorizedException("Current user is not owner of playlist");

        protected PlaylistEditor(string playlistID)
        {
            ClientManager.AfterClientChange += this.SetValidState;
            this.Valid = true;
            this.PlaylistID = playlistID ?? throw new ArgumentNullException(nameof(playlistID));
        }

        public static async Task<PlaylistEditor> GetPlaylistEditor(string playlistId)
        {
            if (playlistId == null)
            {
                throw new ArgumentNullException(nameof(playlistId));
            }
            if (!await SpotifyAPIManager.Instance.IsCurrentUserOwner(playlistId))
            {
                throw UserNotOwnerException;
            }
            return new PlaylistEditor(playlistId);
        }

        public static async Task<PlaylistEditor> GetPlaylistEditor(SimplePlaylist playlist)
        {
            if (playlist == null)
            {
                throw new ArgumentNullException(nameof(playlist));
            }
            if (!await SpotifyAPIManager.Instance.IsCurrentUserOwner(playlist))
            {
                throw UserNotOwnerException;
            }
            return new PlaylistEditor(playlist.Id);
        }

        private void ThrowIfNotValid()
        {
            if (!this.Valid)
            {
                throw UserNotOwnerException;
            }
        }

        private async void SetValidState()
        {
            this.Valid = await SpotifyAPIManager.Instance.IsCurrentUserOwner(this.PlaylistID);
        }

        public Task Remove(string spotifyURI)
        {
            this.ThrowIfNotValid();
            return SpotifyAPIManager.Instance.RemoveFromPlaylist(this.PlaylistID, spotifyURI);
        }

        public Task Remove(FullTrack track)
        {
            return this.Remove(track.Uri);
        }

        public Task RemoveAndUnlike(string spotifyURI)
        {
            this.ThrowIfNotValid();
            Task removeTask = this.Remove(spotifyURI);
            Task unlikeTask = SpotifyAPIManager.Instance.Unlike(StringConverter.GetId(spotifyURI));
            return Task.WhenAll(removeTask, unlikeTask);
        }

        public Task RemoveAndUnlike(FullTrack track)
        {
            this.ThrowIfNotValid();
            Task removeTask = this.Remove(track.Uri);
            Task unlikeTask = SpotifyAPIManager.Instance.Unlike(track.Id);
            return Task.WhenAll(removeTask, unlikeTask);
        }

        public Task BatchAdd(List<string> trackUris)
        {
            this.ThrowIfNotValid();
            return SpotifyAPIManager.Instance.BatchAdd(this.PlaylistID, trackUris);
        }

        public Task BatchAdd(List<FullTrack> tracks)
        {
            return this.BatchAdd(tracks.Select(t => t.Uri).ToList());
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
                ClientManager.AfterClientChange -= this.SetValidState;
            }
            this.Valid = false;
            this.disposedValue = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
        }
        #endregion
    }
}
