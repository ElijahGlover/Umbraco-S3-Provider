using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.S3;
using Umbraco.Core.IO;
using Amazon.S3.Model;
using Umbraco.Storage.S3.Exception;

namespace Umbraco.Storage.S3
{
    public class BucketFileSystem : IFileSystem
    {
        protected readonly string BucketName;
        protected readonly string BucketHostName;
        protected readonly string BucketPrefix;
        protected const string Delimiter = "/";
        protected const int BatchSize = 1000;

        public BucketFileSystem(string bucketName, string bucketHostName, string bucketKeyPrefix, string region)
        {
            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException("bucketName");

            BucketName = bucketName;
            BucketHostName = BucketExtensions.ParseBucketHostName(bucketHostName);
            BucketPrefix = BucketExtensions.ParseBucketPrefix(bucketKeyPrefix);

            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            ClientFactory = () => new WrappedAmazonS3Client(new AmazonS3Client(regionEndpoint));
            LogHelper = new WrappedLogHelper();
            MimeTypeResolver = new DefaultMimeTypeResolver();
        }

        public Func<WrappedAmazonS3Client> ClientFactory { get; set; }

        public ILogHelper LogHelper { get; set; }

        public IMimeTypeResolver MimeTypeResolver { get; set; }

        protected T Execute<T>(Func<WrappedAmazonS3Client, T> request)
        {
            using (var client = ClientFactory())
            {
                try {
                    return request(client);
                } catch (AmazonS3Exception ex) {
                    if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        throw new ObjectNotFoundException(ex.Message);
                    LogHelper.Error<BucketFileSystem>(string.Format("S3 Bucket Error {0} {1}", ex.ErrorCode, ex.Message), ex);
                    throw;
                }
            }
        }

        protected IEnumerable<ListObjectsResponse> ExecuteWithContinuation(ListObjectsRequest request)
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

        protected string ResolveBucketPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return BucketPrefix;
            if (path.StartsWith(BucketHostName, StringComparison.InvariantCultureIgnoreCase))
                path = path.Substring(BucketHostName.Length);

            path = path.Replace("\\", "/");
            if (path == "/")
                return BucketPrefix;

            if (path.StartsWith("/"))
                path = path.Substring(1);

            return string.Concat(BucketPrefix, path);
        }

        protected string RemovePrefix(string key)
        {
            if (!string.IsNullOrEmpty(BucketPrefix) && key.StartsWith(key))
                return key.Substring(BucketPrefix.Length);
            return key;
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            var request = new ListObjectsRequest
            {
                BucketName = BucketName,
                Delimiter = Delimiter,
                Prefix = ResolveBucketPath(path)
            };

            var response = ExecuteWithContinuation(request);
            return response
                .SelectMany(p => p.CommonPrefixes)
                .Select(p => RemovePrefix(string.Concat(p, Delimiter)))
                .ToArray();
        }

        public void DeleteDirectory(string path)
        {
            DeleteDirectory(path, false);
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            //TODO recursive (use WithDelimiter)
            //List Objects To Delete
            var listRequest = new ListObjectsRequest
            {
                BucketName = BucketName,
                Prefix = ResolveBucketPath(path)
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
                    BucketName = BucketName,
                    Objects = items.ToList()
                };
                Execute(client => client.DeleteObjects(deleteRequest));
            }
        }

        public bool DirectoryExists(string path)
        {
            var request = new ListObjectsRequest
            {
                BucketName = BucketName,
                Prefix = ResolveBucketPath(path),
                MaxKeys = 1
            };

            var response = Execute(client => client.ListObjects(request));
            return response.S3Objects.Count > 0;
        }

        public void AddFile(string path, Stream stream)
        {
            AddFile(path, stream, true);
        }

        public void AddFile(string path, Stream stream, bool overrideIfExists)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);

                var request = new PutObjectRequest
                {
                    BucketName = BucketName,
                    Key = ResolveBucketPath(path),
                    CannedACL = S3CannedACL.PublicRead,
                    ContentType = MimeTypeResolver.Resolve(path),
                    InputStream = memoryStream
                };

                var response = Execute(client => client.PutObject(request));
                LogHelper.Info<BucketFileSystem>(string.Format("Object {0} Created, Id:{1}, Hash:{2}", path, response.VersionId, response.ETag));
            }
        }

        public IEnumerable<string> GetFiles(string path)
        {
            return GetFiles(path, string.Empty);
        }

        public IEnumerable<string> GetFiles(string path, string filter)
        {
            //TODO Add Filter To ListObjectRequest
            var request = new ListObjectsRequest
            {
                BucketName = BucketName,
                Delimiter = Delimiter,
                Prefix = ResolveBucketPath(path)
            };

            var response = ExecuteWithContinuation(request);
            return response.SelectMany(p => p.S3Objects).Select(p => RemovePrefix(p.Key));
        }

        public Stream OpenFile(string path)
        {
            var request = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = ResolveBucketPath(path)
            };

            var response = Execute(client => client.GetObject(request));

            //Read Response In Memory To Seek
            var stream = new MemoryStream();
            response.ResponseStream.CopyTo(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public void DeleteFile(string path)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = BucketName,
                Key = ResolveBucketPath(path)
            };
            Execute(client => client.DeleteObject(request));
        }

        public bool FileExists(string path)
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = BucketName,
                Key = ResolveBucketPath(path)
            };

            try {
                Execute(client => client.GetObjectMetadata(request));
                return true;
            } catch (ObjectNotFoundException) {
                return false;
            }
        }

        public string GetRelativePath(string fullPathOrUrl)
        {
            if (string.IsNullOrEmpty(fullPathOrUrl))
                return string.Empty;

            return fullPathOrUrl.StartsWith(BucketHostName, StringComparison.InvariantCultureIgnoreCase)
                ? fullPathOrUrl.Substring(BucketHostName.Length)
                : fullPathOrUrl;
        }

        public string GetFullPath(string path)
        {
            return path;
        }

        public string GetUrl(string path)
        {
            return string.Concat(BucketHostName, ResolveBucketPath(path));
        }

        public DateTimeOffset GetLastModified(string path)
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = BucketName,
                Key = ResolveBucketPath(path)
            };

            var response = Execute(client => client.GetObjectMetadata(request));
            return new DateTimeOffset(response.LastModified);
        }

        public DateTimeOffset GetCreated(string path)
        {
            //It Is Not Possible To Get Object Created Date - Bucket Versioning Required
            //Return Last Modified Date Instead
            return GetLastModified(path);
        }
    }
}
