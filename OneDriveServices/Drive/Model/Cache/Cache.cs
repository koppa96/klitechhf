using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.ObjectBuilder2;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Cache
{
    public class Cache
    {
        private Dictionary<string, CacheItem> _itemStorage;

        public Cache()
        {
            _itemStorage = new Dictionary<string, CacheItem>();
        }

        public void AddItem(DriveItem item, List<DriveItem> children = null)
        {
            _itemStorage.Add(item.Id, new CacheItem
            {
                Item = item,
                Children = children
            }); 

            UpdateItems(children);
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

        public DriveItem GetItem(string id)
        {
            if (_itemStorage.ContainsKey(id))
            {
                return _itemStorage[id].Item;
            }

            return null;
        }

        public List<DriveItem> GetChildrenOf(string id)
        {
            if (_itemStorage.ContainsKey(id))
            {
                return _itemStorage[id].Children;
            }

            return null;
        }

        public void Clear()
        {
            _itemStorage = new Dictionary<string, CacheItem>();
        }

        public void RemoveItem(DriveItem item)
        {
            _itemStorage.Remove(item.Id);
            _itemStorage[item.Parent.Id].Children.Remove(item);
        }

        public void UpdateItem(DriveItem item)
        {
            _itemStorage[item.Id].Item = item;
        }
    }
}
