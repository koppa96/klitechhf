using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using Flurl;
using KlitechProba.Interfaces;
using Prism.Commands;

namespace KlitechProba.ViewModels
{
    public class LoginViewModel
    {
        private string clientId, callbackUrl;
        private ILoginDialog dialog;

        public DelegateCommand<WebView> CheckUriCommand { get; }

        public LoginViewModel(ILoginDialog dialog)
        {
            this.dialog = dialog;

            clientId = App.Current.Resources["ClientId"].ToString();
            callbackUrl = App.Current.Resources["RedirectUrl"].ToString();
            string[] scopes = { "offline_access", "Files.ReadWrite.All", "User.Read" };

            var url = new Url("https://login.microsoftonline.com")
                .AppendPathSegments("common", "oauth2", "v2.0", "authorize")
                .SetQueryParams(new
                {
                    client_id = clientId,
                    scope = $"{scopes[0]} {scopes[1]} {scopes[2]}",
                    response_type = "code",
                    redirect_uri = callbackUrl
                });

            this.dialog.NavigateWebView(url.ToUri());
            CheckUriCommand = new DelegateCommand<WebView>(OnWebViewNavigation, u => true);
        }

        public void OnWebViewNavigation(WebView webView)
        {
            var url = new Url(webView.Source.ToString());
            if (url.Path == callbackUrl)
            {
                var authCode = url.QueryParams["code"].ToString();
                dialog.InvokeLoginComplete(authCode);
                dialog.Hide();
            }
        }
    }
}
