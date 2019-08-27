using System.Configuration;
using Amazon.S3;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Exceptions;
using Umbraco.Core.Logging;
using Umbraco.Storage.S3.Services;

namespace Umbraco.Storage.S3
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class BucketFileSystemComposer : IComposer
    {
        private const string AppSettingsKey = "BucketFileSystem";
        private readonly char[] Delimiters = "/".ToCharArray();

        public void Compose(Composition composition)
        {

            var bucketName = ConfigurationManager.AppSettings[$"{AppSettingsKey}:BucketName"];
            if (bucketName != null)
            {
                var config = CreateConfiguration();

                composition.RegisterUnique(config);
                composition.Register<IMimeTypeResolver>(new DefaultMimeTypeResolver());

                composition.SetMediaFileSystem((f) => new BucketFileSystem(
                    config,
                    f.GetInstance<IMimeTypeResolver>(),
                    null,
                    f.GetInstance<ILogger>(),
                    new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(config.Region))
                ));

                composition.Components().Append<BucketFileSystemComponent>();

            }

        }

        private BucketFileSystemConfig CreateConfiguration()
        {
            var bucketName = ConfigurationManager.AppSettings[$"{AppSettingsKey}:BucketName"];
            var bucketHostName = ConfigurationManager.AppSettings[$"{AppSettingsKey}:BucketHostname"];
            var bucketPrefix = ConfigurationManager.AppSettings[$"{AppSettingsKey}:BucketPrefix"].Trim(Delimiters);
            var region = ConfigurationManager.AppSettings[$"{AppSettingsKey}:Region"];
            bool.TryParse(ConfigurationManager.AppSettings[$"{AppSettingsKey}:DisableVirtualPathProvider"], out var disableVirtualPathProvider);

            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullOrEmptyException("BucketName", $"The AWS S3 Bucket File System is missing the value '{AppSettingsKey}:BucketName' from AppSettings");

            if (string.IsNullOrEmpty(bucketPrefix))
                throw new ArgumentNullOrEmptyException("BucketPrefix", $"The AWS S3 Bucket File System is missing the value '{AppSettingsKey}:BucketPrefix' from AppSettings");

            if (string.IsNullOrEmpty(region))
                throw new ArgumentNullOrEmptyException("Region", $"The AWS S3 Bucket File System is missing the value '{AppSettingsKey}:Region' from AppSettings");

            if (disableVirtualPathProvider && string.IsNullOrEmpty(bucketHostName))
                throw new ArgumentNullOrEmptyException("BucketHostname", $"The AWS S3 Bucket File System is missing the value '{AppSettingsKey}:BucketHostname' from AppSettings");

            return new BucketFileSystemConfig
            {
                BucketName = bucketName,
                BucketHostName = bucketHostName,
                BucketPrefix = bucketPrefix,
                Region = region,
                CannedACL = new S3CannedACL("public-read"),
                ServerSideEncryptionMethod = "",
                DisableVirtualPathProvider = disableVirtualPathProvider
            };
        }
    }
}
