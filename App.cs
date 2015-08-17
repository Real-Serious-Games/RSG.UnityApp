using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Serilog;
using Serilog.Events;
using System.IO;
using RSG.Scene.Query;
using Newtonsoft.Json;

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

        /// <summary>
        /// The name of the device assigned by the application.
        /// </summary>
        void SetDeviceName(string newDeviceName);

        /// <summary>
        /// The logs directory for this application instance.
        /// </summary>
        string LogsDirectoryPath { get; }
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
        /// Name of the game object automatically added to the scene that handles events on behalf of the app.
        /// </summary>
        public static readonly string AppHubObjectName = "_AppHub";

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
        /// Interface that can be implemented by user's of the library to pass in settings to RSG.UnityApp.
        /// </summary>
        private IAppConfigurator appConfigurator;

        /// <summary>
        /// Accessor for the singleton app instance.
        /// </summary>
        public static IApp Instance { get; private set; }

        /// <summary>
        /// The logs directory for this application instance.
        /// </summary>
        public string LogsDirectoryPath
        {
            get
            {
                return Path.Combine(GlobalLogsDirectoryPath, AppInstanceID);
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
        /// The path to where persistant device info is stored.
        /// </summary>
        private string DeviceInfoFilePath
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, "DeviceInfo.json");
            }
        }

        /// <summary>
        /// Path where the 'running file' is saved. This is a file that is present when the app is running and
        /// allows unclean shutdown to be detected.
        /// </summary>
        private string AppRunningFilePath
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, "AppRunning.json");
            }
        }

        /// <summary>
        /// Unique identifier that for this device.
        /// This is generated on first run and then persisted from instance-to-instance of the app.
        /// </summary>
        public static Guid DeviceID { get; private set; }

        /// <summary>
        /// A name for the device. This name can be assigned by the app, although it defaults to the device ID.
        /// </summary>
        public static string DeviceName { get; private set; }

        /// <summary>
        /// Initialize the app. Can be called multiple times.
        /// </summary>
        public static void Init(IAppConfigurator appConfigurator)
        {
            Argument.NotNull(() => appConfigurator);

            if (Instance != null)
            {
                // Already initialised.
                return;
            }

            Instance = new App(appConfigurator);
        }

        /// <summary>
        /// Resolve dependencies on a specific object, first ensuring that the application has been initialized.
        /// </summary>
        public static void ResolveDependencies(object obj, IAppConfigurator appConfigurator)
        {
            Argument.NotNull(() => obj);
            Argument.NotNull(() => appConfigurator);

            Init(appConfigurator);

            Instance.Factory.ResolveDependencies(obj);
        }

        public App(IAppConfigurator appConfigurator)
        {
            Argument.NotNull(() => appConfigurator);

            this.appConfigurator = appConfigurator;

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
            
            InitDeviceId();

            var logger = new SerilogLogger(loggerConfig.CreateLogger());
            this.Logger = logger;
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

            LogSystemInfo(logger, appConfigurator);

            InitRunningFile();

            DeleteOldLogFiles();

            var factory = new Factory("App", logger, reflection);
            factory.Dep<RSG.Utils.ILogger>(logger);
            var dispatcher = new Dispatcher(logger);
            factory.Dep<IDispatcher>(dispatcher);
            factory.Dep<IDispatchQueue>(dispatcher);            
            factory.Dep<ISceneQuery>(new SceneQuery());
            factory.Dep<ISceneTraversal>(new SceneTraversal());

            var singletonManager = InitFactory(logger, factory, reflection);

            this.Factory = factory;

            singletonManager.InstantiateSingletons(factory);
            singletonManager.Startup();

            var appHub = InitAppHub();
            appHub.Shutdown = 
                () =>
                {
                    singletonManager.Shutdown();

                    DeleteRunningFile();
                };

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
        /// Used to serialize details of the running app.
        /// </summary>
        private class AppRunning
        {
            /// <summary>
            /// The instance of the app that is running.
            /// </summary>
            public string AppInstance;

            /// <summary>
            /// The date the app started running at.
            /// </summary>
            public DateTime StartedAt;
        }


        /// <summary>
        /// Initialise a file that is present while the app is running.
        /// We use this to detect an unclean shutodwn on the next app instance.
        /// </summary>
        private void InitRunningFile()
        {
            if (File.Exists(AppRunningFilePath))
            {
                try
                {
                    var previousAppRunning = JsonConvert.DeserializeObject<AppRunning>(File.ReadAllText(AppRunningFilePath));

                    Logger.LogError("Unclean shutdown detected from previous application instance {PrevAppInstanceID} which started at {AppStartDate}", previousAppRunning.AppInstance, previousAppRunning.StartedAt);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unclean shutdown detected from previous application instance, was unable to read 'running file' from {AppRunningFilePath}", AppRunningFilePath);
                }
            }

            try
            {
                File.WriteAllText(AppRunningFilePath,
                    JsonConvert.SerializeObject(
                        new AppRunning()
                        {
                            AppInstance = App.AppInstanceID,
                            StartedAt = DateTime.Now
                        }
                    )
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save 'running file'.");
            }

        }

        /// <summary>
        /// Delete the file that is present while the app is running.
        /// This only happens on clean shutdown.
        /// </summary>
        private void DeleteRunningFile()
        {
            try
            {
                File.Delete(AppRunningFilePath);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to delete 'running file' at {AppRunningFilePath}", AppRunningFilePath);
            }
        }

        /// <summary>
        /// Used for serializing persistent app info.
        /// </summary>
        private class DeviceInfo
        {
            /// <summary>
            /// Unique ID for the device.
            /// </summary>
            public Guid DeviceID;

            /// <summary>
            /// A name that can be assigned to the device (defaults to the ID).
            /// </summary>
            public string DeviceName;
        }

        /// <summary>
        /// Initialise an ID for the device the app is running. 
        /// This allows each device to be uniquely identified.
        /// </summary>
        private void InitDeviceId()
        {
            if (LoadDeviceId())
            {
                // Loaded previously saved device id.
                return;
            }

            // No device info was loaded.
            // Create a new device ID.
            var deviceID = Guid.NewGuid();
            App.DeviceID = deviceID;
            App.DeviceName = string.Empty;

            Debug.Log("Allocated device id " + deviceID);

            SaveDeviceInfoFile();
        }

        /// <summary>
        /// The name of the device assigned by the application.
        /// </summary>
        public void SetDeviceName(string newDeviceName)
        {
            Argument.StringNotNullOrEmpty(() => newDeviceName);

            App.DeviceName = newDeviceName;

            SaveDeviceInfoFile();
        }

        /// <summary>
        /// Save the device info file to persistant data.
        /// </summary>
        private void SaveDeviceInfoFile()
        {
            try
            {
                // Serialize device ID, etc, to be remembered at next app instance.
                File.WriteAllText(DeviceInfoFilePath,
                    JsonConvert.SerializeObject(
                        new DeviceInfo()
                        {
                            DeviceID = App.DeviceID,
                            DeviceName = App.DeviceName
                        }
                    )
                );
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to save DeviceInfo file: " + DeviceInfoFilePath);
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Load device ID from the device info file.
        /// Returns false if the file doesn't exist or failes to load
        /// </summary>
        private bool LoadDeviceId()
        {
            //
            // Load device ID.
            //
            if (!File.Exists(DeviceInfoFilePath))
            {
                return false;
            }

            Debug.Log("Loading device info file: " + DeviceInfoFilePath);

            try
            {
                var deviceInfo = JsonConvert.DeserializeObject<DeviceInfo>(File.ReadAllText(DeviceInfoFilePath));
                App.DeviceID = deviceInfo.DeviceID;
                App.DeviceName = deviceInfo.DeviceName;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to load DeviceInfo file: " + DeviceInfoFilePath);
                Debug.LogException(ex);

                return false;
            }
        }

        /// <summary>
        /// Dump out system info.
        /// </summary>
        private void LogSystemInfo(RSG.Utils.ILogger logger, IAppConfigurator appConfigurator)
        {
            var systemReportsPath = Path.Combine(LogsDirectoryPath, SystemReportsPath);
            var logSystemInfo = new LogSystemInfo(logger, systemReportsPath);
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
                Logger.LogError(ex, "Error deleting old log files.");
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
