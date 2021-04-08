using System;
using System.Collections.Generic;
using System.Text;

namespace SpotifyTool.SpotifyObjects
{
    public class FullPlaylistTrackEqualityComparerByTrackInfo : IEqualityComparer<FullPlaylistTrack>
    {
        public static FullPlaylistTrackEqualityComparerByTrackInfo Instance { get; } = new FullPlaylistTrackEqualityComparerByTrackInfo();
        public bool Equals(FullPlaylistTrack x, FullPlaylistTrack y)
        {
            return FullTrackEqualityComparer.Instance.Equals(x.TrackInfo, y.TrackInfo);
        }

        public int GetHashCode(FullPlaylistTrack obj)
        {
            return FullTrackEqualityComparer.Instance.GetHashCode(obj.TrackInfo);
        }
    }
}
