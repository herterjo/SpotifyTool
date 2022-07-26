using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SpotifyTool.Config
{
    public static class ConfigManager
    {
        public const string ConfigFileName = "config.json";
        private static async Task<ConfigContent> Read()
        {
            if (!File.Exists(ConfigFileName))
            {
                ConfigContent newConfig = new ConfigContent();
                await Write(newConfig);
                return newConfig;
            }
            return await Serialization.DeserializeJson<ConfigContent>(ConfigFileName);
        }

        public static Task Write(ConfigContent config)
        {
            return Serialization.SerializeJson(config, ConfigFileName);
        }

        public static async Task<int> GetCallbackPort()
        {
            if (!File.Exists(ConfigFileName))
            {
                return ConfigContent.DefaultCallbackPort;
            }
            var config = await Read();
            return config.CallbackPort;
        }

        private static async Task<string> GetFromConfigOrConsole(Func<ConfigContent, string> configGetter, string printLn, ConfigContent appConfig = null)
        {
            if (appConfig == null)
            {
                appConfig = await Read();
            }
            string result = configGetter(appConfig);
            if (!String.IsNullOrEmpty(result))
            {
                return result;
            }
            Console.WriteLine(printLn);
            while (true)
            {
                result = Console.ReadLine();
                if (!String.IsNullOrWhiteSpace(result))
                {
                    break;
                }
                Console.WriteLine("Please write something:");
            }
            return result;
        }

        //public static Task<string> GetMainPlaylistID()
        //{
        //    return GetFromConfigOrConsole(c => c.MainPlaylistID, "Please enter the Spotify ID of your main playlist:");
        //}

        //public static Task<string> GetOneArtistPlaylistID()
        //{
        //    return GetFromConfigOrConsole(c => c.OneArtistPlaylistID, "Please enter the Spotify ID of your playlist where every artist is present only once:");
        //}

        public static async Task<(string ClientId, string Secret)> GetClientIDAndSecretOrFromConsole()
        {
            ConfigContent appConfig = await Read();
            string clientID = await GetFromConfigOrConsole(c => c.ClientID, "Please enter your apps client ID", appConfig);
            string clientSecret = await GetFromConfigOrConsole(c => c.ClientSecret, "Please enter your apps client secret", appConfig);
            return (clientID, clientSecret);
        }

        public static async Task<(string MainId, string OneArtistId)> GetMainAndOneArtistPlaylistID()
        {
            ConfigContent appConfig = await Read();
            return (appConfig.MainPlaylistID, appConfig.OneArtistPlaylistID);
        }
    }
}
