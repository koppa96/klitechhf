using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OneDriveServices.Drive.Model
{
    public abstract class DriveItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("lastModifiedDateTime")]
        public DateTime LastModified { get; set; }
    }
}
