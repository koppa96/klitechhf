using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OneDriveServices.Authentication;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Clipboard
{
    public class MoveOperation : IClipboardOperation
    {
        public async Task ExecuteAsync(DriveItem content, DriveFolder target)
        {
            using (var client = new HttpClient())
            {
                var url = new Url(DriveService.BaseUrl)
                    .AppendPathSegments("items", content.Id);

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();

                var requestContent = new ClipboardItem
                {
                    Name = content.Name,
                    ParentReference = new ParentReference
                    {
                        Id = target.Id
                    }
                };
                var json = JsonConvert.SerializeObject(requestContent);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    DriveService.Instance.Cache.MoveItem(content.Id, target.Id);
                    return;
                }

                throw new WebException(await response.Content.ReadAsStringAsync());
            }
        }
    }
}
