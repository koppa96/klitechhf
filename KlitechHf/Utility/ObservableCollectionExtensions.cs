using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KlitechHf.Model;
using OneDriveServices.Drive.Model.DriveItems;

namespace KlitechHf.Utility
{
    public static class ObservableCollectionExtensions
    {
        public static void InsertDriveItemSorted(this ObservableCollection<DriveItem> collection, DriveItem item)
        {
            if (item is ParentItem)
            {
                collection.Insert(0, item);
                return;
            }

            var children = collection.ToList();
            children.Add(item);
            children = children.OrderBy(i => !(i is ParentItem)).ThenBy(i => i is DriveFile).ThenBy(i => i.Name).ToList();

            collection.Insert(children.IndexOf(item), item);
        }
    }
}
