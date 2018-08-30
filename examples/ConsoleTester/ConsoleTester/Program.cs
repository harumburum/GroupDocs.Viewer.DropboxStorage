using System;
using System.IO;
using System.Text;
using Dropbox.Api;
using Dropbox.Api.Files;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Dropbox;
using GroupDocs.Viewer.Handler;

namespace ConsoleTester
{
    static class Program
    {
        static void Main(string[] args)
        {
            //TODO: Follow next guide to get access key -
            //      https://blogs.dropbox.com/developers/2014/05/generate-an-access-token-for-your-own-account/
            var accessKey = "***";

            var client = new DropboxClient(accessKey);
            var fileName = "sample.txt";

            UploadTestFile(fileName, client);
            RenderTestFile(fileName, client);
            DeleteTestFile(fileName, client);

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        static void RenderTestFile(string fileName, DropboxClient client)
        {
            var storage = new DropboxStorage(client);

            var config = new ViewerConfig
            {
                EnableCaching = true
            };
            var viewer = new ViewerHtmlHandler(config, storage);

            var pages = viewer.GetPages(fileName);
            foreach (var page in pages)
                Console.WriteLine(page.HtmlContent);

            viewer.ClearCache(fileName);
        }

        static void UploadTestFile(string fileName, DropboxClient client)
        {
            var fileContent = "Hello, World!";
            var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

            client.Files.UploadAsync
                ("/" + fileName, WriteMode.Overwrite.Instance, body: fileStream).Wait();
        }

        static void DeleteTestFile(string fileName, DropboxClient client)
        {
            client.Files.DeleteV2Async("/" + fileName).Wait();
        }
    }
}
