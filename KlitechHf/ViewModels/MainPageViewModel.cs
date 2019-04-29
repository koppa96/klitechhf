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
        private DriveFolder _currentFolder;
        private ObservableCollection<DriveItem> _children;
        private ObservableCollection<NavigationItem> _navigationItems;
        private bool _isLoading, _removeItemOnPaste, _canPaste, _backgroundTaskRunning;
        private User _currentUser;
        private string _backgroundTaskName;

        public DriveFolder CurrentFolder 
        {
            get => _currentFolder;
            set
            {
                _currentFolder = value; 
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<DriveItem> Children
        {
            get => _children;
            set
            {
                _children = value;
                RaisePropertyChanged();
            }
        }

        public bool IsLoading 
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        public User CurrentUser 
        {
            get => _currentUser;
            set
            {
                _currentUser = value; 
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

        public bool BackgroundTaskRunning
        {
            get => _backgroundTaskRunning;
            set
            {
                _backgroundTaskRunning = value;
                RaisePropertyChanged();
            }
        }

        public string BackgroundTaskName
        {
            get => _backgroundTaskName;
            set
            {
                _backgroundTaskName = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<NavigationItem> NavigationItems {
            get => _navigationItems;
            set
            {
                _navigationItems = value;
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

            IsLoading = false;
            _removeItemOnPaste = false;
        }

        private async void RefreshAsync()
        {
            IsLoading = true;
            CurrentFolder = await _drive.LoadItemAsync<DriveFolder>(CurrentFolder.Id);
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }

            IsLoading = false;
        }

        private async void CreateFolderAsync()
        {
            var name = await _dialogService.ShowNameDialogAsync();
            if (Children.Any(c => c.Name == name))
            {
                await _dialogService.ShowNameConflictErrorAsync();
                return;
            }

            StartBackgroundTask("Creating folder...");

            var folder = await _drive.CreateFolderAsync(CurrentFolder, name);
            Children.InsertDriveItemSorted(folder);

            StopBackgroundTask();
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

            StartBackgroundTask("Pasting...");

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
                StopBackgroundTask();
            }
        }

        private async void UploadAsync()
        {
            var files = await _dialogService.ShowFilePickerAsync();

            StartBackgroundTask("Uploading...");

            foreach (var file in files)
            {
                var content = await FileIO.ReadBufferAsync(file);
                var driveFile = await _drive.UploadAsync(CurrentFolder, file.Name, content.ToArray());
                Children.InsertDriveItemSorted(driveFile);
            }

            StopBackgroundTask();
        }

        private async void NavigateUpAsync()
        {
            IsLoading = true;

            CurrentFolder = await CurrentFolder.GetParentAsync();
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }

            var navigationItem = NavigationItems.Single(i => i.Id == CurrentFolder.Id);
            RemoveLaterNavigationItems(navigationItem);

            IsLoading = false;
        }

        private async void OpenSelectedFolderAsync(DriveFolder folder)
        {
            IsLoading = true;
            CurrentFolder = folder;
            NavigationItems.Add(new NavigationItem(folder));
            Children = new ObservableCollection<DriveItem>(await folder.GetChildrenAsync());
            Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            IsLoading = false;
        }

        private async void OpenNavigationItemAsync(NavigationItem item)
        {
            IsLoading = true;

            RemoveLaterNavigationItems(item);

            CurrentFolder = await _drive.GetItemAsync<DriveFolder>(item.Id);
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }

            IsLoading = false;
        }

        private async void DownloadFileAsync(DriveFile file)
        {
            var folder = await _dialogService.ShowFolderPickerAsync();
            if (folder != null)
            {
                StartBackgroundTask("Downloading...");

                var storageFile = await folder.CreateFileAsync(file.Name);
                var content = await file.DownloadAsync();
                await FileIO.WriteBytesAsync(storageFile, content);

                StopBackgroundTask();
            }
        }

        private async void DeleteSelectedItemAsync(DriveItem item)
        {
            if (await _dialogService.ShowConfirmationDialogAsync($"Are you sure want to delete {item.Name}?"))
            {
                StartBackgroundTask("Deleting...");

                await item.DeleteAsync();
                Children.Remove(item);

                StopBackgroundTask();
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

            StartBackgroundTask("Renaming...");

            await item.RenameAsync(name);

            var index = Children.IndexOf(item);
            Children.Remove(item);
            Children.Insert(index, item);

            StopBackgroundTask();
        }

        public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            await LoginAsync();
        }

        private async Task LoginAsync()
        {
            IsLoading = true;

            await AuthService.Instance.LoginAsync();
            CurrentUser = AuthService.Instance.CurrentUser;

            CurrentFolder = await _drive.GetRootAsync();
            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem
                {
                    Id = CurrentFolder.Id,
                    Name = "Root"
                }
            };
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());

            IsLoading = false;
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
            CurrentUser = null;
            CurrentFolder = null;
            Children.Clear();
            NavigationItems.Clear();
            AuthService.Instance.Logout();
            await LoginAsync();
        }

        private void StartBackgroundTask(string name)
        {
            BackgroundTaskRunning = true;
            BackgroundTaskName = name;
        }

        private void StopBackgroundTask()
        {
            BackgroundTaskRunning = false;
            BackgroundTaskName = null;
        }

        private void RemoveLaterNavigationItems(NavigationItem item)
        {
            for (int i = NavigationItems.Count - 1; i > NavigationItems.IndexOf(item); i--)
            {
                NavigationItems.RemoveAt(i);
            }
        }
    }
}
