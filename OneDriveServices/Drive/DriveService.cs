using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl;
using Newtonsoft.Json;
using OneDriveServices.Authentication;
using OneDriveServices.Drive.Model;

namespace OneDriveServices.Drive
{
    public class DriveService
    {
        public const string BaseUrl = "https://graph.microsoft.com/v1.0/me/drive";

        public static DriveService Instance { get; set; } = new DriveService();
        public DriveFolder CurrentFolder { get; set; }
        public List<DriveItem> Children { get; set; }

        protected DriveService()
        {
        }

        /// <summary>
        /// Loads the root folder of the drive and its children
        /// </summary>
        /// <returns>A task representing the loading</returns>
        public async Task Initialize()
        {
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
                    Children = await CurrentFolder.GetChildrenAsync();
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
            }

            throw new InvalidOperationException("You can only navigate to folders.");
        }
    }
}
