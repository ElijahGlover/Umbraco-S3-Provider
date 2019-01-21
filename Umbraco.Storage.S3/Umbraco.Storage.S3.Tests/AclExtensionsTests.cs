using NUnit.Framework;
using Umbraco.Storage.S3.Extensions;
using Amazon.S3;

namespace Umbraco.Storage.S3.Tests
{
    [TestFixture]
    class AclExtensionsTests
    {
        [Test]
        public void CannedACLNull()
        {
            var actual = AclExtensions.ParseCannedAcl(null);
            Assert.AreEqual(S3CannedACL.PublicRead, actual);
        }

        [Test]
        public void CannedACLEmpty()
        {
            var actual = AclExtensions.ParseCannedAcl("");
            Assert.AreEqual(S3CannedACL.PublicRead, actual);
        }

        [Test]
        public void CannedACLInvalid()
        {
            var actual = AclExtensions.ParseCannedAcl("invalid-value");
            Assert.AreEqual(S3CannedACL.PublicRead, actual);
        }

        [Test]
        public void CannedACLValid()
        {
            var publicRead = AclExtensions.ParseCannedAcl("public-read");
            Assert.AreEqual(S3CannedACL.PublicRead, publicRead);

            var noACL = AclExtensions.ParseCannedAcl("NoACL");
            Assert.AreEqual(S3CannedACL.NoACL, noACL);

            var publicReadWrite = AclExtensions.ParseCannedAcl("public-read-write");
            Assert.AreEqual(S3CannedACL.PublicReadWrite, publicReadWrite);
        }

    }
}
