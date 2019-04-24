using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Cache
{
    public class CacheItem
    {
        public DriveItem Item { get; set; }
        public List<DriveItem> Children { get; set; }
    }
}
