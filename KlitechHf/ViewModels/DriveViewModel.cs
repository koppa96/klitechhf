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
using KlitechHf.Utility;
using OneDriveServices.Drive;
using OneDriveServices.Drive.Model.DriveItems;
using Prism.Commands;

namespace KlitechHf.ViewModels
{
    public class DriveViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<DriveItem> _children;
        private bool _removeItemOnPaste;
        private DriveService _drive;

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

        public DriveViewModel()
        {
            _drive = DriveService.Instance;
            _removeItemOnPaste = false;
            Children = new ObservableCollection<DriveItem>();
        }

        public async Task RefreshAsync()
        {
            CurrentFolder = await _drive.LoadItemAsync<DriveFolder>(CurrentFolder.Id);
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }
        }

        public async Task CreateFolderAsync(string name)
        {
            var folder = await _drive.CreateFolderAsync(CurrentFolder, name);
            Children.InsertDriveItemSorted(folder);
        }

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

        public async Task UploadFilesAsync(IEnumerable<StorageFile> files)
        {
            foreach (var file in files)
            {
                var content = await FileIO.ReadBufferAsync(file);
                var driveFile = await _drive.UploadAsync(CurrentFolder, file.Name, content.ToArray());
                Children.InsertDriveItemSorted(driveFile);
            }
        }

        public async Task NavigateUpAsync()
        {
            CurrentFolder = await CurrentFolder.GetParentAsync();
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }
        }

        public async Task OpenFolderAsync(DriveFolder folder)
        {
            CurrentFolder = folder;
            Children = new ObservableCollection<DriveItem>(await folder.GetChildrenAsync());
            Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
        }

        public async Task OpenNavigationItemAsync(NavigationItem item)
        {
            CurrentFolder = await _drive.GetItemAsync<DriveFolder>(item.Id);
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                Children.InsertDriveItemSorted(new ParentItem { Id = CurrentFolder.Parent.Id });
            }
        }

        public async Task DownloadFileAsync(DriveFile file, StorageFolder folder)
        {
            var storageFile = await folder.CreateFileAsync(file.Name);
            var content = await file.DownloadAsync();
            await FileIO.WriteBytesAsync(storageFile, content);
        }

        public async Task DeleteAsync(DriveItem item)
        {
            await item.DeleteAsync();
            Children.Remove(item);
        }

        public async Task RenameAsync(DriveItem item, string name)
        {
            await item.RenameAsync(name);

            var index = Children.IndexOf(item);
            Children.Remove(item);
            Children.Insert(index, item);
        }

        public async Task LoadRootFolderAsync()
        {
            CurrentFolder = await _drive.GetRootAsync();
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
        }

        public void CopyItem(DriveItem item)
        {
            _removeItemOnPaste = false;
            _drive.Copy(item);
        }

        public void CutItem(DriveItem item)
        {
            _removeItemOnPaste = true;
            _drive.Cut(item);
        }

        public void Clear()
        {
            CurrentFolder = null;
            Children.Clear();
        }
    }
}
