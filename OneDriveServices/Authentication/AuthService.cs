using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneDriveServices.Authentication.Model;
using OneDriveServices.Authentication.Views;

namespace OneDriveServices.Authentication
{
    public class AuthService
    {
        private const string TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        private string _refreshToken;
        private readonly string _clientId, _callbackUrl;
        private readonly ApplicationDataContainer _container;

        public static AuthService Instance { get; } = new AuthService();
        public string AccessToken { get; set; }
        public string Scheme { get; set; }
        public User CurrentUser { get; private set; }

        protected AuthService()
        {
            _clientId = Application.Current.Resources["ClientId"].ToString();
            _callbackUrl = Application.Current.Resources["RedirectUrl"].ToString();

            _container = ApplicationData.Current.LocalSettings;
            _refreshToken = _container.Values["refresh_token"]?.ToString();
        }

        public async Task LoginAsync()
        {
            if (_refreshToken != null)
            {
                await RefreshTokensAsync();

                if (AccessToken == null)
                {
                    await ShowLoginDialogAsync();
                }
            }
            else
            {
                await ShowLoginDialogAsync();
            }
        }

        public void Logout()
        {
            CurrentUser = null;
            AccessToken = null;
            _refreshToken = null;
            _container.Values["refresh_token"] = null;
        }

        public AuthenticationHeaderValue CreateAuthenticationHeader()
        {
            return new AuthenticationHeaderValue(Scheme, AccessToken);
        }

        public async Task ShowLoginDialogAsync()
        {
            var loginDialog = new LoginDialog();
            await loginDialog.ShowAsync();
            await GetAccessTokenFromLoginAsync(loginDialog.AuthCode);
        }

        public async Task RefreshTokensAsync()
        {
            if (_refreshToken == null)
            {
                throw new InvalidOperationException("There is no refresh token to use.");
            }

            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("redirect_uri", _callbackUrl),
                    new KeyValuePair<string, string>("refresh_token", _refreshToken),
                    new KeyValuePair<string, string>("grant_type", "refresh_token")
                });

                var result = await Task.Run(() => client.PostAsync(TokenEndpoint, content));
                if (result.IsSuccessStatusCode)
                {
                    var json = await result.Content.ReadAsStringAsync();
                    ParseTokenResponse(json);
                    await GetUserInfoAsync();
                }
            }
        }

        private async Task GetAccessTokenFromLoginAsync(string authCode)
        {
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new []
                {
                    new KeyValuePair<string, string>("client_id", _clientId), 
                    new KeyValuePair<string, string>("redirect_uri", _callbackUrl), 
                    new KeyValuePair<string, string>("code", authCode),
                    new KeyValuePair<string, string>("grant_type", "authorization_code")
                });

                var result = await Task.Run(() => client.PostAsync(TokenEndpoint, content));
                if (result.IsSuccessStatusCode)
                {
                    var json = await result.Content.ReadAsStringAsync();
                    ParseTokenResponse(json);
                    await GetUserInfoAsync();
                } 
            }
        }

        private void ParseTokenResponse(string response)
        {
            var tokens = JObject.Parse(response);

            AccessToken = tokens["access_token"].ToString();
            Scheme = tokens["token_type"].ToString();
            _refreshToken = tokens["refresh_token"].ToString();

            _container.Values["refresh_token"] = _refreshToken;
        }

        private async Task GetUserInfoAsync()
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
                request.Headers.Authorization = CreateAuthenticationHeader();

                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    CurrentUser = JsonConvert.DeserializeObject<User>(json);
                }
            }
        }
    }
}
