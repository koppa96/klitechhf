using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Flurl;
using OneDriveServices.Authentication.Views;
using Prism.Commands;
using Prism.Windows.Mvvm;

namespace OneDriveServices.Authentication.ViewModels
{
    /// <summary>
    /// A simple ViewModel for the Login dialog
    /// </summary>
    public class LoginDialogViewModel : ViewModelBase
    {
        private readonly string _callbackUrl, _clientId, _baseUrl, _scopes;

        public event Action<string> LoginComplete;  

        public DelegateCommand<WebView> CheckUriCommand { get; }

        public LoginDialogViewModel()
        {
            _clientId = Application.Current.Resources["ClientId"].ToString();
            _baseUrl = Application.Current.Resources["AuthEndpoint"].ToString();
            _scopes = Application.Current.Resources["Scopes"].ToString();
            _callbackUrl = Application.Current.Resources["RedirectUrl"].ToString();

            CheckUriCommand = new DelegateCommand<WebView>(OnWebViewNavigation);
        }

        /// <summary>
        /// Builds the url of the authorization endpoint and navigates the WebView of the dialog to it.
        /// </summary>
        /// <param name="dialog">The dialog</param>
        public void Initialize(ILoginDialog dialog)
        {
            var url = new Url(_baseUrl)
                .SetQueryParams(new
                {
                    client_id = _clientId,
                    scope = _scopes,
                    response_type = "code",
                    redirect_uri = _callbackUrl
                });

            dialog.NavigateWebView(url.ToUri());
        }

        /// <summary>
        /// Checks if the WebView is on the Redirect URI. If it is there it obtains the token from it.
        /// </summary>
        /// <param name="view">The WebView of the login dialog</param>
        public void OnWebViewNavigation(WebView view)
        {
            var url = new Url(view.Source.ToString());
            if (url.Path == _callbackUrl)
            {
                var authCode = url.QueryParams["code"]?.ToString();
                LoginComplete?.Invoke(authCode);
            }
        }
    }
}
