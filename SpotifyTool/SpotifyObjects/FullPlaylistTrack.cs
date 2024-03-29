﻿using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpotifyTool.SpotifyObjects
{
    public class FullPlaylistTrack
    {
        public PlaylistTrack<IPlayableItem> PlaylistInfo { get; }
        public FullTrack TrackInfo { get; }

        public FullPlaylistTrack(PlaylistTrack<IPlayableItem> playlistInfo, FullTrack trackInfo)
        {
            this.PlaylistInfo = playlistInfo;
            this.TrackInfo = trackInfo;
        }

        private FullPlaylistTrack()
        {
        }
    }
}
