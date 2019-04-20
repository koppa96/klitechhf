using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using KlitechProba.Views;
using Newtonsoft.Json.Linq;

namespace KlitechProba.Services
{
    public class AuthService
    {
        private const string TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        private string refreshToken;
        private readonly string clientId, callbackUrl;

        public static AuthService Instance { get; } = new AuthService();
        public string AccessToken { get; set; }

        protected AuthService()
        {
            clientId = Application.Current.Resources["ClientId"].ToString();
            callbackUrl = Application.Current.Resources["RedirectUrl"].ToString();
        }

        public async Task ShowLoginDialogAsync()
        {
            var loginDialog = new LoginDialog();
            loginDialog.LoginComplete += GetAccessTokenFromLoginAsync;

            await loginDialog.ShowAsync();
        }

        public async Task RefreshTokensAsync()
        {
            if (refreshToken == null)
            {
                throw new InvalidOperationException("There is no refresh token to use.");
            }

            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("redirect_uri", callbackUrl),
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                    new KeyValuePair<string, string>("grant_type", "refresh_token")
                });

                var result = await client.PostAsync(TokenEndpoint, content);
                if (result.IsSuccessStatusCode)
                {
                    var json = await result.Content.ReadAsStringAsync();
                    ParseTokenResponse(json);
                }
            }
        }

        private async Task GetAccessTokenFromLoginAsync(string authCode)
        {
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new []
                {
                    new KeyValuePair<string, string>("client_id", clientId), 
                    new KeyValuePair<string, string>("redirect_uri", callbackUrl), 
                    new KeyValuePair<string, string>("code", authCode),
                    new KeyValuePair<string, string>("grant_type", "authorization_code")
                });

                var result = await client.PostAsync(TokenEndpoint, content);
                if (result.IsSuccessStatusCode)
                {
                    var json = await result.Content.ReadAsStringAsync();
                    ParseTokenResponse(json);
                }
            }
        }

        private void ParseTokenResponse(string response)
        {
            var tokens = JObject.Parse(response);

            AccessToken = tokens["access_token"].ToString();
            refreshToken = tokens["refresh_token"].ToString();
        }
    }
}
