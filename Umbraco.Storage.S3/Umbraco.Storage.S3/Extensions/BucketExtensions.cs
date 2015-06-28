namespace Umbraco.Storage.S3
{
    public static class BucketExtensions
    {
        public static string ParseBucketHostName(string hostname)
        {
            if (string.IsNullOrEmpty(hostname))
                return "/";

            return hostname.EndsWith("/")
                ? hostname
                : string.Concat(hostname, "/");
        }

        public static string ParseBucketPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return string.Empty;
            prefix = prefix.Replace("\\", "/");
            if (prefix == "/")
                return string.Empty;
            if (prefix.StartsWith("/"))
                prefix = prefix.Substring(1);
            return prefix.EndsWith("/")
                ? prefix
                : string.Concat(prefix, "/");
        }
    }
}
