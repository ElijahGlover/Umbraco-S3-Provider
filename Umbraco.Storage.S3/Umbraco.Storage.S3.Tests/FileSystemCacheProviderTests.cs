using System;
using System.IO;
using Moq;
using NUnit.Framework;

namespace Umbraco.Storage.S3.Tests
{
    [TestFixture]
    public class FileSystemCacheProviderTests
    {
        [Test]
        public void ResolveStreamForPathWithinTimeToLive()
        {
            //Arrange
            var expectedStream = new MemoryStream();
            var fileSystem = new Mock<IFileSystemWrapper>();
            fileSystem.Setup(p => p.Open("c:\\temp\\5b84e26ad560373eb0a33d169043b3fbb17e37a5"))
                .Returns(expectedStream);
            fileSystem.Setup(p => p.MapPath("~/path/5b84e26ad560373eb0a33d169043b3fbb17e37a5"))
                .Returns("c:\\temp\\5b84e26ad560373eb0a33d169043b3fbb17e37a5");
            fileSystem.Setup(p => p.GetLastAccessTimeUtc("c:\\temp\\5b84e26ad560373eb0a33d169043b3fbb17e37a5"))
                .Returns(DateTime.UtcNow.AddMinutes(-15));

            var cacheProvider = new FileSystemCacheProvider(TimeSpan.FromMinutes(10), "~/path/", fileSystem.Object);

            //Act
            var stream = cacheProvider.Resolve("media/1001/media.jpg");

            //Assert
            Assert.AreEqual(expectedStream, stream);

            fileSystem.Verify();
        }

        [Test]
        public void ResolveStreamForPathExpiredTimeToLive()
        {
            //Arrange
            var fileSystem = new Mock<IFileSystemWrapper>();
            fileSystem.Setup(p => p.MapPath("~/path/5b84e26ad560373eb0a33d169043b3fbb17e37a5"))
                .Returns("c:\\temp\\5b84e26ad560373eb0a33d169043b3fbb17e37a5");
            fileSystem.Setup(p => p.GetLastAccessTimeUtc("c:\\temp\\5b84e26ad560373eb0a33d169043b3fbb17e37a5"))
                .Returns(DateTime.UtcNow.AddMinutes(-7));

            var cacheProvider = new FileSystemCacheProvider(TimeSpan.FromMinutes(10), "~/path/", fileSystem.Object);

            //Act
            var stream = cacheProvider.Resolve("media/1001/media.jpg");

            //Assert
            Assert.IsNull(stream);
        }

        [Test]
        public void PersistStreamAndReset()
        {
            //Arrange
            var expectedStream = new MemoryStream();
            var fileSystem = new Mock<IFileSystemWrapper>();
            fileSystem.Setup(p => p.MapPath("~/path/5b84e26ad560373eb0a33d169043b3fbb17e37a5"))
                .Returns("c:\\temp\\5b84e26ad560373eb0a33d169043b3fbb17e37a5");
            fileSystem.Setup(p => p.MapPath("~/path/"))
                .Returns("c:\\temp\\");

            var cacheProvider = new FileSystemCacheProvider(TimeSpan.FromMinutes(10), "~/path/", fileSystem.Object);

            //Act
            cacheProvider.Persist("media/1001/media.jpg", expectedStream);

            //Assert
            fileSystem.Verify(p => p.EnsureDirectory("c:\\temp\\"), Times.AtLeastOnce);
            fileSystem.Verify(p => p.Create("c:\\temp\\5b84e26ad560373eb0a33d169043b3fbb17e37a5", expectedStream));
        }
    }
}
