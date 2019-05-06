using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneDriveServices.Drive.Model.DriveItems;

namespace KlitechHf.Model
{
    /// <summary>
    /// An item that represents a reference to the parent of the current folder.
    /// </summary>
    public class ParentItem : DriveItem
    {
        protected override void Update(string json)
        {
            throw new NotImplementedException();
        }
    }
}
