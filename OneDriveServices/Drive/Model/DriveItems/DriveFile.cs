using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Flurl;
using Newtonsoft.Json;
using OneDriveServices.Authentication;

namespace OneDriveServices.Drive.Model.DriveItems
{
    public class DriveFile : DriveItem
    {
        [JsonProperty(PropertyName = "size")]
        public int Size { get; set; }

        /// <summary>
        /// Asynchronously downloads the file.
        /// </summary>
        /// <param name="file">The target StorageFile into which the content will be downloaded</param>
        /// <param name="isRetrying">Determines if the method is retrying after an unauthorized response</param>
        /// <returns>The bytes of the file</returns>
        public async Task DownloadAsync(StorageFile file, bool isRetrying = false)
        {
            using (var client = new HttpClient())
            {
                var url = new Url(DriveService.BaseUrl)
                    .AppendPathSegments("items", Id, "content");

                var request = new HttpRequestMessage(HttpMethod.Get, url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();

                var response = await Task.Run(() => client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead));
                if (response.IsSuccessStatusCode)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var outputStream = await file.OpenStreamForWriteAsync())
                        {
                            await stream.CopyToAsync(outputStream);
                            return;
                        }
                    }
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized && !isRetrying)
                {
                    await AuthService.Instance.LoginAsync();
                    await DownloadAsync(file, true);
                }

                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Creates a new DriveFile from the given JSON and updates its own content with it.
        /// </summary>
        /// <param name="json">The JSON representation of the file</param>
        protected override void Update(string json)
        {
            var obj = JsonConvert.DeserializeObject<DriveFile>(json);
            UpdateCommonData(obj);
            Size = obj.Size;
        }
    }
}
