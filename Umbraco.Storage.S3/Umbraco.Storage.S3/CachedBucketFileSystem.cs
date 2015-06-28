using System;
using System.IO;

namespace Umbraco.Storage.S3
{
    public class CachedBucketFileSystem : BucketFileSystem
    {
        public CachedBucketFileSystem(
            string bucketName,
            string bucketHostName,
            string bucketKeyPrefix,
            string region,
            string cachePath,
            string timeToLive)
            : base(bucketName, bucketHostName, bucketKeyPrefix, region)
        {
            var timeToLiveValue = int.Parse(timeToLive);
            CacheProvider = new FileSystemCacheProvider(new TimeSpan(timeToLiveValue), cachePath);
        }

        public ICacheProvider CacheProvider { get; set; }

        public override Stream OpenFile(string path)
        {
            var persistedStream = CacheProvider.Resolve(path);
            if (persistedStream != null)
                return persistedStream;

            var stream = base.OpenFile(path);
            CacheProvider.Persist(path, stream);

            return stream;
        }
    }
}
