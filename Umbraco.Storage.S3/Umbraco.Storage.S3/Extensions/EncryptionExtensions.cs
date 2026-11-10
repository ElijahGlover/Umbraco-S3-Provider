using Amazon.S3;
using System;
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

            if (!ValidServerSideEncryptionMethods.Contains(ssemParam))
            {
                throw new ArgumentException(string.Format("Unrecognised serverSideEncryptionMethod '{0}'. Expected AES256 or aws:kms", ssemParam), "ssemParam");
            }

            return ServerSideEncryptionMethod.FindValue(ssemParam);
        }
    }
}
