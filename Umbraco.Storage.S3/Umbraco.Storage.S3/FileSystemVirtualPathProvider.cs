using System;
using System.Web.Hosting;
using Umbraco.Core.Composing;
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

            _pathPrefix = FormatVirtualPathPrefix(pathPrefix);
            _fileSystem = fileSystem;
        }

        private string FormatVirtualPathPrefix(string pathPrefix)
        {
            pathPrefix = pathPrefix.Replace("\\", "/");
            pathPrefix = pathPrefix.StartsWith("/")
                ? pathPrefix
                : string.Concat("/", pathPrefix);
            pathPrefix = pathPrefix.EndsWith("/")
                ? pathPrefix
                : string.Concat(pathPrefix, "/");
            return pathPrefix;
        }

        public override bool FileExists(string virtualPath)
        {
            var path = FormatVirtualPath(virtualPath);
            if (!path.StartsWith(_pathPrefix, StringComparison.InvariantCultureIgnoreCase))
                return base.FileExists(virtualPath);

            var fileSystemPath = RemovePathPrefix(path);
            return _fileSystem.Value.FileExists(fileSystemPath);
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            var path = FormatVirtualPath(virtualPath);
            if (!path.StartsWith(_pathPrefix, StringComparison.InvariantCultureIgnoreCase))
                return base.GetFile(virtualPath);

            var fileSystemPath = RemovePathPrefix(path);
            return new FileSystemVirtualPathProviderFile(virtualPath, () => _fileSystem.Value.OpenFile(fileSystemPath));
        }

        private string RemovePathPrefix(string virtualPath)
        {
            return virtualPath.Substring(_pathPrefix.Length);
        }

        private string FormatVirtualPath(string virtualPath)
        {
            return virtualPath.StartsWith("~")
                ? virtualPath.Substring(1)
                : virtualPath;
        }

        public string PathPrefix
        {
            get { return _pathPrefix; }
        }

        public static void Configure<TProviderTypeFilter>(string pathPrefix = "media") where TProviderTypeFilter : FileSystemWrapper
        {
            if (string.IsNullOrEmpty(pathPrefix))
                throw new ArgumentNullException("pathPrefix");

            var fileSystem = new Lazy<IFileSystem>(() => Current.MediaFileSystem.Unwrap());
            var provider = new FileSystemVirtualPathProvider(pathPrefix, fileSystem);
            HostingEnvironment.RegisterVirtualPathProvider(provider);
        }

        public static void ConfigureMedia(string pathPrefix = "media")
        {
            Configure<MediaFileSystem>(pathPrefix);
        }
    }
}
