using System;

namespace OneDriveServices.Authentication.Views
{
    /// <summary>
    /// An interface that is used by the LoginDialogViewModel to communicate with the dialog
    /// </summary>
    public interface ILoginDialog
    {
        string AuthCode { get; set; }

        /// <summary>
        /// Navigates the WebView of the login dialog to the given URI.
        /// </summary>
        /// <param name="uri">The target URI</param>
        void NavigateWebView(Uri uri);
    }
}
