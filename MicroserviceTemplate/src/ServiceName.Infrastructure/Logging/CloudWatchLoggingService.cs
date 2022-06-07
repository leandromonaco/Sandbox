﻿using AWS.Logger;
using AWS.Logger.SeriLog;
using Serilog;
using ServiceName.Core.Common.Interfaces;

namespace ServiceName.Infrastructure.Logging
{
    internal class CloudWatchLoggingService : ILoggingService
    {
        /// <summary>
        /// https://docs.aws.amazon.com/lambda/latest/dg/csharp-logging.html (LambdaLogger)
        /// https://github.com/aws/aws-logging-dotnet
        /// https://github.com/aws/aws-logging-dotnet/tree/master/samples/Serilog
        /// </summary>
        public CloudWatchLoggingService()
        {
            Log.Logger = new LoggerConfiguration().WriteTo.AWSSeriLog()
                                                  .WriteTo.Console()
                                                  .CreateLogger();
        }

        public async Task LogErrorAsync(string message, params object[] propertyValues)
        {
            Log.Error(message, propertyValues);
        }

        public async Task LogInformationAsync(string message, params object[] propertyValues)
        {
            Log.Information(message, propertyValues);
        }

        public async Task LogWarningAsync(string message, params object[] propertyValues)
        {
            Log.Warning(message, propertyValues);
        }
    }
}
