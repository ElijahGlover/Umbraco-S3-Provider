using System;
using Amazon;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.DependencyInjection;
using Umbraco.Storage.S3.Services.Impl;

namespace Umbraco.Storage.S3
{
    public static class UmbracoBuilderExtensions
    {
        /// <summary>
        /// Replaces Umbraco's media file system with an S3 bucket, configured from the
        /// <c>Umbraco:Storage:S3:Media</c> section of appsettings.json.
        /// </summary>
        public static IUmbracoBuilder AddS3MediaFileSystem(this IUmbracoBuilder builder, Action<BucketFileSystemOptions>? configure = null)
        {
            var optionsBuilder = builder.Services
                .AddOptions<BucketFileSystemOptions>()
                .Bind(builder.Config.GetSection(BucketFileSystemOptions.MediaConfigurationSection));

            if (configure != null)
                optionsBuilder.Configure(configure);

            builder.SetMediaFileSystem(factory =>
            {
                var options = factory.GetRequiredService<IOptions<BucketFileSystemOptions>>().Value;
                var client = CreateClient(options.Region);
                var mimeTypeResolver = new DefaultMimeTypeResolver();
                var logger = factory.GetRequiredService<ILogger<BucketFileSystem>>();

                if (string.IsNullOrEmpty(options.CachePath))
                    return new BucketFileSystem(options, client, mimeTypeResolver, logger);

                if (options.CacheTimeToLive <= 0)
                    throw new ArgumentException("CacheTimeToLive must be greater than zero seconds when CachePath is set", nameof(options));

                var contentRootPath = factory.GetRequiredService<IHostEnvironment>().ContentRootPath;
                var cacheProvider = new FileSystemCacheProvider(
                    TimeSpan.FromSeconds(options.CacheTimeToLive),
                    options.CachePath,
                    new FileSystemWrapper(contentRootPath));

                return new CachedBucketFileSystem(options, client, mimeTypeResolver, logger, cacheProvider);
            });

            return builder;
        }

        //Credentials Are Resolved From The AWS SDK Default Chain (Environment Variables, Profile, Instance/Task Role)
        private static IAmazonS3 CreateClient(string? region)
        {
            return string.IsNullOrEmpty(region)
                ? new AmazonS3Client()
                : new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
        }
    }
}
