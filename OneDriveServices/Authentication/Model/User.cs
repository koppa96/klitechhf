using Newtonsoft.Json;

namespace OneDriveServices.Authentication.Model
{
    /// <summary>
    /// Data class containing some information about the current user.
    /// </summary>
    public class User
    {
        [JsonProperty("id")]
        public string Uid { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("surname")]
        public string Surname { get; set; }

        [JsonProperty("givenName")]
        public string FirstName { get; set; }

        [JsonProperty("userPrincipalName")]
        public string Email { get; set; }
    }
}
