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
        [JsonProperty(PropertyName = "folder")]
        public FolderInfo Properties { get; set; }

        public async Task<List<DriveItem>> GetChildrenAsync()
        {
            var cachedItems = DriveService.Instance.Cache.GetChildren(Id);
            if (cachedItems != null)
            {
                return cachedItems;
            }

            return await LoadChildrenAsync();
        }

        public async Task<List<DriveItem>> LoadChildrenAsync()
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
                    var childList = children.Select(c => c["folder"] == null ? c.ToObject<DriveFile>() as DriveItem
                        : c.ToObject<DriveFolder>() as DriveItem).ToList();

                    foreach (var child in childList)
                    {
                        DriveService.Instance.Cache.AddItem(child);
                    }
                    
                    return childList;
                }

                throw new WebException(await response.Content.ReadAsStringAsync());
            }
        }

        protected override void Update(string json)
        {
            var obj = JsonConvert.DeserializeObject<DriveFolder>(json);
            UpdateCommonData(obj);
            Properties = obj.Properties;
        }
    }
}
