using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Serilog;
using Serilog.Events;
using System.IO;

namespace RSG
{
    /// <summary>
    /// Singleton application class. Used AppInit to bootstrap in a Unity scene.
    /// </summary>
    public interface IApp
    {
        /// <summary>
        /// Get the factory instance.
        /// </summary>
        IFactory Factory { get; }

        /// <summary>
        /// Global logger.
        /// </summary>
        RSG.Utils.ILogger Logger { get; }
    }

    /// <summary>
    /// Singleton application class. Used AppInit to bootstrap in a Unity scene.
    /// </summary>
    public class App : IApp
    {
        /// <summary>
        /// Instance ID for the application. Used to differentuate logs from different runs of the app.
        /// </summary>
        public static readonly string AppInstanceID = Guid.NewGuid().ToString();

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
        /// Name of the game object automatically added to the scene that handles events on behalf of the app.
        /// </summary>
        public static readonly string AppHubObjectName = "_AppHub";

        /// <summary>
        /// Accessor for the singleton app instance.
        /// </summary>
        public static IApp Instance { get; private set; }

        /// <summary>
        /// The reports directory for this application instance.
        /// </summary>
        public string LogsDirectoryPath
        {
            get
            {
                return Path.Combine(Path.Combine(Application.persistentDataPath, LogsDirectoryName), AppInstanceID);
            }
        }

        /// <summary>
        /// Initialize the app. Can be called multiple times.
        /// </summary>
        public static void Init()
        {
            if (Instance != null)
            {
                // Already initialised.
                return;
            }

            Instance = new App();
        }

        /// <summary>
        /// Resolve dependencies on a specific object, first ensuring that the application has been initialized.
        /// </summary>
        public static void ResolveDependencies(object obj)
        {
            Argument.NotNull(() => obj);

            Init();

            Instance.Factory.ResolveDependencies(obj);
        }

        public App()
        {
            CreateLogsDirectory();

            var loggerConfig = new Serilog.LoggerConfiguration()
                .WriteTo.Trace()
                .Enrich.With<RSGLogEnricher>();

            if (logsDirectoryStatus == LogsDirectoryStatus.Created)
            {
                loggerConfig.WriteTo.File(Path.Combine(LogsDirectoryPath, "Errors.log"), LogEventLevel.Error);
                loggerConfig.WriteTo.File(Path.Combine(LogsDirectoryPath, "Info.log"), LogEventLevel.Information);
                loggerConfig.WriteTo.File(Path.Combine(LogsDirectoryPath, "Verbose.log"), LogEventLevel.Verbose);
            }

            var reflection = new Reflection();
            foreach (var sinkType in reflection.FindTypesMarkedByAttributes(LinqExts.FromItems(typeof(SerilogSinkAttribute))))
            {
                loggerConfig.WriteTo.Sink((Serilog.Core.ILogEventSink)sinkType.GetConstructor(new Type[0]).Invoke(new object[0]));
            }

            var logger = new SerilogLogger(loggerConfig.CreateLogger());
            logger.LogInfo("Application started at {TimeNow}", DateTime.Now);
            logger.LogInfo("Logs directory status: {LogsDirectoryStatus}", logsDirectoryStatus);
            if (logsDirectoryStatus == LogsDirectoryStatus.Failed)
            {
                logger.LogError(logsDirectoryCreateException, "Failed to create logs directory {LogsDirectoryPath}", LogsDirectoryPath);                
            }
            else
            {
                logger.LogInfo("Writing logs and reports to {LogsDirectoryPath}", LogsDirectoryPath);
            }

            LogSystemInfo(logger);

            var factory = new Factory("App", logger, reflection);
            factory.Dep<RSG.Utils.ILogger>(logger);

            var singletonManager = InitFactory(logger, factory, reflection);

            this.Factory = factory;
            this.Logger = logger;

            singletonManager.InstantiateSingletons(factory);
            singletonManager.Startup();

            var appHub = InitAppHub();
            appHub.Shutdown = () => singletonManager.Shutdown();

            // Initialize errors for unhandled promises.
            Promise.UnhandledException += (s, e) => logger.LogError(e.Exception, "Unhandled error from promise.");

            Application.RegisterLogCallbackThreaded((msg, stackTrace, type) =>
            {
                if (!msg.StartsWith(SerilogUnitySink.RSGLogTag))
                {
                    switch (type)
                    {
                        case LogType.Assert:        
                        case LogType.Error:
                        case LogType.Exception:     logger.LogError(msg + "\r\nStack:\r\n{StackTrace}", stackTrace); break;
                        case LogType.Warning:       logger.LogWarning(msg + "\r\nStack:\r\n{StackTrace}", stackTrace); break;
                        default:                    logger.LogInfo(msg + "\r\nStack:\r\n{StackTrace}", stackTrace); break;
                    }
                }
                                    
            });
        }

        /// <summary>
        /// Dump out system info.
        /// </summary>
        private void LogSystemInfo(RSG.Utils.ILogger logger)
        {
            var systemReportsPath = Path.Combine(LogsDirectoryPath, SystemReportsPath);
            var logSystemInfo = new LogSystemInfo(logger, systemReportsPath);
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

                var appLogsPath = Path.Combine(logsPath, AppInstanceID);
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
        /// Helper function to initalize the factory.
        /// </summary>
        private static SingletonManager InitFactory(RSG.Utils.ILogger logger, Factory factory, IReflection reflection)
        {           
            //todo: all this code should merge into RSG.Factory.
            factory.AutoRegisterTypes();

            var singletonManager = new SingletonManager(reflection, logger, factory);

            factory.Dep<IReflection>(reflection);
            factory.AddDependencyProvider(singletonManager);

            var singletonScanner = new SingletonScanner(reflection, logger, singletonManager);
            singletonScanner.ScanSingletonTypes();
            return singletonManager;
        }

        /// <summary>
        /// Helper function to initalize the app hub.
        /// </summary>
        private static AppHub InitAppHub()
        {
            var appHubGO = GameObject.Find(AppHubObjectName);
            if (appHubGO == null)
            {
                appHubGO = new GameObject(AppHubObjectName);
                GameObject.DontDestroyOnLoad(appHubGO);
            }

            var appHub = appHubGO.GetComponent<AppHub>();
            if (appHub == null)
            {
                appHub = appHubGO.AddComponent<AppHub>();
            }
            return appHub;
        }

        /// <summary>
        /// Get the factory instance.
        /// </summary>
        public IFactory Factory
        {
            get;
            private set;
        }

        /// <summary>
        /// Global logger.
        /// </summary>
        public RSG.Utils.ILogger Logger
        {
            get;
            private set;
        }
    }
}
