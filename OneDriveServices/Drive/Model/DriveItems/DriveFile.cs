using System;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl;
using Newtonsoft.Json;
using OneDriveServices.Authentication;

namespace OneDriveServices.Drive.Model.DriveItems
{
    public class DriveFile : DriveItem
    {
        public int Size { get; set; }

        public async Task<byte[]> DownloadAsync()
        {
            using (var client = new HttpClient())
            {
                var url = new Url(DriveService.BaseUrl)
                    .AppendPathSegments("items", Id, "content");

                var request = new HttpRequestMessage(HttpMethod.Get, url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();

                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                throw new InvalidOperationException();
            }
        }

        protected override void Update(string json)
        {
            var obj = JsonConvert.DeserializeObject<DriveFile>(json);
            UpdateCommonData(obj);
            Size = obj.Size;
        }
    }
}
