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
    public class LibraryEditMenuActions : LogFileManagerContainer
    {
        public LibraryEditMenuActions(LogFileManager logFileManager) : base(logFileManager)
        {
        }

        public async Task Like()
        {
            List<string> uris = await MenuHelper.GetTrackUris("like", LogFileManager);
            await LibraryManager.LikeTracks(uris);
        }

        public async Task Unlike()
        {
            List<string> uris = await MenuHelper.GetTrackUris("unlike", LogFileManager);
            await LibraryManager.UnlikeTracks(uris);
        }

        public static Task SearchLibrary()
        {
            return MenuHelper.Search(async s =>
            {
                List<SavedTrack> library = await LibraryManager.GetLibraryTracksForCurrentUser();
                return LibraryManager.GetFullTracks(library);
            });
        }
    }
}
