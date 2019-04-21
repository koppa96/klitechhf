using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KlitechHf.Model
{
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
