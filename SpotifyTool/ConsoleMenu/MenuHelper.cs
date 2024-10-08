﻿using SpotifyAPI.Web;
using SpotifyTool.Logger;
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
        public static Task<FullPlaylist> ChoosePlaylistFromUserPlaylists(params string[] excludeIDs)
        {
            return ChoosePlaylistFromUserPlaylists(false, excludeIDs);
        }

        private static async Task<FullPlaylist> ChoosePlaylistFromUserPlaylists(bool canBreak, params string[] excludeIDs)
        {
            List<FullPlaylist> userPlaylists = await SpotifyAPIManager.Instance.GetPlaylistsFromCurrentUser();
            if (excludeIDs != null)
            {
                userPlaylists = userPlaylists.Where(pl => !excludeIDs.Contains(pl.Id)).ToList();
            }
            if (!userPlaylists.Any())
            {
                return null;
            }
            if (userPlaylists.Count == 1 && !canBreak)
            {
                return userPlaylists[0];
            }
            Console.WriteLine("Choose a playlist:");
            for (int i = 0; i < userPlaylists.Count; i++)
            {
                FullPlaylist pl = userPlaylists[i];
                Console.WriteLine((i + 1) + ") " + StringConverter.PlaylistToString(pl));
            }

            if (canBreak)
            {
                Console.WriteLine((userPlaylists.Count + 1) + ") --- No selection");
            }

            int chosenInt = GetInt(1, userPlaylists.Count + (canBreak ? 0 : 1));

            if (canBreak && chosenInt > userPlaylists.Count)
            {
                return null;
            }

            return userPlaylists[chosenInt - 1];
        }

        public static async Task<List<FullPlaylist>> ChoosePlaylistsFromUserPlaylists(params string[] excludeIDs)
        {
            List<FullPlaylist> chosenPlaylists = new();
            FullPlaylist chosenPlaylist;
            bool firstSpin = true;
            do
            {
                chosenPlaylist = await ChoosePlaylistFromUserPlaylists(!firstSpin, chosenPlaylists.Select(pl => pl.Id).ToArray());
                firstSpin = false;
                if (chosenPlaylist == null)
                {
                    break;
                }
                else
                {
                    chosenPlaylists.Add(chosenPlaylist);
                } 
            } while (true);
            return chosenPlaylists;
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

        public static async Task Search(Func<string, Task<List<FullTrack>>> getFullTracks)
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

        public static async Task<List<string>> GetTrackUris(string name, LogFileManager logFileManager)
        {
            await logFileManager.WriteToLogAndConsole("\n");
            if (!String.IsNullOrWhiteSpace(name))
            {
                await logFileManager.WriteToLogAndConsole("Please write the Spotify URIs for command \"" + name + "\" (seperated by spaces):");
            }
            List<string> uris = Console.ReadLine().Split(" ").Where(s => !String.IsNullOrWhiteSpace(s)).Select(s => StringConverter.GetUri(s, SpotifyObjectTypes.track)).ToList();
            await logFileManager.WriteToLog(String.Join(" ", uris));
            return uris;
        }
    }
}
