using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpotifyTool.SpotifyObjects
{
    public class TrackSubset
    {
        public string Id { get; set; }
        public string Uri { get; set; }
        public HashSet<string> ArtistIds { get; set; }
        public string LowerName { get; set; }

        public TrackSubset(FullTrack fullTrack)
        {
            this.Id = fullTrack.Id;
            this.Uri = fullTrack.Uri;
            this.ArtistIds = new HashSet<string>(fullTrack.Artists.Select(a => a.Id));
            this.LowerName = fullTrack.Name.ToLower();
        }

        public bool CompareForSameTrack(TrackSubset otherTrack)
        {
            return this.LowerName.StartsWith(otherTrack.LowerName) && this.ArtistIds.Any(a => otherTrack.ArtistIds.Contains(a));
        }

        public bool HasSamePropsBinarySearch(TrackSubset[] arr)
        {
            return IsInArray(arr, 0, arr.Length - 1);
        }

        //Base binary search copied from https://github.com/Microsoft/referencesource/blob/master/mscorlib/system/array.cs
        private bool IsInArray(TrackSubset[] arr, int index, int max)
        {
            int lo = index;
            int hi = max;
            while (lo <= hi)
            {
                // i might overflow if lo and hi are both large positive numbers. 
                int i = GetMedian(lo, hi);

                var arrElem = arr[i];
                if (this.Id == arrElem.Id || this.Uri == arrElem.Uri)
                {
                    return IsInArrayCascading(arr, lo, hi, i);
                }
                if (this.CompareForSameTrack(arrElem))
                {
                    return true;
                }

                int c = arrElem.LowerName.CompareTo(this.LowerName);
                if (c == 0)
                {
                    return IsInArrayCascading(arr, lo, hi, i);
                }
                if (c < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }
            return false;
        }

        private bool IsInArrayCascading(TrackSubset[] arr, int lo, int hi, int index)
        {
            return IsInArray(arr, lo, index - 1) || IsInArray(arr, index + 1, hi);
        }

        private static int GetMedian(int low, int hi)
        {
            // Note both may be negative, if we are dealing with arrays w/ negative lower bounds.
            return low + ((hi - low) >> 1);
        }
    }
}
