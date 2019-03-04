using System.IO;

namespace Umbraco.Storage.S3.Services
{
    public interface IFileCacheProvider
    {
        bool Exists(string key);
        void Persist(string key, Stream stream);
        Stream Resolve(string key);
    }
}
