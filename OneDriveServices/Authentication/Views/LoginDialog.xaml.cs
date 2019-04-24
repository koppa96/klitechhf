using System;
using System.Threading.Tasks;
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
                viewModel.Initialize(this);
                viewModel.LoginComplete += OnLoginComplete;
            }
        }

        private void OnLoginComplete(string authCode)
        {
            AuthCode = authCode;
            Hide();
        }

        public void NavigateWebView(Uri uri)
        {
            LoginWebView.Navigate(uri);
        }
    }
}
