using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using KlitechHf.Annotations;
using KlitechHf.Model;
using KlitechHf.Services;
using KlitechHf.Utility;
using OneDriveServices.Drive;
using OneDriveServices.Drive.Model.DriveItems;
using Prism.Commands;

namespace KlitechHf.ViewModels
{
    /// <summary>
    /// A ViewModel that stores the current folder and the contents of the current folder.
    /// </summary>
    public class DriveViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<DriveItem> _children;
        private bool _removeItemOnPaste;
        private DriveService _drive;
        private DialogService _dialogService;

        public DriveFolder CurrentFolder { get; set; }

        public ObservableCollection<DriveItem> Children {
            get => _children;
            set {
                _children = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DriveViewModel(DialogService dialogService)
        {
            _drive = DriveService.Instance;
            _dialogService = dialogService;
            _removeItemOnPaste = false;
            Children = new ObservableCollection<DriveItem>();
        }

        /// <summary>
        /// Refreshes the contents of the current folder by force loading the content.
        /// </summary>
        /// <returns>A task representing the operation</returns>
        public async Task RefreshAsync()
        {
            CurrentFolder = await _drive.LoadItemAsync<DriveFolder>(CurrentFolder.Id);
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                // Inserting an item to the top of the current folder with a reference to the current folder's parent.
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }
        }

        /// <summary>
        /// Creates a folder with the given name and inserts it into the list of children.
        /// </summary>
        /// <param name="name">The name of the new folder</param>
        /// <returns>A task representing the operation</returns>
        public async Task CreateFolderAsync(string name)
        {
            var folder = await _drive.CreateFolderAsync(CurrentFolder, name);
            Children.InsertDriveItemSorted(folder);
        }

        /// <summary>
        /// Pastes the content of the clipboard into the target folder. If it's necessary it removes the pasted item from its original place.
        /// </summary>
        /// <param name="folder">The target folder</param>
        /// <returns>A task representing the operation</returns>
        public async Task PasteAsync(DriveFolder folder)
        {
            if (_removeItemOnPaste)
            {
                Children.Remove(_drive.ClipBoard.Content);
            }

            var item = await _drive.PasteAsync(folder);
            if (CurrentFolder.Id == folder.Id)
            {
                Children.InsertDriveItemSorted(item);
            }
        }

        /// <summary>
        /// Uploads the contents of the given files to the current folder and inserts them into the list of children.
        /// </summary>
        /// <param name="files">The list of files</param>
        /// <returns>A task representing the operation</returns>
        public async Task UploadFilesAsync(IEnumerable<StorageFile> files)
        {
            foreach (var file in files)
            {
                var driveFile = await _drive.UploadAsync(CurrentFolder, file.Name, file);
                Children.InsertDriveItemSorted(driveFile);
            }
        }

        /// <summary>
        /// Navigates into the parent of the current folder and loads its content.
        /// </summary>
        /// <returns>A task representing the operation</returns>
        public async Task NavigateUpAsync()
        {
            CurrentFolder = await CurrentFolder.GetParentAsync();
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                // If the folder is not the root folder insert a parent navigation reference.
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }
        }

        /// <summary>
        /// Navigates into the given folder and loads its content to the list of children.
        /// </summary>
        /// <param name="folder">The folder to be opened</param>
        /// <returns>A task representing the operation</returns>
        public async Task OpenFolderAsync(DriveFolder folder)
        {
            CurrentFolder = folder;
            Children = new ObservableCollection<DriveItem>(await folder.GetChildrenAsync());
            Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
        }

        /// <summary>
        /// Navigates into the folder represented by the given navigation item from the navigation history.
        /// </summary>
        /// <param name="item">The navigation item</param>
        /// <returns>A task representing the operation</returns>
        public async Task OpenNavigationItemAsync(NavigationItem item)
        {
            CurrentFolder = await _drive.GetItemAsync<DriveFolder>(item.Id);
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }
        }

        /// <summary>
        /// Downloads the given file into the previously selected folder.
        /// </summary>
        /// <param name="file">The DriveFile to be downloaded</param>
        /// <param name="folder">The folder on the local PC into which the file will be saved</param>
        /// <returns>A task representing the operation</returns>
        public async Task DownloadFileAsync(DriveFile file, StorageFolder folder)
        {
            var storageFile = await CreateLocalFileAsync(file.Name, folder);
            if (storageFile != null)
            {
                await file.DownloadAsync(storageFile);
            }
        }

        private async Task<StorageFile> CreateLocalFileAsync(string fileName, StorageFolder folder)
        {
            StorageFile file = null;

            try
            {
                file = await folder.CreateFileAsync(fileName);
            }
            catch (Exception)
            {
                var newName = await _dialogService.ShowDownloadNameConflictErrorAsync();
                if (newName != null)
                {
                    file = await CreateLocalFileAsync(newName, folder);
                }
            }

            return file;
        }

        /// <summary>
        /// Deletes the given DriveItem and removes it from the list of children.
        /// </summary>
        /// <param name="item">The DriveItem to be deleted</param>
        /// <returns>A task representing the operation</returns>
        public async Task DeleteAsync(DriveItem item)
        {
            await item.DeleteAsync();
            Children.Remove(item);
        }

        /// <summary>
        /// Renames the given item to the given name and reorders the list of children so the element is in its new alphabetical position.
        /// </summary>
        /// <param name="item">The DriveItem to be renamed</param>
        /// <param name="name">The new name of the DriveItem</param>
        /// <returns>A task representing the operation</returns>
        public async Task RenameAsync(DriveItem item, string name)
        {
            await item.RenameAsync(name);

            var index = Children.IndexOf(item);
            Children.Remove(item);
            Children.InsertDriveItemSorted(item);
        }

        /// <summary>
        /// Gets the root folder of the drive and loads its content into the list of children.
        /// </summary>
        /// <returns>A task representing the operation</returns>
        public async Task LoadRootFolderAsync()
        {
            CurrentFolder = await _drive.GetRootAsync();
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
        }

        /// <summary>
        /// Adds the selected item to the clipboard.
        /// </summary>
        /// <param name="item">The item to be copied</param>
        public void CopyItem(DriveItem item)
        {
            _removeItemOnPaste = false;
            _drive.Copy(item);
        }

        /// <summary>
        /// Adds the selected item to the clipboard. On paste this item will be removed from the list of children.
        /// </summary>
        /// <param name="item">The item to be cut</param>
        public void CutItem(DriveItem item)
        {
            _removeItemOnPaste = true;
            _drive.Cut(item);
        }

        /// <summary>
        /// Clears the data of the current session from the ViewModel.
        /// </summary>
        public void Clear()
        {
            CurrentFolder = null;
            Children.Clear();
        }
    }
}
