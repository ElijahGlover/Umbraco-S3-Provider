using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public BucketFileSystem(string bucketName, string bucketHostName, string bucketKeyPrefix)
        {
            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException("bucketName");
            if (string.IsNullOrEmpty(bucketHostName))
                throw new ArgumentNullException("bucketHostName");

            BucketName = bucketName;
            BucketHostName = BucketExtensions.ParseBucketHostName(bucketHostName);
            BucketPrefix = BucketExtensions.ParseBucketPrefix(bucketKeyPrefix);
            ClientFactory = () => new WrappedAmazonS3Client(new AmazonS3Client());
            LogHelper = new WrappedLogHelper();
            MimeTypeResolver = new DefaultMimeTypeResolver();
        }

        public Func<IAmazonS3Client> ClientFactory { get; set; }

        public ILogHelper LogHelper { get; set; }

        public IMimeTypeResolver MimeTypeResolver { get; set; }

        protected T Execute<T>(Func<IAmazonS3Client, T> request)
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

            while (response.IsTruncated) {
                request.WithMarker(response.NextMarker);
                response = Execute(client => client.ListObjects(request));
                yield return response;
            }
        }

        protected string ResolveBucketPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return BucketPrefix;
            path = path.Replace("\\", "/");
            if (path == "/")
                return BucketPrefix;
            if (path.StartsWith("/"))
                path = path.Substring(1);
            return string.Concat(BucketPrefix, path);
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            var request = new ListObjectsRequest()
                .WithBucketName(BucketName)
                .WithDelimiter(Delimiter)
                .WithPrefix(ResolveBucketPath(path));

            var response = ExecuteWithContinuation(request);
            return response
                .SelectMany(p => p.CommonPrefixes)
                .Select(p => string.Concat(p, Delimiter))
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
            var listRequest = new ListObjectsRequest()
               .WithBucketName(BucketName)
               .WithPrefix(ResolveBucketPath(path));
            var listResponse = ExecuteWithContinuation(listRequest);
            var keys = listResponse
                .SelectMany(p => p.S3Objects)
                .Select(p => new KeyVersion(p.Key))
                .ToArray();

            //Batch Deletion Requests
            foreach (var items in keys.Batch(BatchSize))
            {
                var deleteRequest = new DeleteObjectsRequest()
                    .WithBucketName(BucketName)
                    .WithKeys(items.ToArray());
                Execute(client => client.DeleteObjects(deleteRequest));
            }
        }

        public bool DirectoryExists(string path)
        {
            var request = new ListObjectsRequest()
                .WithBucketName(BucketName)
                .WithPrefix(ResolveBucketPath(path))
                .WithMaxKeys(1);

            var response = Execute(client => client.ListObjects(request));
            return response.S3Objects.Count > 0;
        }

        public void AddFile(string path, Stream stream)
        {
            AddFile(path, stream, true);
        }

        public void AddFile(string path, Stream stream, bool overrideIfExists)
        {
            var request = new PutObjectRequest()
                .WithBucketName(BucketName)
                .WithKey(ResolveBucketPath(path))
                .WithCannedACL(S3CannedACL.PublicRead)
                .WithContentType(MimeTypeResolver.Resolve(path));
            request.InputStream = stream;

            var response = Execute(client => client.PutObject(request));
            LogHelper.Info<BucketFileSystem>(string.Format("Object {0} Created, Id:{1}, Hash:{2}", path, response.VersionId, response.ETag));
        }

        public IEnumerable<string> GetFiles(string path)
        {
            return GetFiles(path, string.Empty);
        }

        public IEnumerable<string> GetFiles(string path, string filter)
        {
            //TODO Add Filter To ListObjectRequest
            var request = new ListObjectsRequest()
                .WithBucketName(BucketName)
                .WithDelimiter(Delimiter)
                .WithPrefix(ResolveBucketPath(path));
            var response = ExecuteWithContinuation(request);
            return response.SelectMany(p => p.S3Objects).Select(p => p.Key);
        }

        public Stream OpenFile(string path)
        {
            var request = new GetObjectRequest()
                .WithBucketName(BucketName)
                .WithKey(ResolveBucketPath(path));

            var response = Execute(client => client.GetObject(request));

            //Read Response In Memory To Seek
            var stream = new MemoryStream();
            response.ResponseStream.CopyTo(stream);
            return stream;
        }

        public void DeleteFile(string path)
        {
            var request = new DeleteObjectRequest()
                .WithBucketName(BucketName)
                .WithKey(ResolveBucketPath(path));
            Execute(client => client.DeleteObject(request));
        }

        public bool FileExists(string path)
        {
            var request = new GetObjectMetadataRequest()
                .WithBucketName(BucketName)
                .WithKey(ResolveBucketPath(path));

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
            var url = string.Concat(BucketHostName, BucketPrefix);
            return fullPathOrUrl.StartsWith(url)
                ? fullPathOrUrl.Substring(url.Length)
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
            var request = new GetObjectMetadataRequest()
              .WithBucketName(BucketName)
              .WithKey(ResolveBucketPath(path));

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
