using Amazon.S3;
using Amazon.S3.Model;

namespace Umbraco.Storage.S3
{
    public class WrappedAmazonS3Client : IAmazonS3Client
    {
        private readonly AmazonS3Client _client;

        public WrappedAmazonS3Client(AmazonS3Client client)
        {
            _client = client;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public ListObjectsResponse ListObjects(ListObjectsRequest request)
        {
            return _client.ListObjects(request);
        }

        public PutObjectResponse PutObject(PutObjectRequest request)
        {
            return _client.PutObject(request);
        }

        public GetObjectResponse GetObject(GetObjectRequest request)
        {
            return _client.GetObject(request);
        }

        public DeleteObjectResponse DeleteObject(DeleteObjectRequest request)
        {
            return _client.DeleteObject(request);
        }

        public GetObjectMetadataResponse GetObjectMetadata(GetObjectMetadataRequest request)
        {
            return _client.GetObjectMetadata(request);
        }

        public DeleteObjectsResponse DeleteObjects(DeleteObjectsRequest request)
        {
            return _client.DeleteObjects(request);
        }
    }
}
