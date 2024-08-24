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

        public static async Task<PlaylistEditor> GetPlaylistEditor(FullPlaylist playlist)
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

        public Task Remove(List<string> spotifyUris)
        {
            this.ThrowIfNotValid();
            return SpotifyAPIManager.Instance.RemoveFromPlaylist(this.PlaylistID, spotifyUris);
        }

        public Task Remove(List<FullTrack> tracks)
        {
            var spotifyUris = tracks.Select(t => t.Uri).ToList();
            return this.Remove(spotifyUris);
        }

        public Task Add(List<string> trackUris)
        {
            this.ThrowIfNotValid();
            return SpotifyAPIManager.Instance.AddToPlaylist(this.PlaylistID, trackUris);
        }

        public Task Add(List<FullTrack> tracks)
        {
            return this.Add(tracks.Select(t => t.Uri).ToList());
        }

        public Task RemoveAndUnlike(List<string> spotifyUris)
        {
            this.ThrowIfNotValid();
            Task removeTask = this.Remove(spotifyUris);
            Task unlikeTask = LibraryManager.UnlikeTracks(spotifyUris);
            return Task.WhenAll(removeTask, unlikeTask);
        }

        public Task RemoveAndUnlike(List<FullTrack> tracks)
        {
            this.ThrowIfNotValid();
            Task removeTask = this.Remove(tracks);
            Task unlikeTask = LibraryManager.UnlikeTracks(tracks);
            return Task.WhenAll(removeTask, unlikeTask);
        }

        public Task AddAndLike(List<string> spotifyUris)
        {
            this.ThrowIfNotValid();
            Task addTask = this.Add(spotifyUris);
            Task likeTask = LibraryManager.LikeTracks(spotifyUris);
            return Task.WhenAll(addTask, likeTask);
        }

        public Task AddAndLike(List<FullTrack> tracks)
        {
            this.ThrowIfNotValid();
            Task addTask = this.Add(tracks);
            Task likeTask = LibraryManager.LikeTracks(tracks);
            return Task.WhenAll(addTask, likeTask);
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
