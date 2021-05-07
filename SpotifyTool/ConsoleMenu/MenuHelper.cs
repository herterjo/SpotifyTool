using SpotifyAPI.Web;
using SpotifyTool.SpotifyAPI;
using SpotifyTool.SpotifyObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            uint chosenInt;
            do
            {
                string chosen = Console.ReadLine();
                if (UInt32.TryParse(chosen, out chosenInt) && chosenInt > 0 && chosenInt < userPlaylists.Count + 1)
                {
                    break;
                }
                Console.WriteLine("Please write a valid number");
            } while (true);
            return userPlaylists[(int)(chosenInt - 1)];
        }
    }
}
