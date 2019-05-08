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
        /// <param name="isRetrying">Determines if the method is retrying after an unauthorized response</param>
        /// <returns>The pasted item</returns>
        Task<DriveItem> ExecuteAsync(DriveItem content, DriveFolder target, bool isRetrying = false);
    }
}
