using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneDriveServices.Drive.Model.DriveItems;

namespace KlitechHf.Model
{
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
