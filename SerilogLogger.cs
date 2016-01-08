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
    [Singleton(typeof(RSG.Utils.ILogger))]
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

        public SerilogLogger(IAppConfigurator appConfigurator)
        {
            Argument.NotNull(() => appConfigurator);

            CreateLogsDirectory();

            var loggerConfig = new Serilog.LoggerConfiguration()
                .WriteTo.Trace()
                .Enrich.With(new RSGLogEnricher(appConfigurator));

            appConfigurator.ConfigureLog(loggerConfig);

            if (logsDirectoryStatus == LogsDirectoryStatus.Created)
            {
                loggerConfig.WriteTo.File(Path.Combine(LogsDirectoryPath, "Errors.log"), LogEventLevel.Error);
                loggerConfig.WriteTo.File(Path.Combine(LogsDirectoryPath, "Info.log"), LogEventLevel.Information);
                loggerConfig.WriteTo.File(Path.Combine(LogsDirectoryPath, "Verbose.log"), LogEventLevel.Verbose);
            }

            if (!string.IsNullOrEmpty(appConfigurator.LogPostUrl))
            {
                Debug.Log("Sending log messages via HTTP to " + appConfigurator.LogPostUrl);

                loggerConfig.WriteTo.Sink(new SerilogHttpSink(appConfigurator.LogPostUrl));
            }
            else
            {
                Debug.Log("Not sending log messages via HTTP");
            }

            var reflection = new Reflection();
            foreach (var sinkType in reflection.FindTypesMarkedByAttributes(LinqExts.FromItems(typeof(SerilogSinkAttribute))))
            {
                loggerConfig.WriteTo.Sink((Serilog.Core.ILogEventSink)sinkType.GetConstructor(new Type[0]).Invoke(new object[0]));
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

            LogSystemInfo(appConfigurator);

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
        private void LogSystemInfo(IAppConfigurator appConfigurator)
        {
            var systemReportsPath = Path.Combine(LogsDirectoryPath, SystemReportsPath);
            var logSystemInfo = new LogSystemInfo(this, systemReportsPath);
            logSystemInfo.Output(appConfigurator);
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
