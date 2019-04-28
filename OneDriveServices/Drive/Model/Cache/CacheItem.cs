using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Cache
{
    /// <summary>
    /// An element of the cache tree structure. It holds a DriveItem and a List of its children that are present in the cache.
    /// </summary>
    public class CacheItem
    {
        public DriveItem Item { get; set; }
        public List<CacheItem> Children { get; set; }

        public CacheItem(DriveItem item)
        {
            Item = item;
            Children = new List<CacheItem>();
        }
    }
}
