using System;
using System.IO;
using System.Web.Hosting;

namespace Umbraco.Storage.S3
{
    public class FileSystemWrapper : IFileSystemWrapper
    {
        public void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public Stream Open(string filePath)
        {
            return File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public void Create(string filePath, Stream stream)
        {
            using (var fileStream = File.Create(filePath))
                stream.CopyTo(fileStream);
        }

        public DateTime GetLastAccessTimeUtc(string filePath)
        {
            return File.GetLastAccessTimeUtc(filePath);
        }

        public string MapPath(string virtualPath)
        {
            return HostingEnvironment.MapPath(virtualPath);
        }
    }
}
