namespace Umbraco.Storage.S3
{
    public interface IMimeTypeResolver
    {
        string Resolve(string filename);
    }
}
