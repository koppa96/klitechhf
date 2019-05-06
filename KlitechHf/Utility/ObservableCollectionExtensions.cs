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
    /// <summary>
    /// Extension methods for ObservableCollections that store DriveItems
    /// </summary>
    public static class ObservableCollectionExtensions
    {
        /// <summary>
        /// Inserts a DriveItem into the list in a sorted way.
        /// </summary>
        /// <param name="collection">The collection to which the item will be inserted</param>
        /// <param name="item">The item to be inserted</param>
        public static void InsertDriveItemSorted(this ObservableCollection<DriveItem> collection, DriveItem item)
        {
            // If the item is a parent reference it will be inserted into the first position
            if (item is ParentItem)
            {
                collection.Insert(0, item);
                return;
            }

            // If not it will be added to the collection and Sorted by the following scheme:
            // ParentReferences first, then the folders in alphabetic order, then the files in alphabetic order.
            // The collection is copied so that the insertion won't cause the whole ListView to refresh
            var children = collection.ToList();
            children.Add(item);
            children = children.OrderBy(i => !(i is ParentItem)).ThenBy(i => i is DriveFile).ThenBy(i => i.Name).ToList();

            // Inserts the item to its correct position that is read from the ordered copy of the child list.
            collection.Insert(children.IndexOf(item), item);
        }
    }
}
