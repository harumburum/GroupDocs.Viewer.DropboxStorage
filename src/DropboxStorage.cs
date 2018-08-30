using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Dropbox.Api;
using Dropbox.Api.Files;
using GroupDocs.Viewer.Storage;

namespace GroupDocs.Viewer.Dropbox
{
    /// <summary>
    /// Dropbox file storage
    /// </summary>
    public class DropboxStorage : IFileStorage, IDisposable
    {
        private readonly DropboxClient _client;

        private const int Timeout = 300 * 1000;

        /// <summary>
        /// Initializes new instance of <see cref="DropboxStorage"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        public DropboxStorage(DropboxClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Checks if file exists
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns><c>true</c> when file exists, otherwise <c>false</c></returns>
        public bool FileExists(string path)
        {
            try
            {
                string key = CleanupPath(path);

                var metadata = _client.Files.GetMetadataAsync(key).Result;
                return !metadata.IsFolder && !metadata.IsDeleted;
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

        /// <summary>
        /// Retrieves file content
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>Stream</returns>
        public Stream GetFile(string path)
        {
            var key = CleanupPath(path);

            MemoryStream result = null;
            using (var wait = new ManualResetEvent(false))
            {
                var file = _client.Files.DownloadAsync(key);
                {
                    result = new MemoryStream(file.Result.GetContentAsByteArrayAsync().Result);
                    wait.Set();
                }
                wait.WaitOne(Timeout);
            }
            return result;
        }

        /// <summary>
        /// Saves file
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="content">File content.</param>
        public void SaveFile(string path, Stream content)
        {
            var fileName = Path.GetFileName(path);
            var folderPath = CleanupPath(Path.GetDirectoryName(path));

            if (content.Position != 0 && content.CanSeek)
                content.Position = 0;

            _client.Files.UploadAsync
                (folderPath + "/" + fileName, WriteMode.Overwrite.Instance, body: content).Wait();
        }

        /// <summary>
        /// Removes directory
        /// </summary>
        /// <param name="path">Directory path.</param>
        public void DeleteDirectory(string path)
        {
            string key = CleanupPath(path);

            _client.Files.DeleteV2Async(key).Wait();
        }

        /// <summary>
        /// Retrieves file information
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>File information.</returns>
        public IFileInfo GetFileInfo(string path)
        {
            var key = CleanupPath(path);

            var metadata = _client.Files.GetMetadataAsync(key).Result;

            return CreateFileInfo(metadata);
        }

        /// <summary>
        /// Retrieves list of files and folders
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <returns>Files and folders.</returns>
        public IEnumerable<IFileInfo> GetFilesInfo(string path)
        {
            var key = CleanupPath(path);

            var result = new List<IFileInfo>();
            var parent = _client.Files.ListFolderAsync(key);

            if (parent.Result != null)
            {
                parent.Result.Entries.Where(x => !x.IsDeleted).ToList()
                    .ForEach(m => result.Add(CreateFileInfo(m)));
            }

            return result;
        }

        private Storage.FileInfo CreateFileInfo(Metadata metadata)
        {
            var fileInfo = new Storage.FileInfo
            {
                IsDirectory = metadata.IsFolder,
                Path = metadata.IsFolder ? metadata.AsFolder.PathDisplay : metadata.AsFile.PathDisplay,
                Size = metadata.IsFolder ? 0 : (long)metadata.AsFile.Size,
                LastModified = metadata.IsFolder ? DateTime.MinValue : metadata.AsFile.ClientModified
            };

            return fileInfo;
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

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}