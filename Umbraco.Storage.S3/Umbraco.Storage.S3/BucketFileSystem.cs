using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using Amazon;
using Amazon.S3;
using Umbraco.Core.IO;
using Amazon.S3.Model;
using Umbraco.Core.Logging;
using Umbraco.Storage.S3.Extensions;
using Umbraco.Storage.S3.Services;

namespace Umbraco.Storage.S3
{
    public class BucketFileSystem : IFileSystem
    {
        protected readonly BucketFileSystemConfig Config;
        protected readonly ILogger Logger;
        protected readonly IFileCacheProvider FileCacheProvider;
        protected readonly IMimeTypeResolver MimeTypeResolver;
        protected readonly IAmazonS3 S3Client;

        protected const string Delimiter = "/";
        protected const int BatchSize = 1000;

        public BucketFileSystem(
            BucketFileSystemConfig config,
            IMimeTypeResolver mimeTypeResolver,
            IFileCacheProvider fileCacheProvider,
            ILogger logger,
            IAmazonS3 s3Client)
        {
            Config = config;
            FileCacheProvider = fileCacheProvider;
            MimeTypeResolver = mimeTypeResolver;
            Logger = logger;
            S3Client = s3Client;
        }

        public bool CanAddPhysical => false;
        public string PathPrefix => Config.BucketPrefix;

        protected virtual T Execute<T>(Func<IAmazonS3, T> request)
        {
            try
            {
                return request(S3Client);
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new FileNotFoundException(ex.Message, ex);
                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException(ex.Message, ex);

                Logger.Error<BucketFileSystem>(string.Format("S3 Bucket Error {0} {1}", ex.ErrorCode, ex.Message), ex);
                throw;
            }
        }

        protected virtual IEnumerable<ListObjectsResponse> ExecuteWithContinuation(ListObjectsRequest request)
        {
            var response = Execute(client => client.ListObjects(request));
            yield return response;

            while (response.IsTruncated)
            {
                request.Marker = response.NextMarker;
                response = Execute(client => client.ListObjects(request));
                yield return response;
            }
        }

        protected virtual string ResolveBucketPath(string path, bool isDir = false)
        {
            if (string.IsNullOrEmpty(path))
                return Config.BucketPrefix;

            //Remove Bucket Hostname
            if (!path.Equals("/") && path.StartsWith(Config.BucketHostName, StringComparison.InvariantCultureIgnoreCase))
                path = path.Substring(Config.BucketHostName.Length);

            // Equalise delimiters
            path = path.Replace("/", Delimiter).Replace("\\", Delimiter);

            if (path.StartsWith(Delimiter))
                path = path.Substring(1);

            //Remove Key Prefix If Duplicate
            if (path.StartsWith(Config.BucketPrefix, StringComparison.InvariantCultureIgnoreCase))
                path = path.Substring(Config.BucketPrefix.Length);

            if (isDir && (!path.EndsWith(Delimiter)))
                path = string.Concat(path, Delimiter);

            if (path.StartsWith(Delimiter))
                path = path.Substring(1);

            return string.Concat(Config.BucketPrefix, "/", path);
        }

        protected virtual string RemovePrefix(string key)
        {
            if (!string.IsNullOrEmpty(Config.BucketPrefix) && key.StartsWith(Config.BucketPrefix))
                key = key.Substring(Config.BucketPrefix.Length);

            return key.TrimStart(Delimiter.ToCharArray()).TrimEnd(Delimiter.ToCharArray());
        }

        public virtual IEnumerable<string> GetDirectories(string path)
        {
            if (string.IsNullOrEmpty(path))
                path = "/";

            path = ResolveBucketPath(path, true);
            var request = new ListObjectsRequest
            {
                BucketName = Config.BucketName,
                Delimiter = Delimiter,
                Prefix = path
            };

            var response = ExecuteWithContinuation(request);
            return response
                .SelectMany(p => p.CommonPrefixes)
                .Select(p => RemovePrefix(p))
                .ToArray();
        }

        public virtual void DeleteDirectory(string path)
        {
            DeleteDirectory(path, false);
        }

        public virtual void DeleteDirectory(string path, bool recursive)
        {
            //List Objects To Delete
            var listRequest = new ListObjectsRequest
            {
                BucketName = Config.BucketName,
                Prefix = ResolveBucketPath(path, true)
            };

            var listResponse = ExecuteWithContinuation(listRequest);
            var keys = listResponse
                .SelectMany(p => p.S3Objects)
                .Select(p => new KeyVersion { Key = p.Key })
                .ToArray();

            //Batch Deletion Requests
            foreach (var items in keys.Batch(BatchSize))
            {
                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = Config.BucketName,
                    Objects = items.ToList()
                };
                Execute(client => client.DeleteObjects(deleteRequest));
            }
        }

        public virtual bool DirectoryExists(string path)
        {
            var request = new ListObjectsRequest
            {
                BucketName = Config.BucketName,
                Prefix = ResolveBucketPath(path, true),
                MaxKeys = 1
            };

            var response = Execute(client => client.ListObjects(request));
            return response.S3Objects.Count > 0;
        }

        public virtual void AddFile(string path, Stream stream)
        {
            AddFile(path, stream, true);
        }

