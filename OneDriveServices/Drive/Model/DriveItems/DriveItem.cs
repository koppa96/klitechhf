﻿using System;
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
    public abstract class DriveItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("lastModifiedDateTime")]
        public DateTime LastModified { get; set; }

        [JsonProperty("parentReference")]
        public ParentReference Parent { get; set; }

        public string Path => Parent.Path + "/" + Name;

        /// <summary>
        /// Tries to load its parent from the local cache. If it doesn't exist then it downloads it from the drive
        /// </summary>
        /// <returns>The parent of the current item</returns>
        public async Task<DriveFolder> GetParentAsync()
        {
            if (Parent.Id == null)
            {
                throw new InvalidOperationException("This is the root folder.");
            }

            var cachedItem = DriveService.Instance.Cache.GetItem(Parent.Id);
            if (cachedItem != null)
            {
                return cachedItem as DriveFolder;
            }

            using (var client = new HttpClient())
            {
                var url = new Url(DriveService.BaseUrl)
                    .AppendPathSegments("items", Parent.Id);

                var request = new HttpRequestMessage(HttpMethod.Get, url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();

                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<DriveFolder>(json);
                }

                throw new WebException(await response.Content.ReadAsStringAsync());
            }
        }

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

                DriveService.Instance.Cache.RemoveItem(this);
            }
        }

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
                    "name", newName
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

        protected abstract void Update(string json);

        protected void UpdateCommonData(DriveItem newItem)
        {
            Id = newItem.Id;
            Name = newItem.Name;
            LastModified = newItem.LastModified;
            Parent = newItem.Parent;
        }
    }
}