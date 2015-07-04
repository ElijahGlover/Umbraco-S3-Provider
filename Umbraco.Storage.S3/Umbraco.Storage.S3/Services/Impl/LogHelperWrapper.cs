using System;
using Umbraco.Core.Logging;

namespace Umbraco.Storage.S3.Services.Impl
{
    public class LogHelperWrapper : ILogHelper
    {
        public void Info<T>(string generateMessageFormat, params Func<object>[] formatItems)
        {
            LogHelper.Info<T>(generateMessageFormat, formatItems);
        }

        public void Info(Type type, string generateMessageFormat, params Func<object>[] formatItems)
        {
            LogHelper.Info(type, generateMessageFormat, formatItems);
        }

        public void Info(Type callingType, Func<string> generateMessage)
        {
            LogHelper.Info(callingType, generateMessage);
        }

        public void Info<T>(Func<string> generateMessage)
        {
            LogHelper.Info<T>(generateMessage);
        }

        public void Error<T>(string message, Exception exception)
        {
            LogHelper.Error<T>(message, exception);
        }

        public void Error(Type callingType, string message, Exception exception)
        {
            LogHelper.Error(callingType, message, exception);
        }
    }
}
