using SpotifyAPI.Web;
using SpotifyTool.Config;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyAPI
{
    public class ClientManager
    {
        public static event Action AfterClientChange;

        private static ClientManager _Instance = null;
        public static ClientManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new ClientManager();
                }
                return _Instance;
            }
        }

        private SpotifyClient SpotifyClient { get; set; }

        protected ClientManager()
        {
        }

        public async Task<SpotifyClient> GetSpotifyClient()
        {
            if (this.SpotifyClient == null)
            {
                KeyValuePair<string, string> clientIDAndSecret = await ConfigManager.GetClientIDAndSecretOrFromConsole();
                SpotifyClientConfig clientConfig = SpotifyClientConfig
                    .CreateDefault()
                    .WithAuthenticator(new ClientCredentialsAuthenticator(clientIDAndSecret.Key, clientIDAndSecret.Value));
                SpotifyClient client = new SpotifyClient(clientConfig);
                this.SetSpotifyClient(client);
            }
            return this.SpotifyClient;
        }

        protected void SetSpotifyClient(SpotifyClient client)
        {
            this.SpotifyClient = client ?? throw new ArgumentNullException(nameof(client));
            AfterClientChange?.Invoke();
        }
    }
}
