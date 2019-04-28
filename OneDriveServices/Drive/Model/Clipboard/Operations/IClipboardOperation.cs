using System.Threading.Tasks;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Clipboard.Operations
{
    /// <summary>
    /// An abstract operation that the clipboard can execute.
    /// </summary>
    public interface IClipboardOperation
    {
        /// <summary>
        /// Executes the subclass' implementation of the operation.
        /// </summary>
        /// <param name="content">The subject of the operation</param>
        /// <param name="target">The target folder of the operation</param>
        /// <returns></returns>
        Task ExecuteAsync(DriveItem content, DriveFolder target);
    }
}
