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
    /// Stores the DriveItems so that the don't need to be downloaded every time
    /// </summary>
    public class Cache
    {
        private Dictionary<string, CacheItem> _itemStorage;
        private DriveFolder _rootFolder;

        public Cache()
        {
            _itemStorage = new Dictionary<string, CacheItem>();
        }

        public void AddItem(DriveItem item)
        {
            //Adding the item to the cache
            CacheItem cachedItem;
            if (_itemStorage.ContainsKey(item.Id))
            {
                cachedItem = _itemStorage[item.Id];
                cachedItem.Item = item;
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
            }
        }

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

        public DriveFolder GetRootFolder()
        {
            return _rootFolder;
        }
        
        public T GetItem<T>(string itemId) where T : DriveItem
        {
            if (_itemStorage.ContainsKey(itemId))
            {
                var item = _itemStorage[itemId].Item;
                return item as T;
            }

            return null;
        }

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
                _itemStorage[cacheItem.Item.Parent.Id].Children.RemoveAll(c => c.Item.Id == itemId);
            }

            //Add it to its new folder            
            if (_itemStorage.ContainsKey(targetFolderId))
            {
                var targetFolder = _itemStorage[targetFolderId];
                var folder = targetFolder.Item as DriveFolder;
                folder.Properties.ChildCount++;
                cacheItem.Item.Parent.Id = targetFolder.Item.Id;
                cacheItem.Item.Parent.Path = targetFolder.Item.Path;
                targetFolder.Children.Add(cacheItem);
            }
        }

        public void Clear()
        {
            _itemStorage.Clear();
            _rootFolder = null;
        }
    }
}
