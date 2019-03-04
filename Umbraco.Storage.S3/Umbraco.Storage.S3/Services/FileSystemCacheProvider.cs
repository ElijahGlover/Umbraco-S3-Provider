using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Umbraco.Storage.S3.Services
{
    public class FileSystemCacheProvider : IFileCacheProvider
    {
        private readonly TimeSpan _timeToLive;
        private readonly string _cachePath;
        private readonly IFileSystemWrapper _fileSystemWrapper;

        public FileSystemCacheProvider(TimeSpan timeToLive, string cachePath, IFileSystemWrapper fileSystemWrapper = null)
        {
            _timeToLive = timeToLive;
            _cachePath = cachePath;
            _fileSystemWrapper = fileSystemWrapper ?? new FileSystemWrapper();
        }

        private static string ResolveStorageKey(string value)
        {
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(value));
                return string.Concat(hash.Select(p => p.ToString("x2")));
            }
        }

        private string ResolveStoragePath(string value)
        {
            var virtualPath = string.Concat(_cachePath, ResolveStorageKey(value));
            return _fileSystemWrapper.MapPath(virtualPath);
        }

        public bool Exists(string key)
        {
            var virtualPath = ResolveStoragePath(key);
            return !IsExpired(_fileSystemWrapper.GetLastAccessTimeUtc(virtualPath));
        }

        public void Persist(string key, Stream stream)
        {
            var basePath = _fileSystemWrapper.MapPath(_cachePath);
            _fileSystemWrapper.EnsureDirectory(basePath);

            var path = ResolveStoragePath(key);
            _fileSystemWrapper.Create(path, stream);

            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
        }

        private bool IsExpired(DateTime time)
        {
            return DateTime.UtcNow.Subtract(time) > _timeToLive;
        }

        public Stream Resolve(string key)
        {
            var path = ResolveStoragePath(key);

            try
            {
                if (IsExpired(_fileSystemWrapper.GetLastAccessTimeUtc(path)))
                    return null;

                return _fileSystemWrapper.Open(path);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}
