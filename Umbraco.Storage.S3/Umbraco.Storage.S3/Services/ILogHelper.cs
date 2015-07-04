using System;

namespace Umbraco.Storage.S3.Services
{
    public interface ILogHelper
    {
        void Info<T>(string generateMessageFormat, params Func<object>[] formatItems);
        void Info(Type type, string generateMessageFormat, params Func<object>[] formatItems);
        void Info(Type callingType, Func<string> generateMessage);
        void Info<T>(Func<string> generateMessage);
        void Error<T>(string message, System.Exception exception);
        void Error(Type callingType, string message, System.Exception exception);
    }
}
