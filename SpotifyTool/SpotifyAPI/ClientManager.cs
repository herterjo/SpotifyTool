using Nito.AsyncEx;
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
        private readonly AsyncReaderWriterLock clientLock = new AsyncReaderWriterLock();

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
            using (await this.clientLock.ReaderLockAsync())
            {
                if (this.SpotifyClient != null)
                {
                    return this.SpotifyClient;
                }
            }
            using (await this.clientLock.WriterLockAsync())
            {
                if (this.SpotifyClient != null)
                {
                    return this.SpotifyClient;
                }
                (string clientId, string secret) = await ConfigManager.GetClientIDAndSecretOrFromConsole();
                SpotifyClientConfig clientConfig = SpotifyClientConfig
                    .CreateDefault()
                    .WithAuthenticator(new ClientCredentialsAuthenticator(clientId, secret));
                SpotifyClient client = new SpotifyClient(clientConfig);
                this.SetSpotifyClientUnsafe(client);
                return this.SpotifyClient;
            }
        }

        private void SetSpotifyClientUnsafe(SpotifyClient client)
        {
            this.SpotifyClient = client ?? throw new ArgumentNullException(nameof(client));
            AfterClientChange?.Invoke();
        }

        protected async void SetSpotifyClient(SpotifyClient client)
        {
            using (await this.clientLock.WriterLockAsync())
            {
                this.SetSpotifyClientUnsafe(client);
            }
        }
    }
}
