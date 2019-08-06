using Amazon.S3;
using System.Configuration;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Storage.S3.Services;

namespace Umbraco.Storage.S3
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class BucketFileSystemComposer : IComposer
    {
        private const string AppSettingsKey = "BucketFileSystem";
        private const string ProviderAlias = "media";
        public void Compose(Composition composition)
        {

            var bucketName = ConfigurationManager.AppSettings[$"{AppSettingsKey}:BucketName"];
            if (bucketName != null)
            {
                var config = CreateConfiguration();

                composition.RegisterUnique(config);
                composition.Register<IMimeTypeResolver>(new DefaultMimeTypeResolver());

                composition.SetMediaFileSystem((f) => new BucketFileSystem(config, f.GetInstance<IMimeTypeResolver>(), null, f.GetInstance<ILogger>())); ;

                composition.Components().Append<BucketFileSystemComponent>();

            }

        }

        private BucketFileSystemConfig CreateConfiguration()
        {
            var bucketName = ConfigurationManager.AppSettings[$"{AppSettingsKey}:BucketName"];
            var bucketHostName = ConfigurationManager.AppSettings[$"{AppSettingsKey}:BucketHostname"];
            var bucketPrefix = ConfigurationManager.AppSettings[$"{AppSettingsKey}:BucketPrefix"];
            var region = ConfigurationManager.AppSettings[$"{AppSettingsKey}:Region"];

            return new BucketFileSystemConfig
            {
                BucketName = bucketName,
                BucketHostName = bucketHostName,
                BucketPrefix = bucketPrefix,
                Region = region,
                CannedACL = new Amazon.S3.S3CannedACL("public-read"),
                ServerSideEncryptionMethod = ""
            };
        }
    }
}
