/*
 * 
 * 
    This program transfers data from the system to s3
    Any type of file can be transferred.
    Credentials have to be adjusted -> AccessKey, SecretKey Endpoint Region
    
    NOTE: Use access key & secret key of the IAM user to keep your AWS account secure.
          Add User in IAM and then provide full access of S3 to that user. Then use its security credentials.
          
    Uploads any type of data to S3.
    The settings of the file in S3 will be similar to that of the folder.
 *
 *
 */


using System;
using Amazon.S3;
using Amazon.S3.Transfer;
using System.IO;

namespace CloudFolder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Transferring Data To S3......");

            /*
             * 
                Enter the credentials here : Secret Key
                                             Access Key
                                             Bucket Name
                                             Path of the directory which needsto be uploaded.
             *
             */

            string AccessKey = "Access key Here";
            string SecretKey = "Secret Key Here";
            string existingBucketName = "Bucket Name Here";
            string directoryPath = @"Location of the file which needs to be transferred";

            try
            {
                TransferUtility directoryTransferUtility = new TransferUtility(new AmazonS3Client(AccessKey, SecretKey, Amazon.RegionEndpoint.APSouth1));
                directoryTransferUtility.UploadDirectory(directoryPath, existingBucketName);
                directoryTransferUtility.UploadDirectory(directoryPath, existingBucketName, "*.*", SearchOption.AllDirectories);
                TransferUtilityUploadDirectoryRequest request = new TransferUtilityUploadDirectoryRequest
                {
                    BucketName = existingBucketName,
                    Directory = directoryPath,
                    SearchOption = SearchOption.AllDirectories,
                    SearchPattern = "*.*"
                };
                directoryTransferUtility.UploadDirectory(request);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("There is Some Problem");
                Console.WriteLine(e.Message, e.InnerException);
            }
            catch ( Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Transfer Complete");
            Console.ReadLine();
        }
    }
}
