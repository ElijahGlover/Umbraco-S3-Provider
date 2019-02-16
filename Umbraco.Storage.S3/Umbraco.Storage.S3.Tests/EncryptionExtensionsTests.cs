using NUnit.Framework;
using Umbraco.Storage.S3.Extensions;
using Amazon.S3;

namespace Umbraco.Storage.S3.Tests
{
    [TestFixture]
    class EncryptionExtensionsTests
    {
        [Test]
        public void ServerSideEncryptionMethodNull()
        {
            var actual = EncryptionExtensions.ParseServerSideEncryptionMethod(null);
            Assert.AreEqual(ServerSideEncryptionMethod.None, actual);
        }

        [Test]
        public void ServerSideEncryptionMethodEmpty()
        {
            var actual = EncryptionExtensions.ParseServerSideEncryptionMethod("");
            Assert.AreEqual(ServerSideEncryptionMethod.None, actual);
        }

        [Test]
        public void ServerSideEncryptionMethodInvalid()
        {
            var actual = EncryptionExtensions.ParseServerSideEncryptionMethod("invalid-value");
            Assert.AreEqual(ServerSideEncryptionMethod.None, actual);
        }

        [Test]
        public void ServerSideEncryptionMethodValid()
        {
            var encrypt_AES256 = EncryptionExtensions.ParseServerSideEncryptionMethod("AES256");
            Assert.AreEqual(ServerSideEncryptionMethod.AES256, encrypt_AES256);

            var encrypt_AWSKMS = EncryptionExtensions.ParseServerSideEncryptionMethod("AWSKMS");
            Assert.AreEqual(ServerSideEncryptionMethod.AWSKMS, encrypt_AWSKMS);

            var encrypt_None = EncryptionExtensions.ParseServerSideEncryptionMethod("None");
            Assert.AreEqual(ServerSideEncryptionMethod.None, encrypt_None);
        }

    }
}
