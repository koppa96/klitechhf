using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using OneDriveServices.Drive;
using OneDriveServices.Drive.Model;
using OneDriveServices.Drive.Model.DriveItems;
using Prism.Windows.Mvvm;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace KlitechHf.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var folder = new DriveFolder
            {
                Id = "147EF121E9508B5!3349"
            };

            var items = await folder.GetChildrenAsync();
        }

        private async void FolderCreation(object sender, RoutedEventArgs e)
        {
            var driveService = DriveService.Instance;
            await driveService.InitializeAsync();
            //await driveService.CreateFolderAsync("TESTFOLDER");

            var dialog = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };

            dialog.FileTypeFilter.Add("*");

            var file = await dialog.PickSingleFileAsync();
            await driveService.UploadAsync(file);
        }
    }
}
