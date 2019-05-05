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
using OneDriveServices.Drive;

namespace OneDriveServices.Authentication
{
    /// <summary>
    /// A Singleton service for handling user logins and tokens.
    /// </summary>
    public class AuthService
    {
        private const string TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        private string _refreshToken, _accessToken, _scheme;
        private readonly string _clientId, _callbackUrl;
        private readonly ApplicationDataContainer _container;

        private static AuthService _instance;
        public static AuthService Instance => _instance ?? (_instance = new AuthService());
        public User CurrentUser { get; private set; }

        public event Action UserLoggedOut; 

        protected AuthService()
        {
            _clientId = Application.Current.Resources["ClientId"].ToString();
            _callbackUrl = Application.Current.Resources["RedirectUrl"].ToString();

            _container = ApplicationData.Current.LocalSettings;
            _refreshToken = _container.Values["refresh_token"]?.ToString();
        }

        /// <summary>
        /// Tries to get access token with the locally stored refresh token. If it fails it shows a login dialog.
        /// </summary>
        /// <returns>A task representing the operation</returns>
        public async Task LoginAsync()
        {
            if (_refreshToken != null)
            {
                await RefreshTokensAsync();

                if (_accessToken == null)
                {
                    await ShowLoginDialogAsync();
                }
            }
            else
            {
                await ShowLoginDialogAsync();
            }
        }

        /// <summary>
        /// Logs the current user out. Drops all the tokens and resets the DriveService.
        /// </summary>
        public void Logout()
        {
            CurrentUser = null;
            _accessToken = null;
            _refreshToken = null;
            _container.Values["refresh_token"] = null;
            
            UserLoggedOut?.Invoke();
        }

        /// <summary>
        /// Creates a new authentication header value with the current access token and authorization scheme.
        /// </summary>
        /// <returns>The authentication header</returns>
        public AuthenticationHeaderValue CreateAuthenticationHeader()
        {
            return new AuthenticationHeaderValue(_scheme, _accessToken);
        }

        /// <summary>
        /// Shows and awaits the closing of the Login dialog.
        /// </summary>
        /// <returns></returns>
        public async Task ShowLoginDialogAsync()
        {
            LoginDialog loginDialog;

            // Creates a new LoginDialog if the user clicks the back button on the displayed login form. 
            do
            {
                loginDialog = new LoginDialog();
                await loginDialog.ShowAsync();
            } while (string.IsNullOrEmpty(loginDialog.AuthCode));

            await GetAccessTokenFromLoginAsync(loginDialog.AuthCode);
        }

        /// <summary>
        /// Gets a new access and refresh token with the current refresh token.
        /// </summary>
        /// <returns>A task representing the operation</returns>
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

        /// <summary>
        /// Gets an access and a refresh token with the authorization code obtained through the login dialog.
        /// </summary>
        /// <param name="authCode">The authorization code</param>
        /// <exception cref="ArgumentException">Thrown when the token could not be obtained with the given authorization code.</exception>
        /// <returns>A task representing the operation</returns>
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
                else
                {
                    throw new ArgumentException("The given authorization code is invalid.");
                }
            }
        }

        /// <summary>
        /// Parses the token response from the server and saves the tokens.
        /// </summary>
        /// <param name="response">The JSON representation of the token response.</param>
        private void ParseTokenResponse(string response)
        {
            var tokens = JObject.Parse(response);

            _accessToken = tokens["access_token"].ToString();
            _scheme = tokens["token_type"].ToString();
            _refreshToken = tokens["refresh_token"].ToString();

            _container.Values["refresh_token"] = _refreshToken;
        }

        /// <summary>
        /// Downloads information about the current user, and fills the CurrentUser property with it.
        /// </summary>
        /// <returns>A task representing the operation</returns>
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