        public virtual void AddFile(string path, Stream stream, bool overrideIfExists)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                var request = new PutObjectRequest
                {
                    BucketName = Config.BucketName,
                    Key = ResolveBucketPath(path),
                    CannedACL = Config.CannedACL,
                    ContentType = MimeTypeResolver.Resolve(path),
                    InputStream = memoryStream,
                    ServerSideEncryptionMethod = Config.ServerSideEncryptionMethod
                };

                var response = Execute(client => client.PutObject(request));
                this.Logger.Info<BucketFileSystem>(string.Format("Object {0} Created, Id:{1}, Hash:{2}", path, response.VersionId, response.ETag));
            }
        }

        public void AddFile(string path, string physicalPath, bool overrideIfExists = true, bool copy = false)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<string> GetFiles(string path)
        {
            return GetFiles(path, "*.*");
        }

        public virtual IEnumerable<string> GetFiles(string path, string filter)
        {
            path = ResolveBucketPath(path, true);

            string filename = Path.GetFileNameWithoutExtension(filter);
            if (filename.EndsWith("*"))
                filename = filename.Remove(filename.Length - 1);

            string ext = Path.GetExtension(filter);
            if (ext.Contains("*"))
                ext = string.Empty;

            var request = new ListObjectsRequest
            {
                BucketName = Config.BucketName,
                Delimiter = Delimiter,
                Prefix = path + filename
            };

            var response = ExecuteWithContinuation(request);
            return response
                .SelectMany(p => p.S3Objects)
                .Select(p => RemovePrefix(p.Key))
                .Where(p => !string.IsNullOrEmpty(p) && p.EndsWith(ext))
                .ToArray();

        }

        public virtual Stream OpenFile(string path)
        {
            if (FileCacheProvider != null)
            {
                var persistedStream = FileCacheProvider.Resolve(path);
                if (persistedStream != null)
                    return persistedStream;
            }

            var request = new GetObjectRequest
            {
                BucketName = Config.BucketName,
                Key = ResolveBucketPath(path)
            };

            MemoryStream stream;
            using (var response = Execute(client => client.GetObject(request)))
            {
                stream = new MemoryStream();
                response.ResponseStream.CopyTo(stream);
            }

            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);

            if (FileCacheProvider != null)
                FileCacheProvider.Persist(path, stream);

            return stream;
        }

        public virtual void DeleteFile(string path)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = Config.BucketName,
                Key = ResolveBucketPath(path)
            };
            Execute(client => client.DeleteObject(request));
        }

        public virtual bool FileExists(string path)
        {
            if (FileCacheProvider != null && FileCacheProvider.Exists(path))
                return true;

            var request = new GetObjectMetadataRequest
            {
                BucketName = Config.BucketName,
                Key = ResolveBucketPath(path)
            };

            try
            {
                Execute(client => client.GetObjectMetadata(request));
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        public virtual string GetRelativePath(string fullPathOrUrl)
        {
            if (string.IsNullOrEmpty(fullPathOrUrl))
                return string.Empty;

            //Strip protocol if not in hostname
            if (!Config.BucketHostName.StartsWith("http"))
            {
                if (fullPathOrUrl.StartsWith("https://"))
                {
                    fullPathOrUrl = fullPathOrUrl.Substring("https://".Length);
                }
                if (fullPathOrUrl.StartsWith("http://"))
                {
                    fullPathOrUrl = fullPathOrUrl.Substring("http://".Length);
                }
            }

            //Strip Hostname
            if (fullPathOrUrl.StartsWith(Config.BucketHostName, StringComparison.InvariantCultureIgnoreCase))
            {
                fullPathOrUrl = fullPathOrUrl.Substring(Config.BucketHostName.Length);
                fullPathOrUrl = fullPathOrUrl.TrimStart(Delimiter.ToCharArray());
            }

            //Strip Bucket Prefix
            if (fullPathOrUrl.StartsWith(Config.BucketPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                fullPathOrUrl = fullPathOrUrl.Substring(Config.BucketPrefix.Length);
                fullPathOrUrl = fullPathOrUrl.TrimStart(Delimiter.ToCharArray());
            }

            return fullPathOrUrl;
        }

        public virtual string GetFullPath(string path)
        {
            return path;
        }

        public virtual string GetUrl(string path)
        {
            var hostName = Config.BucketHostName;

            if (Config.DisableVirtualPathProvider)
            {
                if (!hostName.StartsWith("http://") && !hostName.StartsWith("https://"))
                    hostName = "https://" + hostName;
            }
            else
            {
                hostName = "";
            }

            return string.Concat(hostName, "/", ResolveBucketPath(path));
        }

        public virtual DateTimeOffset GetLastModified(string path)
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = Config.BucketName,
                Key = ResolveBucketPath(path)
            };

            var response = Execute(client => client.GetObjectMetadata(request));
            return new DateTimeOffset(response.LastModified);
        }

        public virtual DateTimeOffset GetCreated(string path)
        {
            //It Is Not Possible To Get Object Created Date - Bucket Versioning Required
            //Return Last Modified Date Instead
            return GetLastModified(path);
        }

        public long GetSize(string path)
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = Config.BucketName,
                Key = ResolveBucketPath(path)
            };

            var response = Execute(client => client.GetObjectMetadata(request));
            return response.ContentLength;
        }
    }
}
