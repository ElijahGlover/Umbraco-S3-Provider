using System;
using System.IO;

namespace Umbraco.Storage.S3.Services
{
    public interface IFileSystemWrapper
    {
        void EnsureDirectory(string path);
        Stream Open(string filePath);
        void Create(string filePath, Stream stream);
        DateTime GetLastAccessTimeUtc(string filePath);
        string MapPath(string path);
    }
}
