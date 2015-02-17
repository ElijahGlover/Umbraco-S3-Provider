using System;
using System.Web.Hosting;
using Umbraco.Core.IO;

namespace Umbraco.Storage.S3
{
    public class FileSystemVirtualPathProvider : VirtualPathProvider
    {
        private readonly string _pathPrefix;
        private readonly Lazy<IFileSystem> _fileSystem; 

        public FileSystemVirtualPathProvider(string pathPrefix, Lazy<IFileSystem> fileSystem)
        {
            if (string.IsNullOrEmpty(pathPrefix))
                throw new ArgumentNullException("pathPrefix");
            if (fileSystem == null)
                throw new ArgumentNullException("fileSystem");

            _pathPrefix = pathPrefix.StartsWith("~/")
                ? pathPrefix
                : string.Concat("~/", pathPrefix);
            _fileSystem = fileSystem;
        }

        public override bool FileExists(string virtualPath)
        {
            if (!virtualPath.StartsWith(_pathPrefix, StringComparison.InvariantCultureIgnoreCase))
                return base.FileExists(virtualPath);

            return _fileSystem.Value.FileExists(StripPathPrefix(virtualPath));
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            if (!virtualPath.StartsWith(_pathPrefix, StringComparison.InvariantCultureIgnoreCase))
                return base.GetFile(virtualPath);

            return new FileSystemVirtualPathProviderFile(virtualPath, () => _fileSystem.Value.OpenFile(StripPathPrefix(virtualPath)));
        }

        private string StripPathPrefix(string virtualPath)
        {
            return virtualPath.Substring(_pathPrefix.Length);
        }

        public string PathPrefix
        {
            get { return _pathPrefix; }
        }

        public static void Configure<TProviderTypeFilter>(string pathPrefix) where TProviderTypeFilter : FileSystemWrapper
        {
            var fileSystem = new Lazy<IFileSystem>(() => FileSystemProviderManager.Current.GetFileSystemProvider<TProviderTypeFilter>());
            var provider = new FileSystemVirtualPathProvider(pathPrefix, fileSystem);
            HostingEnvironment.RegisterVirtualPathProvider(provider);
        }

        public static void ConfigureMedia(string pathPrefix = "~/media")
        {
            Configure<MediaFileSystem>(pathPrefix);
        }
    }
}
