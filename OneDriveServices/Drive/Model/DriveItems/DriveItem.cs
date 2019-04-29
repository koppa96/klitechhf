using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneDriveServices.Authentication;

namespace OneDriveServices.Drive.Model.DriveItems
{
    /// <summary>
    /// An abstract class for the common data and behaviour of the Drive Items.
    /// </summary>
    public abstract class DriveItem
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "lastModifiedDateTime")]
        public DateTime LastModified { get; set; }

        [JsonProperty(PropertyName = "parentReference")]
        public ParentReference Parent { get; set; }

        public string Path => (Parent.Path == null ? "/drive" : "") + Url.Decode(Parent.Path, false) + "/" + Name;//Parent.Path + "/" + Name;

        /// <summary>
        /// Tries to load its parent from the local cache. If it doesn't exist then it downloads it from the drive.
        /// </summary>
        /// <returns>The parent of the current item</returns>
        public async Task<DriveFolder> GetParentAsync()
        {
            if (Parent.Id == null)
            {
                throw new InvalidOperationException("This is the root folder.");
            }

            return await DriveService.Instance.GetItemAsync<DriveFolder>(Parent.Id);
        }

        /// <summary>
        /// Deletes the item from the drive and removes itself from the cache.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task DeleteAsync()
        {
            using (var client = new HttpClient())
            {
                var url = new Url(DriveService.BaseUrl)
                    .AppendPathSegments("items", Id);

                var request = new HttpRequestMessage(HttpMethod.Delete, url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();

                var response = await Task.Run(() => client.SendAsync(request));
                if (!response.IsSuccessStatusCode)
                {
                    throw new WebException(await response.Content.ReadAsStringAsync());
                }

                DriveService.Instance.Cache.RemoveItem(Id);
            }
        }

        /// <summary>
        /// Renames the item on the server and updates its data locally.
        /// </summary>
        /// <param name="newName">The new name of the item</param>
        /// <returns>A task representing the operation</returns>
        public async Task RenameAsync(string newName)
        {
            using (var client = new HttpClient())
            {
                var url = new Url(DriveService.BaseUrl)
                    .AppendPathSegments("items", Id);

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();
                
                var content = new JObject
                {
                    { "name", newName }
                };
                request.Content = new StringContent(content.ToString(), Encoding.UTF8, "application/json");

                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Update(json);
                }
            }
        }

        /// <summary>
        /// Updates the item's data from a JSON
        /// </summary>
        /// <param name="json">The JSON representation of the item</param>
        protected abstract void Update(string json);

        /// <summary>
        /// Updates the common data from the given DriveItem
        /// </summary>
        /// <param name="newItem">The DriveItem containing the new data</param>
        protected void UpdateCommonData(DriveItem newItem)
        {
            Id = newItem.Id;
            Name = newItem.Name;
            LastModified = newItem.LastModified;
            Parent = newItem.Parent;
        }

        /// <summary>
        /// Deserializes a DriveItem subclass instance from the given json by guessing the type of the item
        /// </summary>
        /// <param name="json">The JSON representation of the item</param>
        /// <returns></returns>
        public static DriveItem Deserialize(string json)
        {
            var obj = JObject.Parse(json);
            if (obj["folder"] != null)
            {
                return obj.ToObject<DriveFolder>();
            }

            if (obj["file"] != null)
            {
                return obj.ToObject<DriveFile>();
            }

            throw new ArgumentOutOfRangeException(nameof(json), "Unknown item type.");
        }
    }
}
