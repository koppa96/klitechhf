﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Data;
using KlitechHf.Model;
using OneDriveServices.Authentication;
using OneDriveServices.Drive;
using OneDriveServices.Drive.Model.DriveItems;
using Prism.Commands;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;

namespace KlitechHf.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private readonly DriveService _drive;
        private DriveFolder _currentFolder;
        private ObservableCollection<DriveItem> _children;
        private bool _isLoading;

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

        public ICommand LogoutCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand CutCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand NavigateUpCommand { get; }
        public ICommand UploadCommand { get; }

        public MainPageViewModel()
        {
            Children = new ObservableCollection<DriveItem>();
            _drive = DriveService.Instance;

            LogoutCommand = new DelegateCommand(Logout);
            CopyCommand = new DelegateCommand<DriveItem>(CopySelectedItem);
            CutCommand = new DelegateCommand<DriveItem>(CutSelectedItem);
            RenameCommand = new DelegateCommand<DriveItem>(RenameSelectedItem);
            DeleteCommand = new DelegateCommand<DriveItem>(DeleteSelectedItem);
            DownloadCommand = new DelegateCommand<DriveFile>(DownloadSelectedFile);
            OpenCommand = new DelegateCommand<DriveFolder>(OpenSelectedFolder);
            NavigateUpCommand = new DelegateCommand(NavigateUp);
            UploadCommand = new DelegateCommand(Upload);

            IsLoading = false;
        }

        private async void Upload()
        {
            var filePicker = new FileOpenPicker();
            filePicker.FileTypeFilter.Add("*");

            var files = await filePicker.PickMultipleFilesAsync();
            foreach (var file in files)
            {
                var content = await FileIO.ReadBufferAsync(file);
                await _drive.UploadAsync(CurrentFolder, file.Name, content.ToArray());
            }
        }

        private async void NavigateUp()
        {
            CurrentFolder = await CurrentFolder.GetParentAsync();
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            if (CurrentFolder.Parent.Id != null)
            {
                Children.Insert(0, new ParentItem { Id = CurrentFolder.Parent.Id });
            }
        }

        private async void OpenSelectedFolder(DriveFolder folder)
        {
            CurrentFolder = folder;
            Children = new ObservableCollection<DriveItem>(await folder.GetChildrenAsync());
            Children.Insert(0, new ParentItem { Id = CurrentFolder.Parent.Id });
        }

        private async void DownloadSelectedFile(DriveFile file)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                var storageFile = await folder.CreateFileAsync(file.Name);
                var content = await file.DownloadAsync();
                await FileIO.WriteBytesAsync(storageFile, content);
            }
        }

        private void DeleteSelectedItem(DriveItem obj)
        {
            
        }

        private void RenameSelectedItem(DriveItem item)
        {
            
        }

        public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            await LoginAsync();
        }

        private async Task LoginAsync()
        {
            IsLoading = true;
            await AuthService.Instance.LoginAsync();
            CurrentFolder = await _drive.GetRootAsync();
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            IsLoading = false;
        }

        private void CopySelectedItem(DriveItem item)
        {
            ;
        }

        private void CutSelectedItem(DriveItem item)
        {

        }

        private async void Logout()
        {
            CurrentFolder = null;
            Children.Clear();
            AuthService.Instance.Logout();
            await LoginAsync();
        }
    }
}
