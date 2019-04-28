using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Clipboard
{
    /// <summary>
    /// A request object that is needed for clipboard operation requests.
    /// </summary>
    public class ClipboardItem
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "parentReference")]
        public ParentReference ParentReference { get; set; }
    }
}
