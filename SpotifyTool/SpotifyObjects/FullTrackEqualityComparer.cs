using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotifyTool.SpotifyObjects
{
    public class FullTrackEqualityComparer : IEqualityComparer<FullTrack>
    {
        public static FullTrackEqualityComparer Instance { get; } = new FullTrackEqualityComparer();
        public bool Equals(FullTrack x, FullTrack y)
        {
            return x.Uri == y.Uri;
        }

        public int GetHashCode(FullTrack obj)
        {
            return obj.Uri.GetHashCode();
        }
    }
}
