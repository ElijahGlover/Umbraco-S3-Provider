using System.IO;

namespace Umbraco.Storage.S3.Services
{
    public interface ICacheProvider
    {
        bool Exists(string key);
        void Persist(string key, Stream stream);
        Stream Resolve(string key);
    }
}
