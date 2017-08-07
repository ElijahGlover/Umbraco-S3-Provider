using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace Umbraco.Storage.S3
{
    internal class FileSystemVirtualPathProviderFile : VirtualFile
    {
        private readonly Func<Stream> _stream;
        private readonly string[] _extensionArray;

        public FileSystemVirtualPathProviderFile(string virtualPath, Func<Stream> stream) : base(virtualPath)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            _stream = stream;

            string validExtensions = ConfigurationSettings.AppSettings["Umbraco.S3.Stream.Extensions"];
            _extensionArray = validExtensions?.Split(',');
        }

        public override Stream Open()
        {
            if (HttpContext.Current != null)
            {
                var path = HttpContext.Current.Request.FilePath;
                if (_extensionArray.Any(x => path.Contains(x)))
                {
                    HttpContext.Current.Response.AppendHeader("Accept-Ranges", "bytes");
                }
            }

            return _stream();
        }


        public override bool IsDirectory
        {
            get { return false; }
        }
    }
}

