using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Flurl;
using KlitechHf.Interfaces;
using Prism.Commands;
using Prism.Windows.Mvvm;

namespace KlitechHf.ViewModels
{
    public class LoginDialogViewModel : ViewModelBase
    {
        private readonly string _callbackUrl, _clientId, _baseUrl, _scopes;
        private ILoginDialog _dialog;

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
            _dialog = dialog;

            var url = new Url(_baseUrl)
                .SetQueryParams(new
                {
                    client_id = _clientId,
                    scope = _scopes,
                    response_type = "code",
                    redirect_uri = _callbackUrl
                });

            _dialog.NavigateWebView(url.ToUri());
        }

        public void OnWebViewNavigation(WebView webView)
        {
            var url = new Url(webView.Source.ToString());
            if (url.Path == _callbackUrl)
            {
                var authCode = url.QueryParams["code"].ToString();
                _dialog.InvokeLoginComplete(authCode);
                _dialog.Hide();
            }
        }
    }
}
