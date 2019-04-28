using Newtonsoft.Json;

namespace OneDriveServices.Drive.Model.DriveItems
{
    /// <summary>
    /// Represents the parent of the current item.
    /// </summary>
    public class ParentReference
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("driveId")]
        public string DriveId { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
