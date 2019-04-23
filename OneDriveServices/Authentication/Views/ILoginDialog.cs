using System;

namespace OneDriveServices.Authentication.Views
{
    public interface ILoginDialog
    {
        void NavigateWebView(Uri uri);
    }
}
