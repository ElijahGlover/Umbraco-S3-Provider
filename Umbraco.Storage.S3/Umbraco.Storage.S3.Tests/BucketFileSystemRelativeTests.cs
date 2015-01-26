using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Umbraco.Storage.S3.Tests
{
    [TestClass]
    public class BucketFileSystemRelativeTests
    {
        private BucketFileSystem CreateProvider(Mock<WrappedAmazonS3Client> mock)
        {
            var logHelperMock = new Mock<ILogHelper>();
            var mimeTypeHelper = new Mock<IMimeTypeResolver>();
            return new BucketFileSystem("test", string.Empty, "media", string.Empty) {
                ClientFactory = () => mock.Object,
                LogHelper = logHelperMock.Object,
                MimeTypeResolver = mimeTypeHelper.Object
            };
        }

        [TestMethod]
        public void ResolveFullPath()
        {
            //Arrange
            var provider = CreateProvider(null);

            //Act
            var actual = provider.GetFullPath("1001/media.jpg");

            //Assert
            Assert.AreEqual("1001/media.jpg", actual);
        }

        [TestMethod]
        public void ResolveUrlPath()
        {
            //Arrange
            var provider = CreateProvider(null);

            //Act
            var actual = provider.GetUrl("1001/media.jpg");

            //Assert
            Assert.AreEqual("/media/1001/media.jpg", actual);
        }

        [TestMethod]
        public void ResolveRelativePath()
        {
            //Arrange
            var provider = CreateProvider(null);

            //Act
            var actual = provider.GetRelativePath("1001/media.jpg");

            //Assert
            Assert.AreEqual("1001/media.jpg", actual);
        }
    }
}
