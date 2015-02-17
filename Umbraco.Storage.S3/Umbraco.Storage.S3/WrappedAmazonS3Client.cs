using System;
using Amazon.S3;
using Amazon.S3.Model;

namespace Umbraco.Storage.S3
{
    //Used For Mocking Purposes
    public class WrappedAmazonS3Client : IDisposable
    {
        private readonly AmazonS3Client _client;

        public WrappedAmazonS3Client(AmazonS3Client client)
        {
            _client = client;
        }

        public WrappedAmazonS3Client()
        {

        }

        public virtual void Dispose()
        {
            _client.Dispose();
        }

        public virtual ListObjectsResponse ListObjects(ListObjectsRequest request)
        {
            return _client.ListObjects(request);
        }

        public virtual PutObjectResponse PutObject(PutObjectRequest request)
        {
            return _client.PutObject(request);
        }

        public virtual GetObjectResponse GetObject(GetObjectRequest request)
        {
            return _client.GetObject(request);
        }

        public virtual DeleteObjectResponse DeleteObject(DeleteObjectRequest request)
        {
            return _client.DeleteObject(request);
        }

        public virtual GetObjectMetadataResponse GetObjectMetadata(GetObjectMetadataRequest request)
        {
            return _client.GetObjectMetadata(request);
        }

        public virtual DeleteObjectsResponse DeleteObjects(DeleteObjectsRequest request)
        {
            return _client.DeleteObjects(request);
        }
    }
}
