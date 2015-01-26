using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Amazon.S3.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Umbraco.Storage.S3.Exception;

namespace Umbraco.Storage.S3.Tests
{
    [TestClass]
    public class BucketFileSystemAbsoluteTests
    {
        private BucketFileSystem CreateProvider(Mock<WrappedAmazonS3Client> mock)
        {
            var logHelperMock = new Mock<ILogHelper>();
            var mimeTypeHelper = new Mock<IMimeTypeResolver>();
            return new BucketFileSystem("test", "http://test.amazonaws.com", "media", string.Empty) {
                ClientFactory = () => mock.Object,
                LogHelper = logHelperMock.Object,
                MimeTypeResolver = mimeTypeHelper.Object
            };
        }

        [TestMethod]
        public void GetDirectories()
        {
            //Arrange
            var response = new ListObjectsResponse { IsTruncated = false };
            response.CommonPrefixes.AddRange(new[] { "media/directory1", "media/directory2", "media/directory3" });

            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.ListObjects(It.Is<ListObjectsRequest>(req => req.Delimiter == "/" && req.Prefix == "media/")))
                      .Returns(response);

            var provider = CreateProvider(clientMock);

            //Act
            var actual = provider.GetDirectories("/");

            //Assert
            var expected = new[] {"directory1/", "directory2/", "directory3/"};
            Assert.IsTrue(expected.SequenceEqual(actual));
            clientMock.VerifyAll();
        }

        [TestMethod]
        public void GetDirectoriesWithContinuationMarker()
        {
            //Arrange
            var response1 = new ListObjectsResponse { IsTruncated = true, NextMarker = "Marker1" };
            response1.CommonPrefixes.AddRange(new[] { "media/directory1", "media/directory2", "media/directory3" });

            var response2 = new ListObjectsResponse { IsTruncated = false };
            response2.CommonPrefixes.AddRange(new[] { "media/directory4", "media/directory5", "media/directory6" });

            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.ListObjects(It.Is<ListObjectsRequest>(req => req.Prefix == "media/" && req.Delimiter == "/" && req.Marker == null)))
                      .Returns(response1);
            clientMock.Setup(p => p.ListObjects(It.Is<ListObjectsRequest>(req => req.Prefix == "media/" && req.Delimiter == "/" && req.Marker == "Marker1")))
                      .Returns(response2);

            var provider = CreateProvider(clientMock);

            //Act
            var actual = provider.GetDirectories("/");

