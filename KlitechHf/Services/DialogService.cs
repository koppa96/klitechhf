using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using KlitechHf.Views;

namespace KlitechHf.Services
{
    /// <summary>
    /// A service used for setting up and showing interaction dialogs for the user.
    /// </summary>
    public class DialogService
    {
        /// <summary>
        /// Shows a name enter dialog asynchronously and returns the entered name by the user or null if canceled.
        /// </summary>
        /// <returns>The name entered by the user</returns>
        public async Task<string> ShowNameDialogAsync()
        {
            var dialog = new NameEnterDialog();
            var result = await dialog.ShowAsync();

            return result == ContentDialogResult.Primary ? dialog.EnteredName : null;
        }

        /// <summary>
        /// Shows a confirmation dialog with the given message asynchronously.
        /// </summary>
        /// <param name="message">The message of the dialog</param>
        /// <returns>The response of the user</returns>
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

        /// <summary>
        /// Shows a file picker dialog asynchronously.
        /// </summary>
        /// <returns>The files selected by the user</returns>
        public async Task<IEnumerable<StorageFile>> ShowFilePickerAsync()
        {
            var filePicker = new FileOpenPicker();
            filePicker.FileTypeFilter.Add("*");

            return await filePicker.PickMultipleFilesAsync();
        }

        /// <summary>
        /// Shows a folder picker dialog asynchronously.
        /// </summary>
        /// <returns>The folder selected by the user</returns>
        public async Task<StorageFolder> ShowFolderPickerAsync()
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            return await folderPicker.PickSingleFolderAsync();
        }

        /// <summary>
        /// Shows a dialog that informs the user about a name conflict at copying/renaming/uploading.
        /// </summary>
        /// <returns>A task representing the operation</returns>
        public async Task ShowNameConflictErrorAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = "There is already a file with that name there.",
                CloseButtonText = "Ok"
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// Informs the user that there is a name conflict. It offers to enter a new name for the downloaded file.
        /// </summary>
        /// <returns>The new name of the file</returns>
        public async Task<string> ShowDownloadNameConflictErrorAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = "The target folder contains a file with this name. Please enter a new name for the file.",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Ok"
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? await ShowNameDialogAsync() : null;
        }
    }
}