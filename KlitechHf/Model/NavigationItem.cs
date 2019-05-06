using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneDriveServices.Drive.Model.DriveItems;

namespace KlitechHf.Model
{
    /// <summary>
    /// An item for the navigation list. It holds its DriveItem's ID and Name for navigation purposes.
    /// </summary>
    public class NavigationItem
    {
        public string Name { get; set; }
        public string Id { get; set; }

        public NavigationItem()
        {
        }

        public NavigationItem(DriveItem item)
        {
            Name = item.Name;
            Id = item.Id;
        }
    }
}
