using Newtonsoft.Json;

namespace OneDriveServices.Drive.Model.Clipboard
{
    public class DriveOperation
    {
        [JsonProperty(PropertyName = "operation")]
        public string Operation { get; set; }
        
        [JsonProperty(PropertyName = "percentageComplete")]
        public double Percentage { get; set; }

        [JsonProperty(PropertyName = "resourceId")]
        public string ResourceId { get; set; }
        
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
    }
}