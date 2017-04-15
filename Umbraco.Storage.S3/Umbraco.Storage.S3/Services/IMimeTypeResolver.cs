namespace Umbraco.Storage.S3.Services
{
    public interface IMimeTypeResolver
    {
        string Resolve(string filename);
    }
}
