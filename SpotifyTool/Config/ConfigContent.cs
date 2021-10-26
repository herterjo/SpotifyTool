using System;
using System.Collections.Generic;
using System.Text;

namespace SpotifyTool.Config
{
    public class ConfigContent
    {
        public const int DefaultCallbackPort = 5000;

        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string MainPlaylistID { get; set; }
        public string OneArtistPlaylistID { get; set; }
        public int CallbackPort { get; set; } = DefaultCallbackPort;
    }
}
