using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OneDriveServices.Authentication;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Clipboard
{
    public class Clipboard
    {
        public DriveItem Content { get; set; }
        public IClipboardOperation Operation { get; set; }
        public bool Empty => Content == null;
    }
}
