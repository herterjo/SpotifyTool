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
    public class UserEditMenuActions : LogFileManagerContainer, IDisposable
    {
        private const string ConfirmationString = "y";
        private readonly PlaylistEditor PlaylistEditor;
        protected UserEditMenuActions(LogFileManager logFileManager, PlaylistEditor playlistEditor) : base(logFileManager)
        {
            this.PlaylistEditor = playlistEditor ?? throw new ArgumentNullException(nameof(playlistEditor));
        }

        public static async Task<UserEditMenuActions> GetUserEditMenuActions(LogFileManager logFileManager)
        {
            SimplePlaylist playlist = await MenuHelper.ChoosePlaylistFromUserPlaylists();
            PlaylistEditor editor = await PlaylistEditor.GetPlaylistEditor(playlist);
            await logFileManager.WriteToLog("Chosen playlist to edit: " + playlist.Id);
            return new UserEditMenuActions(logFileManager, editor);
        }

        public async Task LikeAndAdd()
        {
            List<string> uris = await this.GetUris("like and add");
            await this.PlaylistEditor.AddAndLike(uris);
        }

        public async Task UnlikeAndAdd()
        {
            List<string> uris = await this.GetUris("unlike and remove");
            await this.PlaylistEditor.RemoveAndUnlike(uris);
        }

        public async Task Like()
        {
            List<string> uris = await this.GetUris("like");
            await LibraryManager.LikeTracks(uris);
        }

        public async Task Add()
        {
            List<string> uris = await this.GetUris("add");
            await this.PlaylistEditor.Add(uris);
        }

        public async Task Unlike()
        {
            List<string> uris = await this.GetUris("unlike");
            await LibraryManager.UnlikeTracks(uris);
        }

        public async Task Remove()
        {
            List<string> uris = await this.GetUris("remove");
            await this.PlaylistEditor.Remove(uris);
        }

        public Task SearchPlaylist()
        {
            return this.Search(async s =>
            {
                List<FullPlaylistTrack> playlist = await PlaylistManager.GetAllPlaylistTracks(this.PlaylistEditor.PlaylistID);
                return PlaylistManager.GetAllPlaylistTrackInfo(playlist);
            });
        }

        public Task SearchLibrary()
        {
            return this.Search(async s =>
            {
                List<SavedTrack> library = await LibraryManager.GetLibraryTracksForCurrentUser();
                return LibraryManager.GetFullTracks(library);
            });
        }

        private async Task Search(Func<string, Task<List<FullTrack>>> getFullTracks)
        {
            Console.WriteLine("Write the information to search for:");
            string searchString = Console.ReadLine();
            if (String.IsNullOrWhiteSpace(searchString))
            {
                Console.WriteLine("Nothing entered");
                return;
            }
            searchString = searchString.ToLowerInvariant();
            List<FullTrack> allTracks = await getFullTracks(searchString);
            FullTrack[] found = allTracks.Where(t => t.Uri.ToLowerInvariant().Contains(searchString)
                    || t.Name.ToLowerInvariant().Contains(searchString)
                    || t.Artists.Any(a => a.Name.ToLowerInvariant().Contains(searchString))
                    || t.Album.Name.ToLowerInvariant().Contains(searchString))
                .ToArray();
            Console.WriteLine(StringConverter.AllTracksToString("\n", found));
        }

        private async Task<List<string>> GetUris(string name)
        {
            await this.LogFileManager.WriteToLogAndConsole("\n");
            if (!String.IsNullOrWhiteSpace(name))
            {
                await this.LogFileManager.WriteToLogAndConsole("Please write the Spotify URIs for command \"" + name + "\":");
            }
            List<string> uris = Console.ReadLine().Split(" ").Where(s => !String.IsNullOrWhiteSpace(s)).ToList();
            await this.LogFileManager.WriteToLog(String.Join(" ", uris));
            return uris;
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
