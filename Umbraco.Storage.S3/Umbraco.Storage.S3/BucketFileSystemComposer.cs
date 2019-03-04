using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Umbraco.Storage.S3
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class BucketFileSystemComposer : IComposer
    {
        public void Compose(Composition composition)
        {
 
        }
    }
}
