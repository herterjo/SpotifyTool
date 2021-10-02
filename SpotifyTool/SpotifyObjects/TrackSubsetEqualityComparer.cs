using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotifyTool.SpotifyObjects
{
    public class TrackSubsetEqualityComparer : IEqualityComparer<TrackSubset>
    {
        public static TrackSubsetEqualityComparer Instance { get; } = new TrackSubsetEqualityComparer();
        public bool Equals(TrackSubset x, TrackSubset y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(TrackSubset obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
