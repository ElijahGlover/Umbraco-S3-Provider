namespace Umbraco.Storage.S3
{
    public class BucketFileSystemOptions
    {
        public const string MediaConfigurationSection = "Umbraco:Storage:S3:Media";

        /// <summary>S3 Bucket Name</summary>
        public string BucketName { get; set; } = string.Empty;

        /// <summary>S3 Bucket Hostname - Prepended to URLs stored in Umbraco's database (ie https://{bucket}.s3.amazonaws.com or a CDN)</summary>
        public string? BucketHostName { get; set; }

        /// <summary>S3 Object Key Prefix - What should we prefix keys with?</summary>
        public string? BucketKeyPrefix { get; set; } = "media";

        /// <summary>AWS Region Endpoint (us-east-1/us-west-1/ap-southeast-2) - Falls back to the AWS SDK's region resolution when empty</summary>
        public string? Region { get; set; }

        /// <summary>S3 Canned ACL - Sets permissions for uploaded files. Defaults to public-read if omitted or invalid.</summary>
        public string? CannedACL { get; set; }

        /// <summary>S3 ServerSide Encryption Method - Defaults to None if omitted or invalid.</summary>
        public string? ServerSideEncryptionMethod { get; set; }

        /// <summary>Local cache directory (ie ~/umbraco/Data/TEMP/S3Cache/) - Caching is disabled when empty</summary>
        public string? CachePath { get; set; }

        /// <summary>Local cache time to live in seconds</summary>
        public int CacheTimeToLive { get; set; }
    }
}
