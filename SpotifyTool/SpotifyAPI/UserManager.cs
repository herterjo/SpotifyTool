﻿using Nito.AsyncEx;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyTool.Config;
using SpotifyTool.Logger;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpotifyTool.SpotifyAPI
{
    public class UserManager : ClientManager
    {
        public event Action OnLogin;

        private EmbedIOAuthServer server;
        private readonly AsyncLock userLock = new AsyncLock();

        private async Task LogInRequest()
        {
            var appIDSecretTask = ConfigManager.GetClientIDAndSecretOrFromConsole();
            Uri baseUri;
            if (this.server != null)
            {
                await this.StopServerUnsafe();
            }
            var callbackPort = await ConfigManager.GetCallbackPort();
            // Make sure "http://localhost:*callbackPort*/callback" is in your spotify application as redirect uri!
            this.server = new EmbedIOAuthServer(new Uri("http://localhost:" + callbackPort + "/callback"), callbackPort);
            await this.server.Start();
            this.server.AuthorizationCodeReceived += this.OnAuthorizationCodeReceived;
            baseUri = this.server.BaseUri;
            (string id, string _) = await appIDSecretTask;
            LoginRequest request = new LoginRequest(baseUri, id, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> {
                    Scopes.AppRemoteControl,
                    Scopes.PlaylistModifyPrivate,
                    Scopes.PlaylistModifyPublic,
                    Scopes.PlaylistReadCollaborative,
                    Scopes.PlaylistReadPrivate,
                    Scopes.Streaming,
                    Scopes.UserFollowRead,
                    Scopes.UserLibraryRead,
                    Scopes.UserModifyPlaybackState,
                    Scopes.UserReadCurrentlyPlaying,
                    Scopes.UserReadPlaybackPosition,
                    Scopes.UserReadPlaybackState,
                    Scopes.UserReadPrivate,
                    Scopes.UserReadRecentlyPlayed,
                    Scopes.UserTopRead }
            };
            Uri uri = request.ToUri();
            BrowserUtil.Open(uri);
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            var appIDSecretTask = ConfigManager.GetClientIDAndSecretOrFromConsole();
            var callbackPortTask = ConfigManager.GetCallbackPort();
            await this.StopServerUnsafe();
            await Task.WhenAll(appIDSecretTask, callbackPortTask);
            (string id, string secret) = appIDSecretTask.Result;
            var oAuthClient = new OAuthClient();
            AuthorizationCodeTokenResponse tokenResponse = await oAuthClient.RequestToken(
              new AuthorizationCodeTokenRequest(id, secret, response.Code, new Uri("http://localhost:"+ callbackPortTask.Result + "/callback"))
            );
            SpotifyClientConfig spotifyConfig = GetClientConfig()
              .WithAuthenticator(new AuthorizationCodeAuthenticator(id, secret, tokenResponse));
            SpotifyClient spotifyClient = new SpotifyClient(spotifyConfig);
            this.SetSpotifyClient(spotifyClient);
            OnLogin.Invoke();
        }

        private async Task StopServerUnsafe()
        {
            if (this.server == null)
            {
                return;
            }
            await this.server.Stop();
            this.server.Dispose();
        }

        public async Task<PrivateUser> GetUser()
        {
            using (await this.userLock.LockAsync())
            {
                return await this.GetUser(0);
            }
        }

        private async Task<PrivateUser> GetUser(int retries)
        {
            try
            {
                SpotifyClient client = await this.GetSpotifyClient();
                return await client.UserProfile.Current();
            }
            catch (Exception ex)
            {
                APIException apiEx = null;
                if (ex is AggregateException aex && aex.InnerExceptions.Count == 1 && aex.InnerExceptions[0] is APIException apiEx2)
                {
                    apiEx = apiEx2;
                }
                else if (ex is APIException apiEx3)
                {
                    apiEx = apiEx3;
                }
                if (apiEx == null || retries > 1)
                {
                    throw;
                }
            }
            TaskCompletionSource<PrivateUser> loginTaskCompletionSource = new TaskCompletionSource<PrivateUser>();
            async void onLoginCompletion()
            {
                loginTaskCompletionSource.SetResult(await this.GetUser(retries + 1));
                OnLogin -= onLoginCompletion;
            }

            OnLogin += onLoginCompletion;
            await this.LogInRequest();
            return await loginTaskCompletionSource.Task;
        }
    }
}
