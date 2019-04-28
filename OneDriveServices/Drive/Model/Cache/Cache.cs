using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.ObjectBuilder2;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Cache
{
    /// <summary>
    /// Stores the DriveItems so that they don't need to be downloaded every time.
    /// </summary>
    public class Cache
    {
        private readonly Dictionary<string, CacheItem> _itemStorage;
        private DriveFolder _rootFolder;

        public Cache()
        {
            _itemStorage = new Dictionary<string, CacheItem>();
        }

        /// <summary>
        /// Adds a DriveItem to the cache. If it is already added it will be updated.
        /// The item will be added as a child of its parent, and as a parent of its children if they are already cached.
        /// </summary>
        /// <param name="item">The item to be added</param>
        public void AddItem(DriveItem item)
        {
            //Adding the item to the cache
            CacheItem cachedItem;
            if (_itemStorage.ContainsKey(item.Id))
            {
                cachedItem = _itemStorage[item.Id];
                cachedItem.Item = item;
                return;
            }
            else
            {
                cachedItem = new CacheItem(item);
                _itemStorage.Add(item.Id, cachedItem);
            }
            
            //Adding the item to its parent's children
            if (_itemStorage.ContainsKey(item.Parent.Id))
            {
                var parent = _itemStorage[item.Parent.Id];

                if (parent.Children == null)
                {
                    parent.Children = new List<CacheItem>();
                }

                if (parent.Children.Any(c => c.Item.Id == item.Id))
                {
                    var index = parent.Children.FindIndex(c => c.Item.Id == item.Id);
                    parent.Children[index].Item = item;
                }
                else
                {
                    parent.Children.Add(cachedItem);
                }
            }
            
            //Adding the item's children
            foreach (var value in _itemStorage.Values)
            {
                if (value.Item.Parent.Id == item.Id)
                {
                    value.Children.Add(value);
                }
            }
        }

        /// <summary>
        /// Removes an item from the cache. The item will also be removed from the children of its parents.
        /// </summary>
        /// <param name="itemId">The id of the item to be removed</param>
        public void RemoveItem(string itemId)
        {
            if (_itemStorage.ContainsKey(itemId))
            {
                var cachedItem = _itemStorage[itemId];
                if (_itemStorage.ContainsKey(cachedItem.Item.Parent.Id))
                {
                    var parent = _itemStorage[cachedItem.Item.Parent.Id];
                    parent.Children.RemoveAll(c => c.Item.Id == itemId);
                }

                _itemStorage.Remove(itemId);
            }
        }

        /// <summary>
        /// Adds the root folder to the cache.
        /// </summary>
        /// <param name="folder">The root folder of the drive</param>
        public void AddRootFolder(DriveFolder folder)
        {
            if (_itemStorage.ContainsKey(folder.Id))
            {
                _itemStorage[folder.Id].Item = folder;
            }
            else
            {
                _itemStorage.Add(folder.Id, new CacheItem(folder));
            }

            _rootFolder = folder;
        }

        /// <summary>
        /// Gets the root folder of the drive.
        /// </summary>
        /// <returns>The root folder of the drive</returns>
        public DriveFolder GetRootFolder()
        {
            return _rootFolder;
        }
        
        /// <summary>
        /// Gets the DriveItem from the cache with the given ID if it exists.
        /// </summary>
        /// <typeparam name="T">The type of the desired item</typeparam>
        /// <param name="itemId">The identifier of the desired item</param>
        /// <returns></returns>
        public T GetItem<T>(string itemId) where T : DriveItem
        {
            if (_itemStorage.ContainsKey(itemId))
            {
                var item = _itemStorage[itemId].Item;
                return item as T;
            }

            return null;
        }

        /// <summary>
        /// Gets the children of a DriveItem that are stored in the cache ordered by folders first and according to name.
        /// If the number of children of the folder differs from the number of children in the cache null will be returned.
        /// </summary>
        /// <param name="parentId">The identifier of the parent folder</param>
        /// <returns>A list of children items</returns>
        public List<DriveItem> GetChildren(string parentId)
        {
            if (_itemStorage.ContainsKey(parentId))
            {
                var parent = _itemStorage[parentId];
                var parentItem = (DriveFolder) parent.Item;
                if (parentItem.Properties.ChildCount != parent.Children.Count)
                {
                    return null;
                }

                return parent.Children.Select(c => c.Item)
                    .OrderBy(i => i is DriveFile)
                    .ThenBy(i => i.Name)
                    .ToList();
            }

            return null;
        }

        /// <summary>
        /// Moves a DriveItem into a new Folder in the cache.
        /// </summary>
        /// <param name="itemId">The item's identifier</param>
        /// <param name="targetFolderId">The target folder's identifier</param>
        public void MoveItem(string itemId, string targetFolderId)
        {
            if (!_itemStorage.ContainsKey(itemId))
            {
                return;
            }

            var cacheItem = _itemStorage[itemId];
            
            //Remove the item from its current folder
            if (_itemStorage.ContainsKey(cacheItem.Item.Parent.Id))
            {
                var parentItem = _itemStorage[cacheItem.Item.Parent.Id];

                //Updating the parent item
                var parentFolder = (DriveFolder) parentItem.Item;
                parentFolder.Properties.ChildCount--;

                //Removing the item from its children
                parentItem.Children.RemoveAll(c => c.Item.Id == itemId);
            }

            //Add it to its new folder            
            if (_itemStorage.ContainsKey(targetFolderId))
            {
                var targetFolder = _itemStorage[targetFolderId];

                //Updating the new parent folder
                var folder = (DriveFolder) targetFolder.Item;
                folder.Properties.ChildCount++;

                //Setting the parent reference to the new parent
                cacheItem.Item.Parent.Id = targetFolder.Item.Id;
                cacheItem.Item.Parent.Path = targetFolder.Item.Path;
                targetFolder.Children.Add(cacheItem);
            }
        }

        /// <summary>
        /// Clears all data from the cache.
        /// </summary>
        public void Clear()
        {
            _itemStorage.Clear();
            _rootFolder = null;
        }
    }
}
