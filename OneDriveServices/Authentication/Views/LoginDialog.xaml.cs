using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using OneDriveServices.Authentication.ViewModels;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace OneDriveServices.Authentication.Views
{
    public sealed partial class LoginDialog : ContentDialog, ILoginDialog
    {
        public string AuthCode { get; set; }

        public LoginDialog()
        {
            this.InitializeComponent();
            if (DataContext is LoginDialogViewModel viewModel)
            {
                // Navigates the WebView to the authorization endpoint's URL
                viewModel.Initialize(this);
                viewModel.LoginComplete += OnLoginComplete;
            }
        }

        /// <summary>
        /// Called when the login is complete. Hides the dialog and clears the cache of the WebView so it won't log in automatically next time.
        /// </summary>
        /// <param name="authCode">The authorization code obtained with the login</param>
        private async void OnLoginComplete(string authCode)
        {
            AuthCode = authCode;
            await WebView.ClearTemporaryWebDataAsync();
            Hide();
        }

        public void NavigateWebView(Uri uri)
        {
            LoginWebView.Navigate(uri);
        }
    }
}
