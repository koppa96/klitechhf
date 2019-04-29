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
        private bool _isLoading, _removeItemOnPaste, _canPaste;
        private User _currentUser;

        public DriveFolder CurrentFolder {
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

        public bool IsLoading {
            get => _isLoading;
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        public User CurrentUser {
            get => _currentUser;
            set
            {
                _currentUser = value; 
                RaisePropertyChanged();
            }
        }

        public bool CanPaste {
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
            DownloadCommand = new DelegateCommand<DriveFile>(DownloadSelectedFileAsync);
            OpenCommand = new DelegateCommand<DriveFolder>(OpenSelectedFolderAsync);
            NavigateUpCommand = new DelegateCommand(NavigateUpAsync);
            UploadCommand = new DelegateCommand(UploadAsync);
            PasteCommand = new DelegateCommand<DriveFolder>(PasteAsync).ObservesCanExecute(() => CanPaste);
            PasteHereCommand = new DelegateCommand(PasteHereAsync).ObservesCanExecute(() => CanPaste);
            NewFolderCommand = new DelegateCommand(CreateFolderAsync);

            IsLoading = false;
            _removeItemOnPaste = false;
        }

        private async void CreateFolderAsync()
        {
            var name = await _dialogService.ShowNameDialogAsync();
            if (Children.Any(c => c.Name == name))
            {
                await _dialogService.ShowNameConflictErrorAsync();
                return;
            }

            var folder = await _drive.CreateFolderAsync(CurrentFolder, name);
            Children.InsertDriveItemSorted(folder);
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

            if (_removeItemOnPaste)
            {
                Children.Remove(_drive.ClipBoard.Content);
            }

            try
            {
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
        }

        private async Task ReloadContentAsync()
        {
            IsLoading = true;
            CurrentFolder = await _drive.GetItemAsync<DriveFolder>(CurrentFolder.Id);
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }

            IsLoading = false;
        }

        private async void UploadAsync()
        {
            var files = await _dialogService.ShowFilePickerAsync();
            IsLoading = true;
            foreach (var file in files)
            {
                var content = await FileIO.ReadBufferAsync(file);
                var driveFile = await _drive.UploadAsync(CurrentFolder, file.Name, content.ToArray());
                Children.InsertDriveItemSorted(driveFile);
            }

            await ReloadContentAsync();
            IsLoading = false;
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

            IsLoading = false;
        }

        private async void OpenSelectedFolderAsync(DriveFolder folder)
        {
            IsLoading = true;
            CurrentFolder = folder;
            Children = new ObservableCollection<DriveItem>(await folder.GetChildrenAsync());
            Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            IsLoading = false;
        }

        private async void DownloadSelectedFileAsync(DriveFile file)
        {
            var folder = await _dialogService.ShowFolderPickerAsync();
            if (folder != null)
            {
                IsLoading = true;
                var storageFile = await folder.CreateFileAsync(file.Name);
                var content = await file.DownloadAsync();
                await FileIO.WriteBytesAsync(storageFile, content);
                IsLoading = false;
            }
        }

        private async void DeleteSelectedItemAsync(DriveItem item)
        {
            if (await _dialogService.ShowConfirmationDialogAsync($"Are you sure want to delete {item.Name}?"))
            {
                await item.DeleteAsync();
                Children.Remove(item);
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

            await item.RenameAsync(name);

            var index = Children.IndexOf(item);
            Children.Remove(item);
            Children.Insert(index, item);
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
            AuthService.Instance.Logout();
            await LoginAsync();
        }
    }
}
