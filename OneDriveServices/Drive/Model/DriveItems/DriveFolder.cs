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

        /// <summary>
        /// Tries to load the folder's children from the cache. If they are not there or not all of them are there they will be downloaded.
        /// </summary>
        /// <returns>The list of children of the item</returns>
        public async Task<List<DriveItem>> GetChildrenAsync()
        {
            var cachedItems = DriveService.Instance.Cache.GetChildren(Id);
            if (cachedItems != null)
            {
                return cachedItems;
            }

            return await LoadChildrenAsync();
        }

        /// <summary>
        /// Loads the children of the item from the server and adds the downloaded items to the cache.
        /// </summary>
        /// <param name="isRetrying">Determines if the method is retrying after an unauthorized response</param>
        /// <returns>The list of children of the item</returns>
        public async Task<List<DriveItem>> LoadChildrenAsync(bool isRetrying = false)
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
                    var childList = children.Select(c => Deserialize(c.ToString())).Where(i => i != null).ToList();

                    foreach (var child in childList)
                    {
                        DriveService.Instance.Cache.AddItem(child);
                    }
                    
                    return childList;
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized && !isRetrying)
                {
                    await AuthService.Instance.LoginAsync();
                    return await LoadChildrenAsync(true);
                }

                throw new WebException(await response.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// Creates a new DriveFolder from the given JSON and updates its data with it.
        /// </summary>
        /// <param name="json">The JSON representation of the folder</param>
        protected override void Update(string json)
        {
            var obj = JsonConvert.DeserializeObject<DriveFolder>(json);
            UpdateCommonData(obj);
            Properties = obj.Properties;
        }
    }
}
