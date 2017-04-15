using System;
using System.IO;
using Umbraco.Storage.S3.Services;
using Umbraco.Storage.S3.Services.Impl;

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
            int timeToLiveValue;
            if (!int.TryParse(timeToLive, out timeToLiveValue))
                throw new ArgumentException("timeToLive value be castable to int type", timeToLive);

            var timeToLiveTimeSpan = TimeSpan.FromSeconds(timeToLiveValue);
            CacheProvider = new FileSystemCacheProvider(timeToLiveTimeSpan, cachePath);
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

        public override bool FileExists(string path)
        {
            return CacheProvider.Exists(path) || base.FileExists(path);
        }
    }
}
