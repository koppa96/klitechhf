using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Clipboard
{
    public class Clipboard
    {
        public DriveItem Content { get; set; }
        public IClipboardOperation Operation { get; set; }

        public async Task ExecuteAsync()
        {
            await Operation.ExecuteAsync(Content, DriveService.Instance.DriveId);
            Content = null;
            Operation = null;
        }
    }
}
