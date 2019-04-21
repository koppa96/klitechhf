using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using KlitechHf.Interfaces;
using KlitechHf.ViewModels;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KlitechHf.Views
{
    public sealed partial class LoginDialog : ContentDialog, ILoginDialog
    {
        public event Func<string, Task> LoginComplete;

        public LoginDialog()
        {
            this.InitializeComponent();
            if (DataContext is LoginDialogViewModel viewModel)
            {
                viewModel.Initialize(this);
            }
        }

        public void NavigateWebView(Uri uri)
        {
            LoginWebView.Navigate(uri);
        }

        public void InvokeLoginComplete(string authCode)
        {
            LoginComplete?.Invoke(authCode);
        }
    }
}
