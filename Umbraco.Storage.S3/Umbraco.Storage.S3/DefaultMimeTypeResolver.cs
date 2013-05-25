namespace Umbraco.Storage.S3
{
    public class DefaultMimeTypeResolver : IMimeTypeResolver
    {
        public string Resolve(string filename)
        {
            return System.Web.MimeMapping.GetMimeMapping(filename);
        }
    }
}
