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
    /// <summary>
    /// Provides a DataContext for the Main page of the application.
    /// Contains smaller ViewModels and synchronizes their behaviour.
    /// </summary>
    public class MainPageViewModel : ViewModelBase
    {
        private readonly DriveService _drive;
        private readonly DialogService _dialogService;
        private bool _canPaste;

        public TaskViewModel CurrentTasks { get; set; }
        public NavigationViewModel NavigationViewModel { get; set; }
        public UserViewModel UserViewModel { get; set; }
        public DriveViewModel DriveViewModel { get; set; }

        public bool CanPaste {
            get => _canPaste;
            set {
                _canPaste = value;
                RaisePropertyChanged();
            }
        }

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
            DriveViewModel = new DriveViewModel(dialogService);
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
            PasteCommand = new DelegateCommand<DriveFolder>(PasteAsync).ObservesCanExecute(() => CanPaste);
            PasteHereCommand = new DelegateCommand(PasteHereAsync).ObservesCanExecute(() => CanPaste);
            NewFolderCommand = new DelegateCommand(CreateFolderAsync);
            RefreshCommand = new DelegateCommand(RefreshAsync);
            NavigateCommand = new DelegateCommand<NavigationItem>(OpenNavigationItemAsync);
        }

        /// <summary>
        /// Starts a loading animation and refreshes the content of the currently viewed folder.
        /// </summary>
        private async void RefreshAsync()
        {
            CurrentTasks.IsBusy = true;
            await DriveViewModel.RefreshAsync();
            CurrentTasks.IsBusy = false;
        }

        /// <summary>
        /// Asks a name for the new folder and creates it if there is no name conflict.
        /// </summary>
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

        /// <summary>
        /// Pastes the content of the clipboard to the currently viewed folder.
        /// </summary>
        private async void PasteHereAsync()
        {
            PasteAsync(DriveViewModel.CurrentFolder);
        }

        /// <summary>
        /// Pastes the content of the clipboard to the target folder.
        /// </summary>
        /// <param name="folder">The target folder</param>
        private async void PasteAsync(DriveFolder folder)
        {
            // Checking if there's no name conflict with the current content of the folder.
            var children = await folder.GetChildrenAsync();
            if (children.Any(c => c.Name == _drive.ClipBoard.Content.Name))
            {
                await _dialogService.ShowNameConflictErrorAsync();
                return;
            }

            CurrentTasks.StartBackgroundTask("Pasting...");

            try
            {
                CanPaste = false;
                // Starts the pasting and awaits the operation. The operation await is canceled when the user logs out.
                await DriveViewModel.PasteAsync(folder);
            }
            catch (TaskCanceledException)
            {
                // Catching the task cancellation exception
            }
            finally
            {
                CurrentTasks.StopBackgroundTask();
                CanPaste = DriveService.Instance.ClipBoard.CanExecute;
            }
        }

        /// <summary>
        /// Opens a file picker dialog and uploads the picked files to the drive.
        /// </summary>
        private async void UploadAsync()
        {
            var files = await _dialogService.ShowFilePickerAsync();

            CurrentTasks.StartBackgroundTask("Uploading...");
            await DriveViewModel.UploadFilesAsync(files);
            CurrentTasks.StopBackgroundTask();
        }

        /// <summary>
        /// Navigates into the parent folder of the currently viewed folder. And shows a loading animation meanwhile.
        /// </summary>
        private async void NavigateUpAsync()
        {
            CurrentTasks.IsBusy = true;
            await DriveViewModel.NavigateUpAsync();
            NavigationViewModel.RemoveLast();
            CurrentTasks.IsBusy = false;
        }

        /// <summary>
        /// Navigates into the target folder and shows its content. Adds the target folder to the navigation history.
        /// </summary>
        /// <param name="folder">The target folder</param>
        private async void OpenSelectedFolderAsync(DriveFolder folder)
        {
            CurrentTasks.IsBusy = true;
            await DriveViewModel.OpenFolderAsync(folder);
            NavigationViewModel.AddItem(new NavigationItem(folder));
            CurrentTasks.IsBusy = false;
        }

        /// <summary>
        /// Navigates into the folder represented by the given NavigationItem.
        /// </summary>
        /// <param name="item">The navigation item</param>
        private async void OpenNavigationItemAsync(NavigationItem item)
        {
            CurrentTasks.IsBusy = true;
            NavigationViewModel.RemoveLaterThan(item);
            await DriveViewModel.OpenNavigationItemAsync(item);
            CurrentTasks.IsBusy = false;
        }

        /// <summary>
        /// Shows a folder picker dialog and downloads the content of the given DriveFile into the selected folder.
        /// </summary>
        /// <param name="file">The DriveFile to be downloaded</param>
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

        /// <summary>
        /// Deletes the given item.
        /// </summary>
        /// <param name="item">The item to be deleted.</param>
        private async void DeleteSelectedItemAsync(DriveItem item)
        {
            if (await _dialogService.ShowConfirmationDialogAsync($"Are you sure want to delete {item.Name}?"))
            {
                CurrentTasks.StartBackgroundTask("Deleting...");
                await DriveViewModel.DeleteAsync(item);
                CurrentTasks.StopBackgroundTask();
            }
        }
        
        /// <summary>
        /// Shows a name entering dialog and if there are no name conflict it renames the given DriveItem to its new name.
        /// </summary>
        /// <param name="item">The DriveItem to be renamed</param>
        private async void RenameSelectedItemAsync(DriveItem item)
        {
            var name = await _dialogService.ShowNameDialogAsync();
            if (name == null)
            {
                return;
            }

            // Checking for name conflicts
            if (DriveViewModel.Children.Any(c => c.Name == name))
            {
                await _dialogService.ShowNameConflictErrorAsync();
                return;
            }

            CurrentTasks.StartBackgroundTask("Renaming...");
            await DriveViewModel.RenameAsync(item, name);
            CurrentTasks.StopBackgroundTask();
        }

        /// <summary>
        /// Called when the page is opened. It starts the login process if there is no user logged in currently.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="viewModelState"></param>
        public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            if (AuthService.Instance.CurrentUser == null)
            {
                await LoginAsync();
            }
        }

        /// <summary>
        /// Logs in the user and loads their Drive's root folder.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Adds the given item to the clipboard and updates the CanExecute of the paste commands.
        /// </summary>
        /// <param name="item">The DriveItem to be copied</param>
        private void CopySelectedItem(DriveItem item)
        {
            DriveViewModel.CopyItem(item);
            CanPaste = DriveService.Instance.ClipBoard.CanExecute;
        }

        /// <summary>
        /// Adds the given item to the clipboard and updates the CanExecute of the paste commands.
        /// </summary>
        /// <param name="item">The DriveItem to be cut</param>
        private void CutSelectedItem(DriveItem item)
        {
            DriveViewModel.CutItem(item);
            CanPaste = DriveService.Instance.ClipBoard.CanExecute;
        }

        /// <summary>
        /// Signs the current user out, clears the cached data and the tokens of the user.
        /// </summary>
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
