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
        private ObservableCollection<DriveItem> _children;
        private bool _isLoading, _removeItemOnPaste, _canPaste;

        public DriveFolder CurrentFolder { get; set; }
        public TaskViewModel CurrentTasks { get; set; }
        public Navigation Navigation { get; set; }
        public UserViewModel UserViewModel { get; set; }

        public ObservableCollection<DriveItem> Children
        {
            get => _children;
            set
            {
                _children = value;
                RaisePropertyChanged();
            }
        }

        public bool CanPaste 
        {
            get => _canPaste;
            set
            {
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
            Children = new ObservableCollection<DriveItem>();
            CurrentTasks = new TaskViewModel();
            Navigation = new Navigation();
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

            _removeItemOnPaste = false;
        }

        private async void RefreshAsync()
        {
            CurrentTasks.IsBusy = true;
            CurrentFolder = await _drive.LoadItemAsync<DriveFolder>(CurrentFolder.Id);
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }

            CurrentTasks.IsBusy = false;
        }

        private async void CreateFolderAsync()
        {
            var name = await _dialogService.ShowNameDialogAsync();
            if (Children.Any(c => c.Name == name))
            {
                await _dialogService.ShowNameConflictErrorAsync();
                return;
            }

            CurrentTasks.StartBackgroundTask("Creating folder...");

            var folder = await _drive.CreateFolderAsync(CurrentFolder, name);
            Children.InsertDriveItemSorted(folder);

            CurrentTasks.StopBackgroundTask();
        }

        private async void PasteHereAsync()
        {
            PasteAsync(CurrentFolder);
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

            if (_removeItemOnPaste)
            {
                Children.Remove(_drive.ClipBoard.Content);
            }

            try
            {
                CanPaste = false;
                var item = await _drive.PasteAsync(folder);
                if (CurrentFolder.Id == folder.Id)
                {
                    Children.InsertDriveItemSorted(item);
                }

                CanPaste = _drive.ClipBoard.CanExecute;
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

            foreach (var file in files)
            {
                var content = await FileIO.ReadBufferAsync(file);
                var driveFile = await _drive.UploadAsync(CurrentFolder, file.Name, content.ToArray());
                Children.InsertDriveItemSorted(driveFile);
            }

            CurrentTasks.StopBackgroundTask();
        }

        private async void NavigateUpAsync()
        {
            CurrentTasks.IsBusy = true;

            CurrentFolder = await CurrentFolder.GetParentAsync();
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }

            Navigation.RemoveLast();

            CurrentTasks.IsBusy = false;
        }

        private async void OpenSelectedFolderAsync(DriveFolder folder)
        {
            CurrentTasks.IsBusy = true;
            CurrentFolder = folder;
            Navigation.AddItem(new NavigationItem(folder));
            Children = new ObservableCollection<DriveItem>(await folder.GetChildrenAsync());
            Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            CurrentTasks.IsBusy = false;
        }

        private async void OpenNavigationItemAsync(NavigationItem item)
        {
            CurrentTasks.IsBusy = true;

            Navigation.RemoveLaterThan(item);

            CurrentFolder = await _drive.GetItemAsync<DriveFolder>(item.Id);
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }

            CurrentTasks.IsBusy = false;
        }

        private async void DownloadFileAsync(DriveFile file)
        {
            var folder = await _dialogService.ShowFolderPickerAsync();
            if (folder != null)
            {
                CurrentTasks.StartBackgroundTask("Downloading...");

                var storageFile = await folder.CreateFileAsync(file.Name);
                var content = await file.DownloadAsync();
                await FileIO.WriteBytesAsync(storageFile, content);

                CurrentTasks.StopBackgroundTask();
            }
        }

        private async void DeleteSelectedItemAsync(DriveItem item)
        {
            if (await _dialogService.ShowConfirmationDialogAsync($"Are you sure want to delete {item.Name}?"))
            {
                CurrentTasks.StartBackgroundTask("Deleting...");

                await item.DeleteAsync();
                Children.Remove(item);

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

            if (Children.Any(c => c.Name == name))
            {
                await _dialogService.ShowNameConflictErrorAsync();
                return;
            }

            CurrentTasks.StartBackgroundTask("Renaming...");

            await item.RenameAsync(name);

            var index = Children.IndexOf(item);
            Children.Remove(item);
            Children.Insert(index, item);

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

            CurrentFolder = await _drive.GetRootAsync();
            Navigation.AddItem(new NavigationItem
            {
                Id = CurrentFolder.Id,
                Name = "Root"
            });
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());

            CurrentTasks.IsBusy = false;
        }

        private void CopySelectedItem(DriveItem item)
        {
            _removeItemOnPaste = false;
            _drive.Copy(item);
            CanPaste = _drive.ClipBoard.CanExecute;
        }

        private void CutSelectedItem(DriveItem item)
        {
            _removeItemOnPaste = true;
            _drive.Cut(item);
            CanPaste = _drive.ClipBoard.CanExecute;
            
        }

        private async void Logout()
        {
            UserViewModel.CurrentUser = null;
            CurrentFolder = null;
            Children.Clear();
            Navigation.Clear();
            AuthService.Instance.Logout();
            await LoginAsync();
        }
    }
}
