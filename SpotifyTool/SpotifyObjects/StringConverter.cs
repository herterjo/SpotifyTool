using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpotifyTool.SpotifyObjects
{
    public static class StringConverter
    {
        private static readonly Regex SpotifyIdRegex = new("^([1-9]|[a-z]|[A-Z]){22}$");
        private static readonly string SpotifyType = "("
            + String.Join("|", Enum.GetValues(typeof(SpotifyObjectTypes)).Cast<SpotifyObjectTypes>().Select(sot => sot.ToString()))
            + ")";
        private static readonly Regex SpotifyUriRegex = new("^spotify:" + SpotifyType + ":([1-9]|[a-z]|[A-Z]){22}$");

        public static string FullTrackToString(FullTrack fullTrack, string additionalInfo = "")
        {
            string restrictions = "";
            if (fullTrack.Restrictions?.Any() ?? false)
            {
                restrictions = "; Restrictions: ";
                restrictions += String.Join(",", fullTrack.Restrictions.Select(kv => "{" + kv.Key + ":" + kv.Value + "}"));
            }
            return fullTrack.Name + " { " + String.Join(", ", fullTrack.Artists.Select(a => a.Name)) + restrictions + "; ID: " + fullTrack.Id + additionalInfo + " }";
        }

        public static string AllTracksToString(string seperator, params FullTrack[] fullTracks)
        {
            return String.Join(seperator, fullTracks.Select(ft => FullTrackToString(ft)));
        }

        public static string AllPlalistTracksToString(string seperator, params FullPlaylistTrack[] fullPlaylistTracks)
        {
            return String.Join(seperator, fullPlaylistTracks.Select(fpt => FullPlaylistTrackToString(fpt)));
        }

        public static string FullPlaylistTrackToString(FullPlaylistTrack fullPlaylistTrack)
        {
            return FullTrackToString(fullPlaylistTrack.TrackInfo, "; Added: " + fullPlaylistTrack.PlaylistInfo.AddedAt + "; IsLocal: " + fullPlaylistTrack.TrackInfo.IsLocal);
        }

        public static string PlaylistToString(FullPlaylist pl)
        {
            return pl.Name + " {" + pl.Id + "}";
        }

        public static string GetId(string spotifyUriOrUrl)
        {
            if (IsSpotifyUri(spotifyUriOrUrl))
            {
                return GetIdFromSpotifyUri(spotifyUriOrUrl);
            }
            else if (IsSpotifyUrl(spotifyUriOrUrl))
            {
                return GetIdFromSpotifyUrl(spotifyUriOrUrl);
            }
            else if (IsSpotifyId(spotifyUriOrUrl))
            {
                return spotifyUriOrUrl;
            }
            throw new FormatException("Format of " + spotifyUriOrUrl + " not recognized as spotify id, uri or url");
        }

        private static bool IsSpotifyUri(string spotifyUri)
        {
            return SpotifyUriRegex.IsMatch(spotifyUri);
        }

        public static bool IsSpotifyUrl(string spotifyUrl)
        {
            return spotifyUrl.StartsWith("https://open.spotify.com/") || spotifyUrl.StartsWith("http://open.spotify.com/");
        }

        public static bool IsSpotifyId(string spotifyId)
        {
            return SpotifyIdRegex.IsMatch(spotifyId);
        }

        public static string GetIdFromSpotifyUrl(string spotifyUrl)
        {
            return spotifyUrl.Split("/").Last().Split("?").First();
        }

        public static string GetIdFromSpotifyUri(string spotifyUri)
        {
            return spotifyUri.Split(":").Last();
        }

        public static string GetUri(string spotifyIdOrUrl, SpotifyObjectTypes type)
        {
            if (IsSpotifyId(spotifyIdOrUrl))
            {
                return GetUriFromSpotifyId(spotifyIdOrUrl, type);
            }
            else if (IsSpotifyUrl(spotifyIdOrUrl))
            {
                return GetUriFromSpotifyUrl(spotifyIdOrUrl, type);
            }
            else if (IsSpotifyUri(spotifyIdOrUrl))
            {
                var id = GetIdFromSpotifyUri(spotifyIdOrUrl);
                return GetUriFromSpotifyId(id, type);
            }
            throw new FormatException("Format of " + spotifyIdOrUrl + " not recognized as spotify id, uri or url");
        }

        public static string GetUriFromSpotifyId(string spotifyId, SpotifyObjectTypes type)
        {
            return "spotify:" + type.ToString() + ":" + spotifyId;
        }

        public static string GetUriFromSpotifyUrl(string spotifyUrl, SpotifyObjectTypes type)
        {
            string id = GetIdFromSpotifyUrl(spotifyUrl);
            return "spotify:" + type.ToString() + ":" + id;
        }

        public static string GetPrintablePlaylistIds(IEnumerable<string> playlistIds, IEnumerable<FullPlaylist> playlists)
        {
            if (playlistIds == null)
            {
                return GetPrintablePlaylistIds(playlists);
            }
            else
            {
                return GetPrintablePlaylistIds(playlistIds);
            }
        }

        public static string GetPrintablePlaylistIds(IEnumerable<string> playlistIds)
        {
            return String.Join(", ", playlistIds);
        }

        public static string GetPrintablePlaylistIds(IEnumerable<FullPlaylist> playlists)
        {
            return GetPrintablePlaylistIds(playlists.Select(pl => PlaylistToString(pl)));
        }
    }
}
