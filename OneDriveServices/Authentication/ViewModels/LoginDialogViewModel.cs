using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Flurl;
using OneDriveServices.Authentication.Views;
using Prism.Commands;
using Prism.Windows.Mvvm;

namespace OneDriveServices.Authentication.ViewModels
{
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

        public void OnWebViewNavigation(WebView view)
        {
            var url = new Url(view.Source.ToString());
            if (url.Path == _callbackUrl)
            {
                var authCode = url.QueryParams["code"].ToString();
                LoginComplete?.Invoke(authCode);
            }
        }
    }
}
