﻿using SpotifyAPI.Web;
using SpotifyTool.Config;
using SpotifyTool.SpotifyAPI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyObjects
{
    public static class LibraryManager
    {
        public const string LibraryFileEnding = ".library.json";

        private static string GetLibraryFileName(PrivateUser user)
        {
            return GetLibraryFileName(user.Id);
        }

        private static string GetLibraryFileName(string userID)
        {
            return userID + LibraryFileEnding;
        }

        public static List<FullTrack> GetFullTracks(List<SavedTrack> savedTracks)
        {
            return savedTracks.Select(st => st.Track).ToList();
        }

        public static async Task<List<SavedTrack>> RefreshLibraryTracksForCurrentUser()
        {
            SpotifyAPIManager clientManager = SpotifyAPIManager.Instance;
            PrivateUser user = await clientManager.GetUser();
            return await RefreshLibraryTracksForCurrentUser(user);
        }

        private static async Task<List<SavedTrack>> RefreshLibraryTracksForCurrentUser(PrivateUser user)
        {
            SpotifyAPIManager clientManager = SpotifyAPIManager.Instance;
            IList<SavedTrack> savedTracks = await clientManager.GetLikedTracks();
            List<SavedTrack> savedTracksList = savedTracks.ToList();
            //string tracksAsJSON = JsonConvert.SerializeObject(savedTracksList);
            string path = GetLibraryFileName(user);
            await Serialization.SerializeJson(savedTracksList, path, false);
            //await File.WriteAllTextAsync(path, tracksAsJSON);
            return savedTracksList;
        }

        public static async Task<List<SavedTrack>> GetLibraryTracksForCurrentUser()
        {
            SpotifyAPIManager clientManager = SpotifyAPIManager.Instance;
            PrivateUser user = await clientManager.GetUser();
            return await GetLibraryTracksForCurrentUser(user);
        }

        private static Task<List<SavedTrack>> GetLibraryTracksForCurrentUser(PrivateUser user)
        {
            string fn = GetLibraryFileName(user);
            if (File.Exists(fn))
            {
                //string content = await File.ReadAllTextAsync(fn);
                return Serialization.DeserializeJson<List<SavedTrack>>(fn, false);
                //return JsonConvert.DeserializeObject<List<SavedTrack>>(content);
            }
            return RefreshLibraryTracksForCurrentUser();
        }

        public static async Task RefreshLibraryTracksIfCached()
        {
            SpotifyAPIManager clientManager = SpotifyAPIManager.Instance;
            PrivateUser user = await clientManager.GetUser();
            string fn = GetLibraryFileName(user);
            if (!File.Exists(fn))
            {
                return;
            }
            await RefreshLibraryTracksForCurrentUser(user);
        }

        public static Task UnlikeTracks(List<FullTrack> tracks)
        {
            SpotifyAPIManager clientManager = SpotifyAPIManager.Instance;
            List<string> trackIds = tracks.Select(t => t.Id).ToList();
            return clientManager.UnlikeTracks(trackIds);
        }

        public static Task LikeTracks(List<FullTrack> tracks)
        {
            SpotifyAPIManager clientManager = SpotifyAPIManager.Instance;
            List<string> trackIds = tracks.Select(t => t.Id).ToList();
            return clientManager.LikeTracks(trackIds);
        }

        public static Task UnlikeTracks(List<string> spotifyUri)
        {
            SpotifyAPIManager clientManager = SpotifyAPIManager.Instance;
            List<string> trackIds = spotifyUri.Select(uri => StringConverter.GetId(uri)).ToList();
            return clientManager.UnlikeTracks(trackIds);
        }

        public static Task LikeTracks(List<string> spotifyUri)
        {
            SpotifyAPIManager clientManager = SpotifyAPIManager.Instance;
            List<string> trackIds = spotifyUri.Select(uri => StringConverter.GetId(uri)).ToList();
            return clientManager.LikeTracks(trackIds);
        }
    }
}
