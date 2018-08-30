using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Dropbox.Api;
using Dropbox.Api.Files;
using NUnit.Framework;

namespace GroupDocs.Viewer.Dropbox.Tests
{
    [TestFixture]
    public class DropboxStorageTests
    {
        private DropboxClient _dropboxClient;
        private DropboxStorage _dropboxStorage;
        private readonly string _accessToken = ConfigurationManager.AppSettings["AccessToken"];
        private readonly string _testFilePath = "folder/sample.txt";

        [SetUp]
        public void SetupFixture()
        {
            var config = new DropboxClientConfig();
            _dropboxClient = new DropboxClient(_accessToken, config);
            _dropboxStorage = new DropboxStorage(_dropboxClient);

            UploadTestFile(_testFilePath);
        }

        [TearDown]
        public void CleanupFixture()
        {
            DeleteFile(_testFilePath);
        }

        [Test]
        public void TestFileExists()
        {
            Assert.IsTrue(_dropboxStorage.FileExists(_testFilePath));
        }

        [Test]
        public void TestGetFile()
        {
            var stream = _dropboxStorage.GetFile(_testFilePath);

            var contents = Encoding.UTF8.GetString(ReadBytes(stream));

            Assert.AreEqual("Hello, World!", contents);
        }

        [Test]
        public void TestSaveFile()
        {
            var filePath = "folder/saved.txt";

            var fileContent = "Hello, World!";
            var fileBytes = Encoding.UTF8.GetBytes(fileContent);
            var fileStream = new MemoryStream(fileBytes);

            _dropboxStorage.SaveFile(filePath, fileStream);

            Assert.AreEqual("Hello, World!", GetFileContent(filePath));

            DeleteFile(filePath);
        }

        [Test]
        public void TestGetFileInfo()
        {
            var fileInfo = _dropboxStorage.GetFileInfo(_testFilePath);

            Assert.AreEqual(CleanupPath(_testFilePath), fileInfo.Path);
            Assert.AreNotEqual(DateTime.MinValue, fileInfo.LastModified);
            Assert.IsFalse(fileInfo.IsDirectory);
            Assert.IsTrue(fileInfo.Size > 0);
        }

        [Test]
        public void TestGetFilesInfo()
        {
            var path = "folder/";

            var filesInfo = _dropboxStorage.GetFilesInfo(path).ToList();

            Assert.IsTrue(filesInfo.Count > 0);
            Assert.IsTrue(filesInfo.Any(x => x.Path == CleanupPath(_testFilePath)));
        }

        [Test]
        public void TestDeleteDirectory()
        {
            var filePath = "directory_to_delete/file.txt";

            UploadTestFile(filePath);

            _dropboxStorage.DeleteDirectory("directory_to_delete");

            Assert.IsFalse(FileExist(filePath));
        }

        private bool FileExist(string filePath)
        {
            try
            {
                string key = CleanupPath(filePath);

                var entry = _dropboxClient.Files.GetMetadataAsync(key);
                return !entry.Result.IsFolder && !entry.Result.IsDeleted;
            }
            catch (DropboxException)
            {
                return false;
            }
            catch (AggregateException)
            {
                return false;
            }
        }

        private void UploadTestFile(string filePath)
        {
            if (!FileExist(filePath))
            {
                var fileContent = "Hello, World!";
                var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

                var fileName = Path.GetFileName(filePath);
                var folderPath = CleanupPath(Path.GetDirectoryName(filePath));

                _dropboxClient.Files.UploadAsync
                    (folderPath + "/" + fileName, WriteMode.Overwrite.Instance, body: fileStream).Wait();
            }
        }

        private string GetFileContent(string filePath)
        {
            var key = CleanupPath(filePath);

            MemoryStream result = null;
            using (var wait = new ManualResetEvent(false))
            {
                var file = _dropboxClient.Files.DownloadAsync(key);
                {
                    result = new MemoryStream(file.Result.GetContentAsByteArrayAsync().Result);
                    wait.Set();
                }
                wait.WaitOne(60 * 1000);
            }
            return Encoding.UTF8.GetString(ReadBytes(result));
        }

        private void DeleteFile(string filePath)
        {
            var key = CleanupPath(filePath);

            _dropboxClient.Files.DeleteV2Async(key).Wait();
        }

        private byte[] ReadBytes(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private string CleanupPath(string path)
        {
            path = path.Replace("\\", "/");
            if (path != "" && !path.StartsWith("/"))
            {
                path = "/" + path;
            }
            return path;
        }
    }
}