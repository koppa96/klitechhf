# Client side technologies homework
This is a simple API wrapper and UWP client for OneDrive as a homework for the subject called Client Side Technologies: https://www.aut.bme.hu/Course/kliensoldali

## Microsoft Graph API Services
The solution contains a project that provides a wrapper for the Microsoft Graph API for authentication and accessing the drives. The following features were implemented:
 - **Authentication service**
   - Getting and storing your tokens
   - Storing your token for the next login
   - Showing simple login prompt
 - **Drive service**
   - Accessing your DriveItems by ID
   - Accessing the children of your items
   - Accessing the parent of an item
   - Accessing the root folder of your drive
   - Creating folders
   - Uploading files
   - Downloading files
   - Clipboard for copying, and moving items
   - Caching for faster response times
   
I did not implement further features of the API as they were not necessary for the functionality of the app. This API wrapper is not aware of the modifications done to your drive by other apps.

### Samples
After logging in you can access the logged in user's drive. You can get the root folder of the drive like this:
```
DriveFolder rootFolder = await DriveService.Instance.GetRootAsync();
```

You can then list the children of your folder like this:
```
List<DriveItem> children = await rootFolder.GetChildrenAsync(); // Gets the children from the cache if they are in there
children = await rootFolder.LoadChildrenAsync(); // Force-loads the children from the server if you don't want to use cache
```

You can also get an item with a specific ID:
```
DriveFile file = await DriveService.Instance.GetItemAsync<DriveFile>("FILE_ID"); // You can force-load from server by LoadItemAsync<>()
```

If you want to download a file you just have to do this:
```
byte[] content = await file.DownloadAsync();
```

To delete an item do this:
```
DriveItem item = await DriveService.Instance.GetItemAsync("ITEM_ID");
await item.DeleteAsync(); //This removes the file both from your drive and the local cache
```

To rename an item do this:
```
await item.RenameAsync("NewName.txt");
```

## OneDrive UWP Client
The solution also contains a UWP client that uses the API wrapper. The app provides all the functionality mentioned above.
This project uses Prism as an MVVM Framework.
