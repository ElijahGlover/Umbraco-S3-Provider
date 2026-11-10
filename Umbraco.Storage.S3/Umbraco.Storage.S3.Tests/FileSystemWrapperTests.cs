using System.IO;
using NUnit.Framework;
using Umbraco.Storage.S3.Services.Impl;

namespace Umbraco.Storage.S3.Tests
{
    [TestFixture]
    public class FileSystemWrapperTests
    {
        private static readonly string ContentRoot = Path.Combine(Path.GetTempPath(), "site");

        [Test]
        public void MapPathVirtual()
        {
            var wrapper = new FileSystemWrapper(ContentRoot);

            var actual = wrapper.MapPath("~/umbraco/Data/TEMP/S3Cache/5b84e26ad560373eb0a33d169043b3fbb17e37a5");

            var expected = Path.Combine(ContentRoot, "umbraco", "Data", "TEMP", "S3Cache", "5b84e26ad560373eb0a33d169043b3fbb17e37a5");
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void MapPathRelative()
        {
            var wrapper = new FileSystemWrapper(ContentRoot);

            var actual = wrapper.MapPath("S3Cache/");

            Assert.That(actual, Is.EqualTo(Path.Combine(ContentRoot, "S3Cache") + Path.DirectorySeparatorChar));
        }

        [Test]
        public void MapPathRooted()
        {
            var wrapper = new FileSystemWrapper(ContentRoot);
            var rooted = Path.Combine(Path.GetTempPath(), "S3Cache");

            var actual = wrapper.MapPath(rooted);

            Assert.That(actual, Is.EqualTo(rooted));
        }
    }
}
