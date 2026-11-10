using NUnit.Framework;
using Umbraco.Storage.S3.Services.Impl;

namespace Umbraco.Storage.S3.Tests
{
    [TestFixture]
    public class MimeTypeResolverTests
    {
        [Test]
        public void ResolveKnownExtension()
        {
            var actual = new DefaultMimeTypeResolver().Resolve("1001/media.jpg");
            Assert.That(actual, Is.EqualTo("image/jpeg"));
        }

        [Test]
        public void ResolveUnknownExtension()
        {
            var actual = new DefaultMimeTypeResolver().Resolve("1001/media.unknown");
            Assert.That(actual, Is.EqualTo("application/octet-stream"));
        }
    }
}
