using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using OneDriveServices.Drive.Model.DriveItems;

namespace KlitechHf.Utility
{
    public class DriveItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FileTemplate { get; set; }
        public DataTemplate FolderTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item is DriveFile)
            {
                return FileTemplate;
            }

            if (item is DriveFolder)
            {
                return FolderTemplate;
            }

            throw new ArgumentException("Could not find template for this object");
        }
    }
}
