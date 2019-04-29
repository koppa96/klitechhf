using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Flurl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneDriveServices.Authentication;
using OneDriveServices.Drive.Model;
using OneDriveServices.Drive.Model.Cache;
using OneDriveServices.Drive.Model.Clipboard;
using OneDriveServices.Drive.Model.Clipboard.Operations;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive
{
    /// <summary>
    /// A Singleton service for accessing the drive of the currently logged in user of the AuthService.
    /// </summary>
    public class DriveService
    {
        public const string BaseUrl = "https://graph.microsoft.com/v1.0/me/drive";
        private const int ChunkSize = 62914560;

        private static DriveService _instance;
        public static DriveService Instance => _instance ?? (_instance = new DriveService());

        public Clipboard ClipBoard { get; }
        public string DriveId { get; private set; }
        public Cache Cache { get; }
        public List<CancellationTokenSource> CurrentOperations { get; }

        protected DriveService()
        {
            Cache = new Cache();
            ClipBoard = new Clipboard();
            AuthService.Instance.UserLoggedOut += OnUserLogout;
            CurrentOperations = new List<CancellationTokenSource>();
        }

        /// <summary>
        /// Cancels the monitoring of the current user's asynchronous operations and clears the service's data
        /// </summary>
        private void OnUserLogout()
        {
            foreach (var operation in CurrentOperations)
            {
                operation.Cancel();
            }

            Cache.Clear();
            ClipBoard.Clear();
            DriveId = null;
        }

        /// <summary>
        /// Tries to get the root folder of the drive from the cache. If it doesn't exist it will be loaded from the server.
        /// </summary>
        /// <returns>The root folder of the drive</returns>
        public async Task<DriveFolder> GetRootAsync()
        {
            var cachedFolder = Cache.GetRootFolder();
            if (cachedFolder != null)
            {
                return cachedFolder;
            }

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
                    var root = JsonConvert.DeserializeObject<DriveFolder>(json);
                    Cache.AddRootFolder(root);

                    DriveId = root.Parent.DriveId;

                    return root;
                }

                throw new WebException("Couldn't get the root folder.");
            }
        }

        /// <summary>
        /// Tries to load the item from the cache, and if it's not there it loads from the server. Only use this if you have no type information.
        /// </summary>
        /// <param name="id">The identifier of the item</param>
        /// <returns>The item</returns>
        public async Task<DriveItem> GetItemAsync(string id)
        {
            var cachedItem = Cache.GetItem<DriveItem>(id);
            return cachedItem ?? await LoadItemAsync(id);
        }

        /// <summary>
        /// Tries to load the item from the cache, and if it's not there it loads from the server
        /// </summary>
        /// <typeparam name="T">The type of the desired DriveItem</typeparam>
        /// <param name="id">The identifier of the item</param>
        /// <returns>The item</returns>
        public async Task<T> GetItemAsync<T>(string id) where T : DriveItem
        {
            var cachedItem = Cache.GetItem<T>(id);
            return cachedItem ?? await LoadItemAsync<T>(id);
        }

        /// <summary>
        /// Loads the desired item from the server if the type of the item is not known
        /// </summary>
        /// <param name="id">The identifier of the item</param>
        /// <returns>The item</returns>
        public async Task<DriveItem> LoadItemAsync(string id)
        {
            using (var client = new HttpClient())
            {
                var url = new Url(BaseUrl)
                    .AppendPathSegments("items", id);

                var request = new HttpRequestMessage(HttpMethod.Get, url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();

                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var item = DriveItem.Deserialize(json);

                    if (item != null)
                    {
                        Cache.AddItem(item);
                    }

                    return item;
                }

                throw new WebException(await response.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// Loads the desired item from the server
        /// </summary>
        /// <typeparam name="T">The type of the desired DriveItem</typeparam>
        /// <param name="id">The identifier of the item</param>
        /// <returns></returns>
        public async Task<T> LoadItemAsync<T>(string id) where T : DriveItem
        {
            using (var client = new HttpClient())
            {
                var url = new Url(BaseUrl)
                    .AppendPathSegments("items", id);

                var request = new HttpRequestMessage(HttpMethod.Get, url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();

                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var item = JsonConvert.DeserializeObject<T>(json);

                    Cache.AddItem(item);
                    return item;
                }

                throw new WebException(await response.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// Creates a folder as a child of the target folder and adds it to the list of children
        /// </summary>
        /// <param name="parent">The parent of the newly created folder</param>
        /// <param name="name">The name of the folder to be created</param>
        /// <returns></returns>
        public async Task<DriveFolder> CreateFolderAsync(DriveFolder parent, string name)
        {
            using (var client = new HttpClient())
            {
                var url = new Url(BaseUrl)
                    .AppendPathSegments("items", parent.Id, "children");

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
                    Cache.AddItem(folder);

                    return folder;
                }

                throw new WebException(await response.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// Uploads a file with the given name and the given content to the current folder
        /// </summary>
        /// <param name="parent">The parent folder of the new file</param>
        /// <param name="filename">The name of the file</param>
        /// <param name="content">The content of the file</param>
        /// <returns>The uploaded file</returns>
        public async Task<DriveFile> UploadAsync(DriveFolder parent, string filename, byte[] content)
        {
            using (var client = new HttpClient())
            {
                var url = new Url(BaseUrl)
                    .AppendPathSegments("items", $"{parent.Id}:", $"{filename}:", "createUploadSession");

                var request = new HttpRequestMessage(HttpMethod.Post, url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();

                // Starting the download session
                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    var obj = JObject.Parse(await response.Content.ReadAsStringAsync());
                    var uploadUrl = obj["uploadUrl"].ToString();

                    // Calculating the amount of chunks to be sent
                    var intermediateChunks = content.Length % ChunkSize == 0 ? 
                        content.Length / ChunkSize - 1 : content.Length / ChunkSize;

                    // Sending all the intermediate chunk
                    int i;
                    for (i = 0; i < intermediateChunks; i++)
                    {
                        await SendChunkAsync(client, uploadUrl, content, i);
                    }

                    // Sending the last chunk and returning its result
                    return await SendLastChunkAsync(client, uploadUrl, content, i);
                }

                throw new WebException(await response.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// Sends a chunk of data with ChunkSize length to the target url.
        /// </summary>
        /// <param name="client">The HTTP client to send the chunk</param>
        /// <param name="uploadUrl">The target URL</param>
        /// <param name="content">The content to be sent</param>
        /// <param name="offset">The offset of the sent bytes</param>
        /// <returns>A task representing the operation</returns>
        private async Task SendChunkAsync(HttpClient client, string uploadUrl, byte[] content, int offset)
        {
            var uploadRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
            uploadRequest.Content = new ByteArrayContent(content, offset * ChunkSize, ChunkSize);
            uploadRequest.Content.Headers.ContentRange = new ContentRangeHeaderValue(offset * ChunkSize, (offset + 1) * ChunkSize - 1, content.Length);

            var uploadResponse = await Task.Run(() => client.SendAsync(uploadRequest));
            if (!uploadResponse.IsSuccessStatusCode)
            {
                throw new WebException(await uploadResponse.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// Sends the last part of the data and gets the uploaded file
        /// </summary>
        /// <param name="client">The HTTP client to send the chunk</param>
        /// <param name="uploadUrl">The target URL</param>
        /// <param name="content">The content to be sent</param>
        /// <param name="offset">The offset of the sent bytes</param>
        /// <returns>The uploaded file</returns>
        private async Task<DriveFile> SendLastChunkAsync(HttpClient client, string uploadUrl, byte[] content, int offset)
        {
            var lastRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
            lastRequest.Content = new ByteArrayContent(content, offset * ChunkSize, content.Length - offset * ChunkSize);
            lastRequest.Content.Headers.ContentRange = new ContentRangeHeaderValue(offset * ChunkSize, content.Length - 1, content.Length);

            var lastResponse = await Task.Run(() => client.SendAsync(lastRequest));
            if (lastResponse.IsSuccessStatusCode)
            {
                var json = await lastResponse.Content.ReadAsStringAsync();
                var file = JsonConvert.DeserializeObject<DriveFile>(json);

                Cache.AddItem(file);
                return file;
            }

            throw new WebException(await lastResponse.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Places an item to the clipboard with a copy operation. The item will be copied when PasteAsync is called.
        /// </summary>
        /// <param name="item">The item to be copied</param>
        public void Copy(DriveItem item)
        {
            ClipBoard.Content = item;
            ClipBoard.Operation = new CopyOperation();
        }

        /// <summary>
        /// Places an item to the clipboard with a move operation. The item will be moved when PasteAsync is called.
        /// </summary>
        /// <param name="item">The DriveItem to be placed on the clipboard</param>
        public void Cut(DriveItem item)
        {
            ClipBoard.Content = item;
            ClipBoard.Operation = new MoveOperation();
        }

        /// <summary>
        /// Executes the clipboard operation
        /// </summary>
        /// <param name="targetFolder">The target folder of the operation</param>
        /// <returns>A task representing the operation</returns>
        public async Task<DriveItem> PasteAsync(DriveFolder targetFolder)
        {
            var task = ClipBoard.ExecuteAsync(targetFolder);
            ClipBoard.Clear();

            return await task;
        }

        /// <summary>
        /// Awaits the asynchronous drive operation (e.g. copying) by using a background thread to query if it has finished
        /// </summary>
        /// <param name="operationUri">The uri of the asynchronous operation</param>
        /// <param name="token">A token to cancel the operation</param>
        /// <returns>A task representing the waiting</returns>
        public static async Task<DriveOperation> AwaitOperationAsync(Uri operationUri, CancellationToken token = default)
        {
            return await Task.Run(async () =>
            {
                using (var client = new HttpClient())
                {
                    while (true)
                    {
                        var response = await client.GetAsync(operationUri, token);
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var progress = JsonConvert.DeserializeObject<DriveOperation>(json);
                            if (progress.Status == "completed")
                            {
                                return progress;
                            }

                            await Task.Delay(1000, token);
                        }
                        else
                        {
                            throw new WebException(await response.Content.ReadAsStringAsync());
                        }
                    }
                }
            }, token);
        }
    }
}
