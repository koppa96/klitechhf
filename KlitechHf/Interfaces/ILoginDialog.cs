using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KlitechProba.Interfaces
{
    public interface ILoginDialog
    {
        void NavigateWebView(Uri uri);
        void InvokeLoginComplete(string authCode);
        void Hide();
    }
}
