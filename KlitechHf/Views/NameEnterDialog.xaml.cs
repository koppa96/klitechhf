using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KlitechHf.Views
{
    /// <summary>
    /// A simple dialog with a TextBox used to obtain a name from the user.
    /// </summary>
    public sealed partial class NameEnterDialog : ContentDialog
    {
        public string EnteredName { get; set; }

        public NameEnterDialog()
        {
            this.InitializeComponent();
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            EnteredName = NameTextBox.Text;
        }
    }
}
