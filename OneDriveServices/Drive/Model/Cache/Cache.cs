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
            _itemStorage.Add(item.Id, new CacheItem
            {
                Item = item
            });
        }

        public void AddChildrenToItem(string id, List<DriveItem> children)
        {
            _itemStorage[id].Children = children;
            UpdateItems(children);
        }

        private void UpdateItems(IEnumerable<DriveItem> items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var child in items)
            {
                if (_itemStorage.ContainsKey(child.Id))
                {
                    _itemStorage[child.Id].Item = child;
                }
                else
                {
                    AddItem(child);
                }
            }
        }

        /// <summary>
        /// Gets the cached DriveItem with the given ID
        /// </summary>
        /// <param name="id">The identifier of the DriveItem</param>
        /// <returns>The DriveItem</returns>
        public DriveItem GetItem(string id)
        {
            if (_itemStorage.ContainsKey(id))
            {
                return _itemStorage[id].Item;
            }

            return null;
        }

        /// <summary>
        /// Gets the children of the DriveFolder with the given ID
        /// </summary>
        /// <param name="id">The identifier of the folder</param>
        /// <returns></returns>
        public List<DriveItem> GetChildrenOf(string id)
        {
            if (_itemStorage.ContainsKey(id))
            {
                return _itemStorage[id].Children;
            }

            return null;
        }

        /// <summary>
        /// Removes all the data from the cache
        /// </summary>
        public void Clear()
        {
            _itemStorage = new Dictionary<string, CacheItem>();
            _rootFolder = null;
        }

        /// <summary>
        /// Removes the DriveItem from the Cache
        /// </summary>
        /// <param name="item">The DriveItem to be removed</param>
        public void RemoveItem(DriveItem item)
        {
            _itemStorage.Remove(item.Id);
            _itemStorage[item.Parent.Id].Children.Remove(item);
        }

        public void UpdateItem(DriveItem item)
        {
            _itemStorage[item.Id].Item = item;
        }

        public void AddRootFolder(DriveFolder folder)
        {
            _rootFolder = folder;
            AddItem(folder);
        }

        public DriveFolder GetRootFolder()
        {
            return _rootFolder;
        }

        public void AppendChild(string id, DriveItem item)
        {
            if (_itemStorage[id].Children == null)
            {
                _itemStorage[id].Children = new List<DriveItem> { item };
            }
            else
            {
                _itemStorage[id].Children.Add(item);
            }
        }
    }
}
