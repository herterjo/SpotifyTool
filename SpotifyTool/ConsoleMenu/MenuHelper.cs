using SpotifyAPI.Web;
using SpotifyTool.SpotifyAPI;
using SpotifyTool.SpotifyObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.ConsoleMenu
{
    public static class MenuHelper
    {
        public static async Task<SimplePlaylist> ChoosePlaylistFromUserPlaylists(params string[] excludeIDs)
        {
            List<SimplePlaylist> userPlaylists = await SpotifyAPIManager.Instance.GetPlaylistsFromCurrentUser();
            if (excludeIDs != null)
            {
                userPlaylists = userPlaylists.Where(pl => !excludeIDs.Contains(pl.Id)).ToList();
            }
            if (!userPlaylists.Any())
            {
                return null;
            }
            if (userPlaylists.Count == 1)
            {
                return userPlaylists[0];
            }
            Console.WriteLine("Choose a playlist:");
            for (int i = 0; i < userPlaylists.Count; i++)
            {
                SimplePlaylist pl = userPlaylists[i];
                Console.WriteLine((i + 1) + ") " + StringConverter.PlaylistToString(pl));
            }
            int chosenInt = GetInt(1, userPlaylists.Count);
            return userPlaylists[chosenInt - 1];
        }

        public static async Task<string> GetArtistId()
        {
            Console.WriteLine("Write the artist id and press enter: ");
            string artistId;
            do
            {
                artistId = Console.ReadLine();
                var successfull = false;
                try
                {
                    artistId = StringConverter.GetId(artistId);
                    var artist = await SpotifyAPIManager.Instance.GetArtist(artistId);
                    successfull = artist != null && artist.Id == artistId && !String.IsNullOrWhiteSpace(artist.Uri);
                }
                catch (Exception)
                {
                }
                if (successfull)
                {
                    break;
                }
                Console.WriteLine("Please write a valid artist id:");
            } while (true);
            return artistId;
        }

        public static int GetInt(int min, int max)
        {
            int chosenInt;
            do
            {
                string chosen = Console.ReadLine();
                if (Int32.TryParse(chosen, out chosenInt) && chosenInt >= min && chosenInt <= max)
                {
                    break;
                }
                Console.WriteLine("Please write a valid number from " + min + " to " + max + ":");
            } while (true);
            return chosenInt;
        }
    }
}
