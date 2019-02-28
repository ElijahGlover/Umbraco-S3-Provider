using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.S3;
using Umbraco.Core.IO;
using Amazon.S3.Model;
using Umbraco.Storage.S3.Extensions;
using Umbraco.Storage.S3.Services;
using Umbraco.Storage.S3.Services.Impl;

namespace Umbraco.Storage.S3
{
    public class BucketFileSystem : IFileSystem
    {
        protected readonly string BucketName;
        protected readonly string BucketHostName;
        protected readonly string BucketPrefix;
        protected readonly ServerSideEncryptionMethod ServerSideEncryptionMethod;
        protected readonly S3CannedACL ACL;
        protected const string Delimiter = "/";
        protected const int BatchSize = 1000;

        public BucketFileSystem(
            string bucketName,
            string bucketHostName,
            string bucketKeyPrefix,
            string region,
            string cannedACL,
            string serverSideEncryptionMethod)
        {

            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException("bucketName");

            BucketName = bucketName;
            BucketHostName = BucketExtensions.ParseBucketHostName(bucketHostName);
            BucketPrefix = BucketExtensions.ParseBucketPrefix(bucketKeyPrefix);

            ServerSideEncryptionMethod = EncryptionExtensions.ParseServerSideEncryptionMethod(serverSideEncryptionMethod);

            ACL = AclExtensions.ParseCannedAcl(cannedACL);

            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            ClientFactory = () => new AmazonS3Client(regionEndpoint);
            LogHelper = new LogHelperWrapper();
            MimeTypeResolver = new DefaultMimeTypeResolver();
        }

        public BucketFileSystem(
            string bucketName,
            string bucketHostName,
            string bucketKeyPrefix,
            string region): this(bucketName, bucketHostName, bucketKeyPrefix, region, null, null) { }

        public Func<IAmazonS3> ClientFactory { get; set; }

        public ILogHelper LogHelper { get; set; }

        public IMimeTypeResolver MimeTypeResolver { get; set; }

        public bool CanAddPhysical => false;

        protected virtual T Execute<T>(Func<IAmazonS3, T> request)
        {
            using (var client = ClientFactory())
            {
                try {
                    return request(client);
                } catch (AmazonS3Exception ex) {
                    if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        throw new FileNotFoundException(ex.Message, ex);
                    if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        throw new UnauthorizedAccessException(ex.Message, ex);
                    LogHelper.Error<BucketFileSystem>(string.Format("S3 Bucket Error {0} {1}", ex.ErrorCode, ex.Message), ex);
                    throw;
                }
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
                return BucketPrefix;

            //Remove Bucket Hostname
            if (!path.Equals("/") && path.StartsWith(BucketHostName, StringComparison.InvariantCultureIgnoreCase))
                path = path.Substring(BucketHostName.Length);

            path = path.Replace("\\", Delimiter);
            if (path == Delimiter)
                return BucketPrefix;

            if (path.StartsWith(Delimiter))
                path = path.Substring(1);

            //Remove Key Prefix If Duplicate
            if (path.StartsWith(BucketPrefix, StringComparison.InvariantCultureIgnoreCase))
                path = path.Substring(BucketPrefix.Length);

            if (isDir && (!path.EndsWith(Delimiter)))
                path = string.Concat(path, Delimiter);

            return string.Concat(BucketPrefix, path);
        }

        protected virtual string RemovePrefix(string key)
        {
            if (!string.IsNullOrEmpty(BucketPrefix) && key.StartsWith(BucketPrefix))
                key = key.Substring(BucketPrefix.Length);

            if (key.EndsWith(Delimiter))
                key = key.Substring(0, key.Length - Delimiter.Length);
            return key;
        }

        public virtual IEnumerable<string> GetDirectories(string path)
        {
            if (string.IsNullOrEmpty(path))
                path = "/";

            path = ResolveBucketPath(path, true);
            var request = new ListObjectsRequest
            {
                BucketName = BucketName,
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
                BucketName = BucketName,
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
                    BucketName = BucketName,
                    Objects = items.ToList()
                };
                Execute(client => client.DeleteObjects(deleteRequest));
            }
        }

        public virtual bool DirectoryExists(string path)
        {
            var request = new ListObjectsRequest
            {
                BucketName = BucketName,
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
                    BucketName = BucketName,
                    Key = ResolveBucketPath(path),
                    CannedACL = ACL,
                    ContentType = MimeTypeResolver.Resolve(path),
                    InputStream = memoryStream,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod
                };

                var response = Execute(client => client.PutObject(request));
                LogHelper.Info<BucketFileSystem>(string.Format("Object {0} Created, Id:{1}, Hash:{2}", path, response.VersionId, response.ETag));
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
                BucketName = BucketName,
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
            var request = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = ResolveBucketPath(path)
            };

            MemoryStream stream;
            using (var response = Execute(client => client.GetObject(request)))
            {
                stream = new MemoryStream();
                response.ResponseStream.CopyTo(stream);
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public virtual void DeleteFile(string path)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = BucketName,
                Key = ResolveBucketPath(path)
            };
            Execute(client => client.DeleteObject(request));
        }

        public virtual bool FileExists(string path)
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = BucketName,
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

            if (fullPathOrUrl.StartsWith(Delimiter))
                fullPathOrUrl = fullPathOrUrl.Substring(1);

            //Strip Hostname
            if (fullPathOrUrl.StartsWith(BucketHostName, StringComparison.InvariantCultureIgnoreCase))
                fullPathOrUrl = fullPathOrUrl.Substring(BucketHostName.Length);

            //Strip Bucket Prefix
            if (fullPathOrUrl.StartsWith(BucketPrefix, StringComparison.InvariantCultureIgnoreCase))
                return fullPathOrUrl.Substring(BucketPrefix.Length);

            return fullPathOrUrl;
        }

        public virtual string GetFullPath(string path)
        {
            return path;
        }

        public virtual string GetUrl(string path)
        {
            return string.Concat(BucketHostName, ResolveBucketPath(path));
        }

        public virtual DateTimeOffset GetLastModified(string path)
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = BucketName,
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
                BucketName = BucketName,
                Key = ResolveBucketPath(path)
            };

            var response = Execute(client => client.GetObjectMetadata(request));
            return response.ContentLength;
        }
    }
}
