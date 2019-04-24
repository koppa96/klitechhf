using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneDriveServices.Authentication;

namespace OneDriveServices.Drive.Model.DriveItems
{
    public class DriveFolder : DriveItem
    {
        [JsonProperty("folder/childCount")]
        public int ChildCount { get; set; }

        public async Task<List<DriveItem>> GetChildrenAsync()
        {
            using (var client = new HttpClient())
            {
                var url = new Url(DriveService.BaseUrl)
                    .AppendPathSegments("items", Id, "children");

                var request = new HttpRequestMessage(HttpMethod.Get, url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();

                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JObject.Parse(json);

                    var children = result["value"];
                    return children.Select(c => c["folder"] == null ? c.ToObject<DriveFile>() as DriveItem 
                            : c.ToObject<DriveFolder>() as DriveItem).ToList();
                }

                throw new WebException(await response.Content.ReadAsStringAsync());
            }
        }

        protected override void Update(string json)
        {
            var obj = JsonConvert.DeserializeObject<DriveFolder>(json);
            UpdateCommonData(obj);
            ChildCount = obj.ChildCount;
        }
    }
}
