# Dropbox storage for GroupDocs.Viewer .NET 

[Dropbox](https://www.dropbox.com) file storage for [GroupDocs.Viewer for .NET](https://www.nuget.org/packages/groupdocs.viewer)
 allows working with you files in Dropbox cloud storage.
 
## Installation

Install via [nuget.org](http://nuget.org)

```powershell
Install-Package GroupDocs.Viewer.DropboxStorage
```
## How to use

Follow [this](https://blogs.dropbox.com/developers/2014/05/generate-an-access-token-for-your-own-account) guide to retrieve your access key.

```csharp
 
var accessKey = "***";
var client = new DropboxClient(accessKey);
var storage = new DropboxStorage(client);

var viewer = new ViewerHtmlHandler(storage);

var pages = viewer.GetPages("sample.docx");
```

## License

Dropbox storage for GroupDocs.Viewer .NET is Open Source software released under the [MIT license](https://github.com/harumburum/GroupDocs.Viewer.DropboxStorage/blob/master/LICENSE.md).