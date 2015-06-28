using System.IO;

namespace Umbraco.Storage.S3
{
    public interface ICacheProvider
    {
        void Persist(string key, Stream stream);
        Stream Resolve(string key);
    }
}
