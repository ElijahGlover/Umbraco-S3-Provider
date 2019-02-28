using System;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;

namespace Umbraco.Storage.S3.Services.Impl
{
    public class LogHelperWrapper : ILogHelper
    {
        public void Info<T>(string generateMessageFormat, params Func<object>[] formatItems)
        {
            Current.Logger.Info<T>(generateMessageFormat, formatItems);
        }

        public void Error<T>(string message, Exception exception)
        {
            Current.Logger.Info<T>(message, exception);
        }
    }
}
