using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using KlitechHf.Services;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace KlitechHf.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static string[] scopes = { "offline_access", "User.Read", "Files.ReadWrite.All" };

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void GetAccessTokenAsync(object sender, RoutedEventArgs e)
        {
            var authService = AuthService.Instance;
            await authService.ShowLoginDialogAsync();
        }

        public async void ShowLoginDialog(object sender, RoutedEventArgs e)
        {
            var loginDialog = new LoginDialog();

            /*
            loginDialog.Measure(new Size(400, 600));
            loginDialog.Arrange(new Rect(new Point(100, 100), new Size(400, 600)));
            loginDialog.UpdateLayout();*/

            await loginDialog.ShowAsync();
        }

        private async void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            await AuthService.Instance.RefreshTokensAsync();
        }
    }
}
