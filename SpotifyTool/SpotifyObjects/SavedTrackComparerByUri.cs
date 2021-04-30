using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotifyTool.SpotifyObjects
{
    public class SavedTrackComparerByUri : IEqualityComparer<SavedTrack>
    {
        public readonly static SavedTrackComparerByUri Instance = new SavedTrackComparerByUri();
        public int GetHashCode(SavedTrack obj)
        {
            return obj.Track.Uri.GetHashCode();
        }

        public bool Equals(SavedTrack x, SavedTrack y)
        {
            return x.Track.Uri == y.Track.Uri;
        }
    }
}
