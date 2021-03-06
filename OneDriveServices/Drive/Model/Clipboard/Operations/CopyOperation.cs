﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Newtonsoft.Json;
using OneDriveServices.Authentication;
using OneDriveServices.Drive.Model.DriveItems;

namespace OneDriveServices.Drive.Model.Clipboard.Operations
{
    public class CopyOperation : IClipboardOperation
    {
        /// <summary>
        /// Executes a copy operation awaits it and reloads the target folder into the cache.
        /// </summary>
        /// <param name="content">The item to be moved into the folder</param>
        /// <param name="target">The target folder</param>
        /// <param name="isRetrying">Determines if the method is retrying after an unauthorized response</param>
        /// <exception cref="TaskCanceledException">Throws exception upon canceling the monitoring task</exception>
        /// <returns>The pasted item</returns>
        public async Task<DriveItem> ExecuteAsync(DriveItem content, DriveFolder target, bool isRetrying)
        {
            using (var client = new HttpClient())
            {
                var url = new Url(DriveService.BaseUrl)
                    .AppendPathSegments("items", content.Id, "copy");

                var request = new HttpRequestMessage(HttpMethod.Post, url.ToUri());
                request.Headers.Authorization = AuthService.Instance.CreateAuthenticationHeader();

                var requestContent = new ClipboardItem
                {
                    Name = content.Name,
                    ParentReference = new ParentReference
                    {
                        DriveId = DriveService.Instance.DriveId,
                        Id = target.Id
                    }
                };
                var json = JsonConvert.SerializeObject(requestContent);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await Task.Run(() => client.SendAsync(request));
                if (response.IsSuccessStatusCode)
                {
                    var operationUri = response.Headers.Location;

                    // Adding cancellation token for the operation
                    var tokenSource = new CancellationTokenSource();
                    DriveService.Instance.CurrentOperations.Add(tokenSource);


                    // Waiting for the copying to end
                    var result = await DriveService.AwaitOperationAsync(operationUri, tokenSource.Token);

                    // Removing the cancellation token as the operation already ended
                    DriveService.Instance.CurrentOperations.Remove(tokenSource);

                    //This reloads the target folder into the cache to update its values
                    await DriveService.Instance.LoadItemAsync<DriveFolder>(target.Id);

                    // This results the item to be updated in the cache
                    return await DriveService.Instance.GetItemAsync(result.ResourceId);
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized && !isRetrying)
                {
                    await AuthService.Instance.LoginAsync();
                    return await ExecuteAsync(content, target, true);
                }
                
                throw new WebException(await response.Content.ReadAsStringAsync());
            }
        }
    }
}
