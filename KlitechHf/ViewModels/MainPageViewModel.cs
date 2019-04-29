using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Data;
using KlitechHf.Model;
using KlitechHf.Services;
using KlitechHf.Utility;
using OneDriveServices.Authentication;
using OneDriveServices.Authentication.Model;
using OneDriveServices.Drive;
using OneDriveServices.Drive.Model.DriveItems;
using Prism.Commands;
using Prism.Events;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;

namespace KlitechHf.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private readonly DriveService _drive;
        private readonly DialogService _dialogService;

        public TaskViewModel CurrentTasks { get; set; }
        public NavigationViewModel NavigationViewModel { get; set; }
        public UserViewModel UserViewModel { get; set; }
        public DriveViewModel DriveViewModel { get; set; }

        public ICommand LogoutCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand CutCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand NavigateUpCommand { get; }
        public ICommand UploadCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand PasteHereCommand { get; }
        public ICommand NewFolderCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand NavigateCommand { get; }

        public MainPageViewModel(DialogService dialogService)
        {
            DriveViewModel = new DriveViewModel();
            CurrentTasks = new TaskViewModel();
            NavigationViewModel = new NavigationViewModel();
            UserViewModel = new UserViewModel();
            _drive = DriveService.Instance;
            _dialogService = dialogService;

            LogoutCommand = new DelegateCommand(Logout);
            CopyCommand = new DelegateCommand<DriveItem>(CopySelectedItem);
            CutCommand = new DelegateCommand<DriveItem>(CutSelectedItem);
            RenameCommand = new DelegateCommand<DriveItem>(RenameSelectedItemAsync);
            DeleteCommand = new DelegateCommand<DriveItem>(DeleteSelectedItemAsync);
            DownloadCommand = new DelegateCommand<DriveFile>(DownloadFileAsync);
            OpenCommand = new DelegateCommand<DriveFolder>(OpenSelectedFolderAsync);
            NavigateUpCommand = new DelegateCommand(NavigateUpAsync);
            UploadCommand = new DelegateCommand(UploadAsync);
            PasteCommand = new DelegateCommand<DriveFolder>(PasteAsync).ObservesCanExecute(() => DriveViewModel.CanPaste);
            PasteHereCommand = new DelegateCommand(PasteHereAsync).ObservesCanExecute(() => DriveViewModel.CanPaste);
            NewFolderCommand = new DelegateCommand(CreateFolderAsync);
            RefreshCommand = new DelegateCommand(RefreshAsync);
            NavigateCommand = new DelegateCommand<NavigationItem>(OpenNavigationItemAsync);
        }

        private async void RefreshAsync()
        {
            CurrentTasks.IsBusy = true;
            await DriveViewModel.RefreshAsync();
            CurrentTasks.IsBusy = false;
        }

        private async void CreateFolderAsync()
        {
            var name = await _dialogService.ShowNameDialogAsync();
            if (DriveViewModel.Children.Any(c => c.Name == name))
            {
                await _dialogService.ShowNameConflictErrorAsync();
                return;
            }

            CurrentTasks.StartBackgroundTask("Creating folder...");
            await DriveViewModel.CreateFolderAsync(name);
            CurrentTasks.StopBackgroundTask();
        }

        private async void PasteHereAsync()
        {
            PasteAsync(DriveViewModel.CurrentFolder);
        }

        private async void PasteAsync(DriveFolder folder)
        {
            var children = await folder.GetChildrenAsync();
            if (children.Any(c => c.Name == _drive.ClipBoard.Content.Name))
            {
                await _dialogService.ShowNameConflictErrorAsync();
                return;
            }

            CurrentTasks.StartBackgroundTask("Pasting...");

            try
            {
                await DriveViewModel.PasteAsync(folder);
            }
            catch (TaskCanceledException)
            {
                // Catching the task cancellation exception
            }
            finally
            {
                CurrentTasks.StopBackgroundTask();
            }
        }

        private async void UploadAsync()
        {
            var files = await _dialogService.ShowFilePickerAsync();

            CurrentTasks.StartBackgroundTask("Uploading...");
            await DriveViewModel.UploadFilesAsync(files);
            CurrentTasks.StopBackgroundTask();
        }

        private async void NavigateUpAsync()
        {
            CurrentTasks.IsBusy = true;
            await DriveViewModel.NavigateUpAsync();
            NavigationViewModel.RemoveLast();
            CurrentTasks.IsBusy = false;
        }

        private async void OpenSelectedFolderAsync(DriveFolder folder)
        {
            CurrentTasks.IsBusy = true;
            await DriveViewModel.OpenFolderAsync(folder);
            NavigationViewModel.AddItem(new NavigationItem(folder));
            CurrentTasks.IsBusy = false;
        }

        private async void OpenNavigationItemAsync(NavigationItem item)
        {
            CurrentTasks.IsBusy = true;
            NavigationViewModel.RemoveLaterThan(item);
            await DriveViewModel.OpenNavigationItemAsync(item);
            CurrentTasks.IsBusy = false;
        }

        private async void DownloadFileAsync(DriveFile file)
        {
            var folder = await _dialogService.ShowFolderPickerAsync();
            if (folder != null)
            {
                CurrentTasks.StartBackgroundTask("Downloading...");
                await DriveViewModel.DownloadFileAsync(file, folder);
                CurrentTasks.StopBackgroundTask();
            }
        }

        private async void DeleteSelectedItemAsync(DriveItem item)
        {
            if (await _dialogService.ShowConfirmationDialogAsync($"Are you sure want to delete {item.Name}?"))
            {
                CurrentTasks.StartBackgroundTask("Deleting...");
                await DriveViewModel.DeleteAsync(item);
                CurrentTasks.StopBackgroundTask();
            }
        }

        private async void RenameSelectedItemAsync(DriveItem item)
        {
            var name = await _dialogService.ShowNameDialogAsync();
            if (name == null)
            {
                return;
            }

            if (DriveViewModel.Children.Any(c => c.Name == name))
            {
                await _dialogService.ShowNameConflictErrorAsync();
                return;
            }

            CurrentTasks.StartBackgroundTask("Renaming...");
            await DriveViewModel.RenameAsync(item, name);
            CurrentTasks.StopBackgroundTask();
        }

        public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            await LoginAsync();
        }

        private async Task LoginAsync()
        {
            CurrentTasks.IsBusy = true;

            await AuthService.Instance.LoginAsync();
            UserViewModel.CurrentUser = AuthService.Instance.CurrentUser;
            await DriveViewModel.LoadRootFolderAsync();
            NavigationViewModel.AddItem(new NavigationItem
            {
                Id = DriveViewModel.CurrentFolder.Id,
                Name = "Root"
            });

            CurrentTasks.IsBusy = false;
        }

        private void CopySelectedItem(DriveItem item)
        {
            DriveViewModel.CopyItem(item);
        }

        private void CutSelectedItem(DriveItem item)
        {
            DriveViewModel.CutItem(item);
        }

        private async void Logout()
        {
            UserViewModel.CurrentUser = null;
            DriveViewModel.Clear();
            NavigationViewModel.Clear();
            AuthService.Instance.Logout();
            await LoginAsync();
        }
    }
}
