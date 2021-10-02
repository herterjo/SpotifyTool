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

        public TrackSubset(SimpleTrack simpleTrack)
        {
            this.Id = simpleTrack.Id;
            this.Uri = simpleTrack.Uri;
            this.ArtistIds = new HashSet<string>(simpleTrack.Artists.Select(a => a.Id));
            this.LowerName = simpleTrack.Name.ToLower();
        }

        public bool CompareForSameTrack(TrackSubset otherTrack)
        {
            return this.LowerName.StartsWith(otherTrack.LowerName) && this.ArtistIds.Any(a => otherTrack.ArtistIds.Contains(a));
        }

        public int SamePropsBinaryFind(TrackSubset[] arr, int index = 0, int max = -1)
        {
            if (max < 0)
            {
                max = arr.Length - 1;
            }
            if (index < 0 || index > max || index >= arr.Length)
            {
                throw new ArgumentException("index " + index + " is invalid fot array with length " + arr.Length + " and max search index " + max);
            }
            if(max >= arr.Length)
            {
                throw new ArgumentException("max " + max + " is invalid fot array with length " + arr.Length);
            }
            return FindInArray(arr, index, max);
        }

        //Base binary search copied from https://github.com/Microsoft/referencesource/blob/master/mscorlib/system/array.cs
        public int FindInArray(TrackSubset[] arr, int index, int max)
        {
            if (arr.Length < 1)
            {
                return -1;
            }
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
                    return i;
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
            return -1;
        }

        private int IsInArrayCascading(TrackSubset[] arr, int lo, int hi, int index)
        {
            var partialResult = FindInArray(arr, lo, index - 1);
            if (partialResult >= 0) {
                return partialResult;
            }
            partialResult = FindInArray(arr, index + 1, hi);
            if (partialResult >= 0)
            {
                return partialResult;
            }
            return -1;
        }

        private static int GetMedian(int low, int hi)
        {
            // Note both may be negative, if we are dealing with arrays w/ negative lower bounds.
            return low + ((hi - low) >> 1);
        }
    }
}
