using Amazon.S3;
using System.Linq;

namespace Umbraco.Storage.S3.Extensions
{
    public static class AclExtensions
    {
        private static string[] ValidAcls = new string[]
        {
            "private",
            "public-read",
            "public-read-write",
            "aws-exec-read",
            "authenticated-read",
            "bucket-owner-read",
            "bucket-owner-full-control",
            "NoACL",
        };

        public static S3CannedACL ParseCannedAcl(string aclParam)
        {
            if (string.IsNullOrEmpty(aclParam))
            {
                return S3CannedACL.PublicRead;
            }

            return ValidAcls.Contains(aclParam)
                ? S3CannedACL.FindValue(aclParam)
                : S3CannedACL.PublicRead;
        }
    }
}
