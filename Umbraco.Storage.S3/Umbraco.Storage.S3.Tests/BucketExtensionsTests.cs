using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Umbraco.Storage.S3.Tests
{
    [TestClass]
    public class BucketExtensionsTests
    {
        [TestMethod]
        public void BucketHostnameTrailingSlash()
        {
            //Arrange
            const string hostname = "http://umbraco6test.s3-website-ap-southeast-2.amazonaws.com";
            //Act
            var actual = BucketExtensions.ParseBucketHostName(hostname);
            //Assert
            Assert.AreEqual("http://umbraco6test.s3-website-ap-southeast-2.amazonaws.com/", actual);
        }

        [TestMethod]
        public void NullBucketPrefix()
        {
            //Act
            var actual = BucketExtensions.ParseBucketPrefix(null);
            //Assert
            Assert.AreEqual(string.Empty, actual);
        }

        [TestMethod]
        public void BucketPrefix()
        {
            //Act
            var actual = BucketExtensions.ParseBucketPrefix("media");
            //Assert
            Assert.AreEqual("media/", actual);
        }
    }
}