            //Assert
            var expected = new[] { "directory1/", "directory2/", "directory3/", "directory4/", "directory5/", "directory6/" };
            Assert.IsTrue(expected.SequenceEqual(actual));
            clientMock.VerifyAll();
        }

        [TestMethod]
        public void AddFile()
        {
            //Arrange
            var steam = new MemoryStream();
            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.PutObject(It.Is<PutObjectRequest>(req => req.Key == "media/1001/media.jpg")))
                      .Returns(new PutObjectResponse());

            var provider = CreateProvider(clientMock);

            //Act
            provider.AddFile("/1001/media.jpg", steam);

            //Assert
            clientMock.VerifyAll();
        }

        [TestMethod]
        public void OpenFile()
        {
            //Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test123"));
            var response = new GetObjectResponse {
                ResponseStream = stream
            };

            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.GetObject(It.Is<GetObjectRequest>(req => req.Key == "media/1001/media.jpg")))
                      .Returns(response);

            var provider = CreateProvider(clientMock);

            //Act
            var actual = provider.OpenFile("1001/media.jpg");

            //Assert
            Assert.AreEqual(new StreamReader(actual).ReadToEnd(), "Test123");
            clientMock.VerifyAll();
        }

        [TestMethod]
        public void DeleteFile()
        {
            //Arrange
            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.DeleteObject(It.Is<DeleteObjectRequest>(req => req.Key == "media/1010/media.jpg")))
                      .Returns(new DeleteObjectResponse { DeleteMarker = "Marker1" });

            var provider = CreateProvider(clientMock);

            //Act
            provider.DeleteFile("/1010/media.jpg");

            //Assert
            clientMock.VerifyAll();
        }

        [TestMethod]
        public void DeleteDirectory()
        {
            //Arrange
            var response = new ListObjectsResponse { IsTruncated = false };
            response.S3Objects.AddRange(new [] {
                new S3Object { Key = "media/abc/object1" },
                new S3Object { Key = "media/abc/object2" }
            });

            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.ListObjects(It.Is<ListObjectsRequest>(req => req.Prefix == "media/abc")))
                      .Returns(response);
            clientMock.Setup(p => p.DeleteObjects(It.IsAny<DeleteObjectsRequest>()))
                      .Returns(new DeleteObjectsResponse { DeletedObjects = new List<DeletedObject>() });

            var provider = CreateProvider(clientMock);

            //Act
            provider.DeleteDirectory("/abc");

            //Assert
            clientMock.VerifyAll();
        }

        [TestMethod]
        public void DeleteDirectoryWithContinuationMarker()
        {
            //Arrange
            var response1 = new ListObjectsResponse { IsTruncated = true, NextMarker = "Marker1" };
             response1.S3Objects.AddRange(new [] {
                new S3Object { Key = "media/object1" },
                new S3Object { Key = "media/object2" }
            });
            var response2 = new ListObjectsResponse { IsTruncated = false };
            response2.S3Objects.AddRange(new [] {
                new S3Object { Key = "media/object3" },
                new S3Object { Key = "media/object4" }
            });

            var response3 = new DeleteObjectsResponse();
            response3.DeletedObjects = new List<DeletedObject>(new[] {
                new DeletedObject { DeleteMarker = true, Key = "media/object1" },
                new DeletedObject { DeleteMarker = true, Key = "media/object2" },
                new DeletedObject { DeleteMarker = true, Key = "media/object3" },
                new DeletedObject { DeleteMarker = true, Key = "media/object4" }
            });

            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.ListObjects(It.Is<ListObjectsRequest>(req => req.Prefix == "media/" && req.Marker == null)))
                      .Returns(response1);
            clientMock.Setup(p => p.ListObjects(It.Is<ListObjectsRequest>(req => req.Prefix == "media/" && req.Marker == "Marker1")))
                      .Returns(response2);
            clientMock.Setup(p => p.DeleteObjects(It.IsAny<DeleteObjectsRequest>()))
                      .Returns(response3);

            var provider = CreateProvider(clientMock);

            //Act
            provider.DeleteDirectory("/");

            //Assert
            clientMock.VerifyAll();
        }

        [TestMethod]
        public void DirectoryExists()
        {
            //Arrange
            var response = new ListObjectsResponse { IsTruncated = false };
            response.S3Objects.AddRange(new [] {
                new S3Object { Key = "abc/object1" },
                new S3Object { Key = "abc/object2" }
            });

            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.ListObjects(It.Is<ListObjectsRequest>(req => req.MaxKeys == 1)))
                      .Returns(response);

            var provider = CreateProvider(clientMock);

            //Act
            var actual = provider.DirectoryExists("/abc");

            //Arrange
            Assert.IsTrue(actual);
            clientMock.VerifyAll();
        }

        [TestMethod]
        public void DirectoryExistsNoObjects()
        {
            //Arrange
            var listResponse = new ListObjectsResponse { IsTruncated = false };

            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.ListObjects(It.Is<ListObjectsRequest>(req => req.MaxKeys == 1)))
                      .Returns(listResponse);

            var provider = CreateProvider(clientMock);

            //Act
            var actual = provider.DirectoryExists("/abc");

            //Arrange
            Assert.IsFalse(actual);
            clientMock.VerifyAll();
        }

        [TestMethod]
        public void GetFiles()
        {
            //Arrange
            var response = new ListObjectsResponse { IsTruncated = false };
            response.S3Objects.AddRange(new[] {
                new S3Object {Key = "media/1001/object1"},
                new S3Object {Key = "media/1001/object2"},
                new S3Object {Key = "media/1001/object3"}
            });

            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.ListObjects(It.Is<ListObjectsRequest>(req => req.Delimiter == "/" && req.Prefix == "media/1001/")))
                      .Returns(response);

            var provider = CreateProvider(clientMock);

            //Act
            var actual = provider.GetFiles("1001/");

            //Assert
            var expected = new[] { "1001/object1", "1001/object2", "1001/object3" };
            Assert.IsTrue(expected.SequenceEqual(actual));
            clientMock.VerifyAll();
        }

        [TestMethod]
        public void GetFilesWithContinuationMarker()
        {
            //Arrange
            var response1 = new ListObjectsResponse { IsTruncated = true, NextMarker = "marker1" };
            response1.S3Objects.AddRange(new [] {
                new S3Object { Key = "media/object1" },
                new S3Object { Key = "media/object2" }
            });
            var response2 = new ListObjectsResponse { IsTruncated = false };
            response2.S3Objects.AddRange(new [] {
                new S3Object { Key = "media/object3" },
                new S3Object { Key = "media/object4" }
            });

            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.ListObjects(It.Is<ListObjectsRequest>(req => req.Prefix == "media/1001/" && req.Delimiter == "/" && req.Marker == null)))
                      .Returns(response1);
            clientMock.Setup(p => p.ListObjects(It.Is<ListObjectsRequest>(req => req.Prefix == "media/1001/" && req.Delimiter == "/" && req.Marker == "marker1")))
                      .Returns(response2);

            var provider = CreateProvider(clientMock);

            //Act
            var actual = provider.GetFiles("1001/");

            //Assert
            var expected = new[] { "object1", "object2", "object3", "object4" };
            Assert.IsTrue(expected.SequenceEqual(actual));
            clientMock.VerifyAll();
        }

        [TestMethod]
        public void FileExists()
        {
            //Arrange
            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(req => req.Key == "media/1001/media.jpg")))
                      .Returns(new GetObjectMetadataResponse());

            var provider = CreateProvider(clientMock);

            //Act
            var actual = provider.FileExists("/1001/media.jpg");

            //Assert
            Assert.IsTrue(actual);
            clientMock.VerifyAll();
        }

        [TestMethod]
        public void FileExistsThrowsNotFound()
        {
            //Arrange
            var clientMock = new Mock<WrappedAmazonS3Client>();
            clientMock.Setup(p => p.GetObjectMetadata(It.Is<GetObjectMetadataRequest>(req => req.Key == "media/1001/media.jpg")))
                      .Throws(new ObjectNotFoundException("media/1001/media.jpg"));

            var provider = CreateProvider(clientMock);

            //Act
            var actual = provider.FileExists("/1001/media.jpg");

            //Assert
            Assert.IsFalse(actual);
            clientMock.VerifyAll();
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
            Assert.AreEqual("http://test.amazonaws.com/media/1001/media.jpg", actual);
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
