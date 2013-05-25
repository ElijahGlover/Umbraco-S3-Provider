using System;
using Amazon.S3.Model;

namespace Umbraco.Storage.S3
{
    public interface IAmazonS3Client : IDisposable
    {
        ListObjectsResponse ListObjects(ListObjectsRequest request);
        PutObjectResponse PutObject(PutObjectRequest request);
        GetObjectResponse GetObject(GetObjectRequest request);
        DeleteObjectResponse DeleteObject(DeleteObjectRequest request);
        GetObjectMetadataResponse GetObjectMetadata(GetObjectMetadataRequest request);
        DeleteObjectsResponse DeleteObjects(DeleteObjectsRequest request);
    }
}
