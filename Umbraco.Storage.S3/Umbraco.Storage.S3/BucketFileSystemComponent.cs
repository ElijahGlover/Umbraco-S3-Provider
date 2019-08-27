using Umbraco.Core.Composing;
using Umbraco.Core.IO;

namespace Umbraco.Storage.S3
{
    public class BucketFileSystemComponent : IComponent
    {
        private readonly SupportingFileSystems supportingFileSystems;
        private readonly BucketFileSystemConfig config;

        public BucketFileSystemComponent(SupportingFileSystems supportingFileSystems, BucketFileSystemConfig config)
        {
            this.supportingFileSystems = supportingFileSystems;
            this.config = config;
        }

        public void Initialize()
        {
            var fs = supportingFileSystems.For<IMediaFileSystem>() as BucketFileSystem;
            if (!config.DisableVirtualPathProvider && fs != null)
            {
                FileSystemVirtualPathProvider.ConfigureMedia();
            }
        }

        public void Terminate()
        {

        }
    }
}
