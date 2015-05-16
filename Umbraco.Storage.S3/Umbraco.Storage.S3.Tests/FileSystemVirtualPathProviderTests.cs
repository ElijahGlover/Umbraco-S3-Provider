using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Umbraco.Core.IO;

namespace Umbraco.Storage.S3.Tests
{
    [TestFixture]
    public class FileSystemVirtualPathProviderTests
    {
        [Test]
        public void FilePathPrefixFormatted()
        {
            var fileProvider = new Mock<IFileSystem>();
            var provider = new FileSystemVirtualPathProvider("media", new Lazy<IFileSystem>(() => fileProvider.Object));

            Assert.AreEqual(provider.PathPrefix, "/media/");
        }

        [Test]
        public void FilePathShouldBeExecuted()
        {
            var fileProvider = new Mock<IFileSystem>();
            var provider = new FileSystemVirtualPathProvider("media", new Lazy<IFileSystem>(() => fileProvider.Object));

            var result = provider.GetFile("~/media/1001/media.jpg");
        }

        [Test]
        public void FilePathShouldBeNotIgnored()
        {
            var fileProvider = new Mock<IFileSystem>();
            var provider = new FileSystemVirtualPathProvider("media", new Lazy<IFileSystem>(() => fileProvider.Object));

            var result = provider.GetFile("~/styles/main.css");
        }
    }
}
