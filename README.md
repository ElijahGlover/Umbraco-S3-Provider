# Umbraco-S3-Provider

[Amazon Web Services S3](http://aws.amazon.com/s3/) IFileSystem provider for Umbraco 7. Used to offload static files (ie media/templates/stylesheets) to the cloud! You don't have to be hosting your code in EC2 to get the benefits like handling large media libraries, freeing up disk space and removing static files from your deployment process.

Most of the code floating around the internet is slight modifications to code contained in this repository. If your not 100% happy with it, submit a pull request or open dialog with the rest of the community. It's heavily unit tested against the official AWS S3 .Net API bindings.

[![Build status](https://ci.appveyor.com/api/projects/status/1p6qllpo5ep42ys9?svg=true)](https://ci.appveyor.com/project/ElijahGlover/umbraco-s3-provider)

## Installation & Configuration

Install via NuGet.org - Packaged upon every comment thanks to the guys at [AppVeyor](http://www.appveyor.com/)
```powershell
Install-Package Umbraco.Storage.S3
```

Update ~/Config/FileSystemProviders.config
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
It's also a good idea to use [AWS best security practices](http://docs.aws.amazon.com/general/latest/gr/aws-access-keys-best-practices.html). Like not using your root access account, use short lived access keys and don't EVER commit them to source control.

If you aren't using EC2/ElasticBeanstalk to access generated temporary keys, you can put them within your application (AppSettings in web.config)

```xml
<?xml version="1.0"?>
<configuration>
  <appSettings>
    <add key="AWSAccessKey" value="" />
    <add key="AWSSecretKey" value="" />
  </appSettings>
</configuration>
```

## Virtual Path Provider
If your wanting to serve files transparently from your domain or serve templates/stylesheets directly from S3 it's possible by using a custom [Virtual Path Provider](https://msdn.microsoft.com/en-us/library/system.web.hosting.virtualpathprovider%28v=vs.110%29.aspx) included.
Before you enable this option you might want to read how Virtual Path Providers affect performance/caching as it differs from IIS's [unmanaged handler](http://www.paraesthesia.com/archive/2011/05/02/when-staticfilehandler-is-not-staticfilehandler.aspx/).

- Set bucketHostName value to empty inside ~/Config/FileSystemProviders.config *(This will prevent the S3 hostname from being prepended when uploaded from within Umbraco)*
- Create/modify applications ~/global.asax
```c#
using Umbraco.Storage.S3;
      
public class Global : UmbracoApplication
{
   protected override void OnApplicationStarting(object sender, EventArgs e)
   {
      FileSystemVirtualPathProvider.ConfigureMedia();
      base.OnApplicationStarting(sender, e);
   }
}
```
- Add static file handler to serve all files from the media directory
```xml
<?xml version="1.0"?>
  <configuration>
    <location path="Media">
      <system.webServer>
        <handlers>
          <remove name="StaticFileHandler" />
          <add name="StaticFileHandler" path="*" verb="*" preCondition="integratedMode" type="System.Web.StaticFileHandler" />
        </handlers>
      </system.webServer>
    </location>
  </configuration>
  ```
