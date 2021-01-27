using SpotifyAPI.Web;
using System;
using System.Linq;

namespace SpotifyTool.SpotifyObjects
{
    public static class StringConverter
    {
        public static string FullTrackToString(FullTrack fullTrack)
        {
            string restrictions = "";
            if (fullTrack.Restrictions?.Any() ?? false)
            {
                restrictions = "; Restrictions: ";
                restrictions += String.Join(",", fullTrack.Restrictions.Select(kv => "{" + kv.Key + ":" + kv.Value + "}"));
            }
            return fullTrack.Name + " { " + String.Join(", ", fullTrack.Artists.Select(a => a.Name)) + restrictions + "; ID: " + fullTrack.Id + "}";
        }

        public static string AllTracksToString(string seperator, params FullTrack[] fullTracks)
        {
            return String.Join(seperator, fullTracks.Select(FullTrackToString));
        }

        public static string PlaylistToString(SimplePlaylist pl)
        {
            return pl.Name + "{" + pl.Id + "}";
        }
    }
}
