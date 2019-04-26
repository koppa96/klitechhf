using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Controls;

namespace KlitechHf.Services
{
    public class DialogService
    {
        public async Task<string> ShowRenameDialogAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "Enter a name",
                Content = new TextBox(),
                CloseButtonText = "Ok"
            };

            await dialog.ShowAsync();
            return (dialog.Content as TextBox)?.Text;
        }

        public async Task<bool> ShowConfirmationDialogAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Confirmation needed",
                Content = message,
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }
    }
}