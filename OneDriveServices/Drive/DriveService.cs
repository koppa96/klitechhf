using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Flurl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneDriveServices.Authentication;
using OneDriveServices.Drive.Model;
using OneDriveServices.Drive.Model.Cache;
using OneDriveServices.Drive.Model.Clipboard;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive
{
    public class DriveService
    {
        public const string BaseUrl = "https://graph.microsoft.com/v1.0/me/drive";

        public static DriveService Instance { get; set; } = new DriveService();
        public DriveFolder CurrentFolder { get; set; }
        public List<DriveItem> Children { get; set; }
        public Clipboard ClipBoard { get; set; }
        public string DriveId { get; set; }
        public Cache Cache { get; set; }

        protected DriveService()
        {
            Cache = new Cache();
        }

        /// <summary>
        /// Loads the root folder of the drive and its children
        /// </summary>
        /// <returns>A task representing the loading</returns>
        public async Task InitializeAsync()
        {
            Cache.Clear();
            using (var client = new HttpClient())
            {
                var url = new Url(BaseUrl)
                    .AppendPathSegment("root");

                var request = new HttpRequestMessage(HttpMethod.Get, url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();

                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    CurrentFolder = JsonConvert.DeserializeObject<DriveFolder>(json);
                    Cache.AddItem(CurrentFolder);

                    Children = await CurrentFolder.GetChildrenAsync();
                    Cache.AddChildrenToItem(CurrentFolder.Id, Children);

                    DriveId = CurrentFolder.Parent.DriveId;

                    return;
                }

                throw new WebException("Couldn't get the root folder.");
            }
        }

        /// <summary>
        /// Navigates to the parent directory and loads the content of the directory
        /// </summary>
        /// <returns>A task representing the operation</returns>
        public async Task NavigateUp()
        {
            CurrentFolder = await CurrentFolder.GetParentAsync();
            Children = await CurrentFolder.GetChildrenAsync();
        }

        /// <summary>
        /// Sets the current folder to the selected child folder and loads its content
        /// </summary>
        /// <param name="id">The identifier of the child folder</param>
        /// <exception cref="InvalidOperationException">Throws exception if there is no child with the given id or if the child is not a folder</exception>
        /// <returns>A task representing the operation</returns>
        public async Task NavigateChild(string id)
        {
            var child = Children.Single(c => c.Id == id);

            if (child is DriveFolder folder)
            {
                CurrentFolder = folder;
                Children = await CurrentFolder.GetChildrenAsync();
                return;
            }

            throw new InvalidOperationException("You can only navigate to folders.");
        }

        /// <summary>
        /// Creates a folder as a child of the current folder and adds it to the list of children
        /// </summary>
        /// <param name="name">The name of the folder to be created</param>
        /// <returns></returns>
        public async Task CreateFolderAsync(string name)
        {
            if (Children.Any(c => c.Name == name))
            {
                throw new InvalidOperationException("There is already a child with that name.");
            }

            using (var client = new HttpClient())
            {
                var url = new Url(BaseUrl)
                    .AppendPathSegments("items", CurrentFolder.Id, "children");

                var request = new HttpRequestMessage(HttpMethod.Post, url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();

                var content = new JObject
                {
                    { "name", name },
                    { "folder", new JObject() },
                    { "@microsoft.graph.conflictBehaviour", "fail" }
                };
                request.Content = new StringContent(content.ToString(), Encoding.UTF8, "application/json");

                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var folder = JsonConvert.DeserializeObject<DriveFolder>(json);
                    Children.Add(folder);

                    Children = Children.OrderBy(c => c is DriveFile).ThenBy(c => c.Name).ToList();
                }

                throw new WebException(await response.Content.ReadAsStringAsync());
            }
        }

        public async Task UploadAsync(string filename, byte[] content)
        {
            if (Children.Any(c => c.Name == filename))
            {
                throw new InvalidOperationException("There is already a child with that name.");
            }

            using (var client = new HttpClient())
            {
                var url = new Url(BaseUrl)
                    .AppendPathSegments("items", $"{CurrentFolder.Id}:", $"{filename}:", "content");

                var request = new HttpRequestMessage(HttpMethod.Put, url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();
                request.Content = new ByteArrayContent(content);

                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var file = JsonConvert.DeserializeObject<DriveFile>(json);

                    Children.Add(file);
                    Cache.AddItem(file);
                }
            }
        }

        public async Task RefreshContent()
        {
            Children = await CurrentFolder.LoadChildrenAsync();
        }
    }
}
