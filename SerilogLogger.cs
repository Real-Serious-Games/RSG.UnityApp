using RSG.Utils;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RSG
{
    public class SerilogLogger : RSG.Utils.ILogger
    {
        /// <summary>
        /// Serilog logger.
        /// </summary>
        private readonly Serilog.ILogger serilog;

        /// <summary>
        /// Enables verbose logging.
        /// </summary>
        public bool EnableVerbose { get; set; }

        public SerilogLogger(LogConfig logConfig, IReflection reflection)
        {
            Argument.NotNull(() => logConfig);
            Argument.NotNull(() => reflection);

            this.EnableVerbose = logConfig.Verbose;

            CreateLogsDirectory();

            var loggerConfig = new Serilog.LoggerConfiguration();

            if (this.EnableVerbose)
            {
                loggerConfig = loggerConfig.MinimumLevel.Verbose();
            }

            loggerConfig = loggerConfig.WriteTo.Trace();

            var emptyTypeArray = new Type[0];
            var emptyObjectArray = new object[0];

            var logEnrichers = reflection.FindTypesMarkedByAttributes(LinqExts.FromItems(typeof(LogEnricherAttribute)));
            loggerConfig = logEnrichers
                .Select(logEnricherType => logEnricherType.GetConstructor(emptyTypeArray).Invoke(emptyObjectArray))
                .Cast<Serilog.Core.ILogEventEnricher>()
                .Aggregate(loggerConfig, (prevLoggerConfig, logEnricher) => prevLoggerConfig.Enrich.With(logEnricher));

            if (logsDirectoryStatus == LogsDirectoryStatus.Created)
            {
                loggerConfig.WriteTo.File(Path.Combine(LogsDirectoryPath, "Errors.log"), LogEventLevel.Error);
                loggerConfig.WriteTo.File(Path.Combine(LogsDirectoryPath, "Info.log"), LogEventLevel.Information);
                loggerConfig.WriteTo.File(Path.Combine(LogsDirectoryPath, "Verbose.log"), LogEventLevel.Verbose);
            }

            if (!string.IsNullOrEmpty(logConfig.LogPostUrl))
            {
                Debug.Log("Sending log messages via HTTP to " + logConfig.LogPostUrl);

                loggerConfig.WriteTo.Sink(new SerilogHttpSink(logConfig.LogPostUrl));
            }
            else
            {
                Debug.Log("Not sending log messages via HTTP");
            }

            foreach (var sinkType in reflection.FindTypesMarkedByAttributes(LinqExts.FromItems(typeof(SerilogSinkAttribute))))
            {
                loggerConfig.WriteTo.Sink((Serilog.Core.ILogEventSink)sinkType.GetConstructor(emptyTypeArray).Invoke(emptyObjectArray));
            }

            this.serilog = loggerConfig.CreateLogger();

            LogInfo("Application started at {TimeNow}", DateTime.Now);
            LogInfo("Logs directory status: {LogsDirectoryStatus}", logsDirectoryStatus);
            if (logsDirectoryStatus == LogsDirectoryStatus.Failed)
            {
                LogError(logsDirectoryCreateException, "Failed to create logs directory {LogsDirectoryPath}", LogsDirectoryPath);
            }
            else
            {
                LogInfo("Writing logs and reports to {LogsDirectoryPath}", LogsDirectoryPath);
            }

            if (this.EnableVerbose)
            {
                LogInfo("Verbose logging is enabled.");
            }
            else
            {
                LogInfo("Verbose logging is not enabled.");
            }

            LogSystemInfo();

            DeleteOldLogFiles();

            // Initialize errors for unhandled promises.
            Promise.UnhandledException += (s, e) => LogError(e.Exception, "Unhandled error from promise.");

            Application.RegisterLogCallbackThreaded((msg, stackTrace, type) =>
            {
                if (!msg.StartsWith(SerilogUnitySink.RSGLogTag))
                {
                    switch (type)
                    {
                        case LogType.Assert:
                        case LogType.Error:
                        case LogType.Exception: LogError(msg + "\r\nStack:\r\n{StackTrace}", stackTrace); break;
                        case LogType.Warning: LogWarning(msg + "\r\nStack:\r\n{StackTrace}", stackTrace); break;
                        default: LogInfo(msg + "\r\nStack:\r\n{StackTrace}", stackTrace); break;
                    }
                }
            });
        }

        /// <summary>
        /// Log an error.
        /// </summary>
        public void LogError(string message, params object[] args)
        {
            serilog.Error(message, args);
        }

        /// <summary>
        /// Log an error.
        /// </summary>
        public void LogError(Exception ex, string message, params object[] args)
        {
            serilog.Error(ex, message, args);

            LogExceptions(ex);
        }

        /// <summary>
        /// Helper to log inner exceptions.
        /// </summary>
        private void LogExceptions(Exception ex)
        {
            while (ex != null)
            {
                var formattedException = ex as FormattedException;
                if (formattedException != null)
                {
                    serilog.Warning(ex, "Exception ocurred\r\n" + formattedException.MessageTemplate, formattedException.PropertyValues);
                }
                else
                {
                    serilog.Warning(ex,
                        "Exception ocurred: {ExceptionMessage}\r\n" +
                        "Details:\r\n" +
                        "{@Exception}",
                        ex.Message, ex
                    );
                }

                ex = ex.InnerException;
            }            
        }

        /// <summary>
        /// Log an info message.
        /// </summary>
        public void LogInfo(string message, params object[] args)
        {
            serilog.Information(message, args);
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public void LogWarning(string message, params object[] args)
        {
            serilog.Warning(message, args);
        }

        /// <summary>
        /// Log a verbose message, by default these aren't output.
        /// </summary>
        public void LogVerbose(string message, params object[] args)
        {
            serilog.Verbose(message, args);
        }

        /// <summary>
        /// Records the status of the logs directory.
        /// </summary>
        private enum LogsDirectoryStatus
        {
            Unknown,
            Created,
            Failed,
        }

        /// <summary>
        /// Records the status of the logs directory.
        /// </summary>
        private LogsDirectoryStatus logsDirectoryStatus = LogsDirectoryStatus.Unknown;

        /// <summary>
        /// Records an exception, if any thrown, during creation of logs directory.
        /// </summary>
        private Exception logsDirectoryCreateException;

        /// <summary>
        /// Location to save reports to.
        /// </summary>
        private static readonly string LogsDirectoryName = "Logs";

        /// <summary>
        /// Location to save system reports to.
        /// </summary>
        private static readonly string SystemReportsPath = "System";

        /// <summary>
        /// The logs directory for this application instance.
        /// </summary>
        public string LogsDirectoryPath
        {
            get
            {
                return Path.Combine(GlobalLogsDirectoryPath, App.AppInstanceID);
            }
        }

        /// <summary>
        /// Directory where subdirectories for application instance log files are stored.
        /// </summary>
        private string GlobalLogsDirectoryPath
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, LogsDirectoryName);
            }
        }

        /// <summary>
        /// Dump out system info.
        /// </summary>
        private void LogSystemInfo()
        {
            var systemReportsPath = Path.Combine(LogsDirectoryPath, SystemReportsPath);
            var logSystemInfo = new LogSystemInfo(this, systemReportsPath);
            logSystemInfo.Output();
        }

        /// <summary>
        /// Create the application's logs directory.
        /// </summary>
        private void CreateLogsDirectory()
        {
            try
            {
                var logsPath = Path.Combine(Application.persistentDataPath, LogsDirectoryName);
                if (!Directory.Exists(logsPath))
                {
                    Directory.CreateDirectory(logsPath);
                }

                var appLogsPath = Path.Combine(logsPath, App.AppInstanceID);
                if (!Directory.Exists(appLogsPath))
                {
                    Directory.CreateDirectory(appLogsPath);
                }

                logsDirectoryStatus = LogsDirectoryStatus.Created;
            }
            catch (Exception ex)
            {
                logsDirectoryStatus = LogsDirectoryStatus.Failed;
                logsDirectoryCreateException = ex;
            }
        }

        /// <summary>
        /// Removes log files more than a month old.
        /// </summary>
        private void DeleteOldLogFiles()
        {
            const int maxAgeDays = 30;

            try
            {
                Directory
                    .GetDirectories(GlobalLogsDirectoryPath)
                    .Where(directory => Directory.GetLastWriteTime(directory) <= DateTime.Now.AddDays(-maxAgeDays))
                    .Each(directory => Directory.Delete(directory, true));
            }
            catch (Exception ex)
            {
                LogError(ex, "Error deleting old log files.");
            }
        }
    }
}
