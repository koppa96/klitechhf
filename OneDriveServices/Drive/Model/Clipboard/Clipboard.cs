using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OneDriveServices.Authentication;
using OneDriveServices.Drive.Model.Clipboard.Operations;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Clipboard
{
    /// <summary>
    /// Holds an item with an operation. 
    /// </summary>
    public class Clipboard
    {
        public DriveItem Content { get; set; }
        public IClipboardOperation Operation { get; set; }
        public bool CanExecute => Content != null && Operation != null;

        public async Task ExecuteAsync(DriveFolder targetFolder)
        {
            if (!CanExecute)
            {
                return;
            }

            await Operation.ExecuteAsync(Content, targetFolder);
        }

        public void Clear()
        {
            Content = null;
            Operation = null;
        }
    }
}
