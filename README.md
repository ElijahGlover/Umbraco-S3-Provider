# Umbraco-S3-Provider

[Amazon Web Services S3](http://aws.amazon.com/s3/) IFileSystem provider for Umbraco 7. Used to offload static files (ie media/templates/stylesheets) to the cloud! You don't have to be hosting your code in EC2 to get the benefits of handling large media libraries

Most of the code floating around the internet is slight modifications to code contained in this repository. If your not 100% happy with it, submit a pull request or open dialog with the rest of the community.

## Installation & Configuration

Install via NuGet.org
```powershell
Install-Package Umbraco.Storage.S3
```

~/Config/FileSystemProviders.config
```xml
<?xml version="1.0"?>
<FileSystemProviders>
  <Provider alias="media" type="Umbraco.Storage.S3.BucketFileSystem, Umbraco.Storage.S3">
    <Parameters>
      <!-- S3 Bucket Name - Used For Making API Requests -->
      <add key="bucketName" value="{Your Unique Bucket Name}" />
      <!-- S3 Bucket Hostname - Used For Storage In Umbraco's Database (Can be blank if using the bundled file provider) -->
      <add key="bucketHostName" value="{s3.yourwebsite.com}" />
      <!-- S3 Object Key Prefix -->
      <add key="bucketKeyPrefix" value="media" />
      <!-- AWS Region Endpoint (us-east-1/us-west-1/ap-southeast-2) -->
      <add key="region" value="us-west-2" />
    </Parameters>
  </Provider>
</FileSystemProviders>
```
Ok so where are the [IAM access keys?](http://docs.aws.amazon.com/IAM/latest/UserGuide/ManagingCredentials.html) Depending on how you host your project they already exist if deploying inside an EC2 instance via environment variables specified during deployment and creation of infrastructure.
Please use [AWS best practices](http://docs.aws.amazon.com/general/latest/gr/aws-access-keys-best-practices.html). Like not using your root access account, use short lived access keys and don't EVER commit them to source control.

If you aren't using AWS to configure access keys (AppSettings in web.config)

```xml
<configuration>
  <appSettings>
    <add key="AWSAccessKey" value="" />
    <add key="AWSSecretKey" value="" />
  </appSettings>
</configuration>
```
