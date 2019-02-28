using System;

namespace Umbraco.Storage.S3.Services
{
    public interface ILogHelper
    {
        void Info<T>(string generateMessageFormat, params Func<object>[] formatItems);
        void Error<T>(string message, Exception exception);
    }
}
