using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Newtonsoft.Json;
using OneDriveServices.Authentication;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Clipboard
{
    public class MoveOperation : IClipboardOperation
    {
        public async Task ExecuteAsync(DriveItem content, string parentId)
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
                        Id = parentId
                    }
                };
                var json = JsonConvert.SerializeObject(requestContent);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await Task.Run(() => client.SendAsync(request));
                if (!response.IsSuccessStatusCode)
                {
                    throw new WebException(await response.Content.ReadAsStringAsync());
                }

                DriveService.Instance.Children.Remove(content);
            }
        }
    }
}
