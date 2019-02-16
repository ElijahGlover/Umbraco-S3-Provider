using Amazon.S3;
using System.Linq;

namespace Umbraco.Storage.S3.Extensions
{
    public static class EncryptionExtensions
    {
        private static string[] ValidServerSideEncryptionMethods = new string[]
        {
            "AES256",
            "aws:kms",
            "",
        };

        public static ServerSideEncryptionMethod ParseServerSideEncryptionMethod(string ssemParam)
        {
            if (string.IsNullOrEmpty(ssemParam))
            {
                return ServerSideEncryptionMethod.None;
            }

            return ValidServerSideEncryptionMethods.Contains(ssemParam)
                ? ServerSideEncryptionMethod.FindValue(ssemParam)
                : ServerSideEncryptionMethod.None;
        }
    }
}
