using Amazon.S3;

namespace Umbraco.Storage.S3
{
    public class BucketFileSystemConfig
    {
        public string BucketName { get; set; }

        public string BucketHostName { get; set; }

        public string BucketPrefix { get; set; }

        public string Region { get; set; }

        public S3CannedACL CannedACL { get; set; }

        public ServerSideEncryptionMethod ServerSideEncryptionMethod { get; set; }
    }
}
