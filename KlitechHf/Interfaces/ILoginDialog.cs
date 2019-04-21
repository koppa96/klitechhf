using System;

namespace KlitechHf.Interfaces
{
    public interface ILoginDialog
    {
        void NavigateWebView(Uri uri);
        void InvokeLoginComplete(string authCode);
        void Hide();
    }
}
