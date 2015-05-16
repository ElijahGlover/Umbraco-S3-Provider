using System;
using System.Collections.Generic;
using System.IO;
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
            Assert.IsNotNull(result);
        }

        [Test]
        public void FilePathShouldBeNotIgnored()
        {
            var fileProvider = new Mock<IFileSystem>();
            var provider = new FileSystemVirtualPathProvider("media", new Lazy<IFileSystem>(() => fileProvider.Object));

            var result = provider.GetFile("~/styles/main.css");
            Assert.IsNull(result);
        }

        [Test]
        public void ProviderShouldCallFileSystemOpen()
        {
            var stream = new MemoryStream();

            var fileProvider = new Mock<IFileSystem>();
            fileProvider.Setup(p => p.OpenFile("1001/media.jpg")).Returns(stream);
            var provider = new FileSystemVirtualPathProvider("media", new Lazy<IFileSystem>(() => fileProvider.Object));

            var result = provider.GetFile("~/media/1001/media.jpg");
            var streamResult = result.Open();

            Assert.AreEqual(stream, streamResult);
            fileProvider.Verify(p => p.OpenFile("1001/media.jpg"), Times.Once);
        }

        [Test]
        public void ProviderShouldCallFileSystemFileExists()
        {
            var stream = new MemoryStream();

            var fileProvider = new Mock<IFileSystem>();
            fileProvider.Setup(p => p.OpenFile("1001/media.jpg")).Returns(stream);
            var provider = new FileSystemVirtualPathProvider("media", new Lazy<IFileSystem>(() => fileProvider.Object));

            var result = provider.FileExists("~/media/1001/media.jpg");
            fileProvider.Verify(p => p.FileExists("1001/media.jpg"), Times.Once);
        }
    }
}
