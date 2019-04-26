using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OneDriveServices.Drive.Model.DriveItems
{
    public class FolderInfo
    {
        [JsonProperty("childCount")]
        public int ChildCount { get; set; }
    }
}
