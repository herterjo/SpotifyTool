using SpotifyAPI.Web;
using SpotifyTool.SpotifyObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyAPI
{
    public class PlaylistEditor
    {
        public string PlaylistID { get; }

        public PlaylistEditor(string playlistID)
        {
            this.PlaylistID = playlistID ?? throw new ArgumentNullException(nameof(playlistID));
        }

        public PlaylistEditor(SimplePlaylist playlist)
        {
            this.PlaylistID = playlist?.Id ?? throw new ArgumentNullException(nameof(playlist));
        }

        public Task Remove(string spotifyURI)
        {
            return SpotifyAPIManager.Instance.RemoveFromPlaylistWithOwnerCheck(PlaylistID, spotifyURI);
        }

        public Task Remove(FullTrack track)
        {
            return SpotifyAPIManager.Instance.RemoveFromPlaylistWithOwnerCheck(PlaylistID, track);
        }

        public Task RemoveAndUnlike(string spotifyURI)
        {
            var removeTask = SpotifyAPIManager.Instance.RemoveFromPlaylistWithOwnerCheck(PlaylistID, spotifyURI);
            var unlikeTask = SpotifyAPIManager.Instance.Unlike(StringConverter.GetId(spotifyURI));
            return Task.WhenAll(removeTask, unlikeTask);
        }

        public Task RemoveAndUnlike(FullTrack track)
        {
            var removeTask = SpotifyAPIManager.Instance.RemoveFromPlaylistWithOwnerCheck(PlaylistID, track);
            var unlikeTask = SpotifyAPIManager.Instance.Unlike(track);
            return Task.WhenAll(removeTask, unlikeTask);
        }
    }
}
