using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Clipboard
{
    public interface IClipboardOperation
    {
        Task ExecuteAsync(DriveItem content, DriveFolder parent);
    }
}
