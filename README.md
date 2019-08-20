# Umbraco S3 Provider

[Amazon Web Services S3](http://aws.amazon.com/s3/) IFileSystem provider for Umbraco 8. Used to offload media to the cloud! You don't have to be hosting your code in EC2 to get the benefits like handling large media libraries, freeing up disk space and removing static files from your deployment process.

Most of the code floating around the internet are slight modifications to code contained in this repository. If you're not 100% happy with it, submit a pull request or open dialog with the rest of the community. It's heavily unit tested against the official AWS S3 .Net API bindings.

[![Build status](https://ci.appveyor.com/api/projects/status/1p6qllpo5ep42ys9?svg=true)](https://ci.appveyor.com/project/ElijahGlover/umbraco-s3-provider)

## Installation & Configuration

Install via NuGet.org - Packaged upon every comment thanks to the guys at [AppVeyor](http://www.appveyor.com/)
```powershell
Install-Package Umbraco.Storage.S3
```

Add the following keys to `~/Web.config`
```xml
<?xml version="1.0"?>
<configuration>
  <appSettings>
    <add key="BucketFileSystem:Region" value="" />
    <add key="BucketFileSystem:BucketPrefix" value="media" />
    <add key="BucketFileSystem:BucketName" value="" />
    <add key="BucketFileSystem:BucketHostname" value="" />
    <add key="BucketFileSystem:DisableVirtualPathProvider" value="false" />
  </appSettings>
</configuration>
```
`Region`, `BucketPrefix`, and `BucketName` are always required.

`DisableVirtualPathProvider` can be left empty as it will default to `false`.

If `DisableVirtualPathProvider` is set to `true`, you must include `BucketHostname`. However, if `DisableVirtualPathProvider` is set to `false` then `BucketHostname` can be left empty, as it doesn't have any effect.

If `DisableVirtualPathProvider` is set to `default` or left empty, then you'll need to add the following to `~/Web.config`
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
You also need to add the following to `~/Media/Web.config`
```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <clear />
      <add name="StaticFileHandler" path="*" verb="*" preCondition="integratedMode" type="System.Web.StaticFileHandler" />
      <add name="StaticFile" path="*" verb="*" modules="StaticFileModule,DefaultDocumentModule,DirectoryListingModule" resourceType="Either" requireAccess="Read" />
    </handlers>
  </system.webServer>
</configuration>
```


## AWS Authentication

Ok so where are the [IAM access keys?](http://docs.aws.amazon.com/IAM/latest/UserGuide/ManagingCredentials.html) Depending on how you host your project they already exist if deploying inside an EC2 instance via environment variables specified during deployment and creation of infrastructure.
It's also a good idea to use [AWS best security practices](http://docs.aws.amazon.com/general/latest/gr/aws-access-keys-best-practices.html). Like not using your root access account, use short lived access keys and don't EVER commit them to source control.

If you aren't using EC2/ElasticBeanstalk to access generated temporary keys, you can put them into `~/Web.config`
```xml
<?xml version="1.0"?>
<configuration>
  <appSettings>
    <add key="AWSAccessKey" value="" />
    <add key="AWSSecretKey" value="" />
  </appSettings>
</configuration>
```


## Should I use the Virtual Path Provider?
Using a custom [Virtual Path Provider](https://msdn.microsoft.com/en-us/library/system.web.hosting.virtualpathprovider%28v=vs.110%29.aspx) (the default configuration) means your files are routed transparently through your domain (e.g. `https://example.com/media`). Anyone visiting your site won't be able to tell your files are stored on S3.

Turning the VPP functionality off will store the full S3 URL for each media item, and this will be visible to anyone visiting your site.

Before making a decision either way you might want to read how Virtual Path Providers affect performance/caching, as it differs from IIS's [unmanaged handler](http://www.paraesthesia.com/archive/2011/05/02/when-staticfilehandler-is-not-staticfilehandler.aspx/).


## Using ImageProcessor
Support for remote files has been added to ImageProcessor in version > `2.3.2`. You'll also want to ensure that you are using Virtual Path Provider as ImageProcessor only hijacks requests when parameters are present in the querystring (like width, height, etc).

```powershell
Install-Package ImageProcessor.Web.Config
```

Replace config file located `~/config/imageprocessor/security.config`
```xml
<?xml version="1.0" encoding="utf-8"?>
<security>
  <services>
    <service prefix="media/" name="CloudImageService" type="ImageProcessor.Web.Services.CloudImageService, ImageProcessor.Web">
      <settings>
        <setting key="MaxBytes" value="8194304"/>
        <setting key="Timeout" value="30000"/>
        <setting key="Host" value="http://{Your Unique Bucket Name}.s3.amazonaws.com/{Your Key Prefix}/"/>
      </settings>
    </service>
  </services>
</security>
```
