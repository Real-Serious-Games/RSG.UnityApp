using RSG.Utils;
using Serilog;
using Serilog.Events;
using System;

namespace RSG
{
    /// <summary>
    /// Logger that writes to its own log file and pipes errors through to the main log.
    /// </summary>
    internal class FactoryLogger : Utils.ILogger
    {
        private readonly Serilog.ILogger factoryLogger;

        private readonly Utils.ILogger mainLogger;

        public FactoryLogger(Utils.ILogger mainLogger, string logFilePath)
        {
            Argument.NotNull(() => mainLogger);

            this.mainLogger = mainLogger;

            var loggerConfig = new LoggerConfiguration();
            if (!string.IsNullOrEmpty(logFilePath))
            {
                loggerConfig.WriteTo.File(logFilePath, LogEventLevel.Verbose);
            }
            factoryLogger = loggerConfig.CreateLogger();
        }

        /// <summary>
        /// Log an error.
        /// </summary>
        public void LogError(string message, params object[] args)
        {
            factoryLogger.Error(message, args);
            mainLogger.LogError(message, args);
        }

        /// <summary>
        /// Log an error.
        /// </summary>
        public void LogError(Exception ex, string message, params object[] args)
        {
            factoryLogger.Error(ex, message, args);
            mainLogger.LogError(ex, message, args);
        }

        /// <summary>
        /// Log an info message.
        /// </summary>
        public void LogInfo(string message, params object[] args)
        {
            factoryLogger.Information(message, args);
            mainLogger.LogVerbose(message, args);
        }

        /// <summary>
        /// Log a verbose message.
        /// </summary>
        public void LogVerbose(string message, params object[] args)
        {
            factoryLogger.Verbose(message, args);
            mainLogger.LogVerbose(message, args);
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public void LogWarning(string message, params object[] args)
        {
            factoryLogger.Warning(message, args);
            mainLogger.LogWarning(message, args);
        }
    }
}
