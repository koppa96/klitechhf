using System;

namespace OneDriveServices.Authentication.Views
{
    public interface ILoginDialog
    {
        string AuthCode { get; set; }

        void NavigateWebView(Uri uri);
    }
}
