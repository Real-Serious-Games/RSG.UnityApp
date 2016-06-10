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
using RSG.UnityApp.Internal;

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
        /// Used to schedule code onto the main thread.
        /// </summary>
        IDispatcher Dispatcher { get; }

        /// <summary>
        /// Get the global promise timer.
        /// </summary>
        IPromiseTimer PromiseTimer { get; }

        /// <summary>
        /// Global singleton manager.
        /// </summary>
        ISingletonManager SingletonManager { get; }

        /// <summary>
        /// The name of the device assigned by the application.
        /// </summary>
        void SetDeviceName(string newDeviceName);
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
        /// Accessor for the singleton app instance.
        /// </summary>
        public static IApp Instance { get; private set; }

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
        /// The path to where log configuration is stored.
        /// </summary>
        private string LogConfigFilePath
        {
            get
            {
#if UNITY_ANDROID
                return Path.Combine(Application.persistentDataPath, "LogInfo.json");
#else
                return Path.Combine(Application.dataPath, "LogInfo.json");
#endif
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
            InitDeviceId();

            var reflection = new Reflection();
            var logger = new SerilogLogger(LoadLogConfig(), reflection);

            var factory = new Factory("App", logger, reflection);
            factory.Dep<IApp>(this);
            factory.Dep<RSG.Utils.ILogger>(logger);
            var dispatcher = new Dispatcher(logger);
            this.Dispatcher = dispatcher;
            factory.Dep<IDispatcher>(dispatcher);
            factory.Dep<IDispatchQueue>(dispatcher);            
            factory.Dep<ISceneQuery>(new SceneQuery());
            factory.Dep<ISceneTraversal>(new SceneTraversal());
            this.PromiseTimer = new PromiseTimer();
            factory.Dep<IPromiseTimer>(this.PromiseTimer);

            this.SingletonManager = InitFactory(logger, factory, reflection);

            this.Factory = factory;

            SingletonManager.InstantiateSingletons(factory);

            this.Logger = factory.ResolveDep<RSG.Utils.ILogger>();

            InitRunningFile();

            SingletonManager.Startup();

            var taskManager = factory.ResolveDep<ITaskManager>();
            SingletonManager.Singletons.ForType((IUpdatable u) => taskManager.RegisterUpdatable(u));
            SingletonManager.Singletons.ForType((IRenderable r) => taskManager.RegisterRenderable(r));
            SingletonManager.Singletons.ForType((IEndOfFrameUpdatable u) => taskManager.RegisterEndOfFrameUpdatable(u));
            SingletonManager.Singletons.ForType((ILateUpdatable u) => taskManager.RegisterLateUpdatable(u));

            var appHub = InitAppHub();
            appHub.Shutdown = 
                () =>
                {
                    SingletonManager.Shutdown();

                    SingletonManager.Singletons.ForType((IUpdatable u) => taskManager.UnregisterUpdatable(u));
                    SingletonManager.Singletons.ForType((IRenderable r) => taskManager.UnregisterRenderable(r));
                    SingletonManager.Singletons.ForType((IEndOfFrameUpdatable u) => taskManager.UnregisterEndOfFrameUpdatable(u));
                    SingletonManager.Singletons.ForType((ILateUpdatable u) => taskManager.UnregisterLateUpdatable(u));

                    DeleteRunningFile();
                };
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
        /// Load logger configuration from a file.
        /// </summary>
        private LogConfig LoadLogConfig()
        {
            //
            // Load log configuration.
            //
            if (!File.Exists(LogConfigFilePath))
            {
                return new LogConfig();
            }

            Debug.Log("Loading log configuration file: " + LogConfigFilePath);

            try
            {
                return JsonConvert.DeserializeObject<LogConfig>(File.ReadAllText(LogConfigFilePath));
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to load log configuration file: " + LogConfigFilePath);
                Debug.LogException(ex);

                return new LogConfig();
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

        /// <summary>
        /// Used to schedule code onto the main thread.
        /// </summary>
        public IDispatcher Dispatcher
        {
            get;
            private set;
        }

        /// <summary>
        /// Get the global promise timer.
        /// </summary>
        public IPromiseTimer PromiseTimer 
        { 
            get;
            private set;
        }

        /// <summary>
        /// Global singleton manager.
        /// </summary>
        public ISingletonManager SingletonManager
        {
            get;
            private set;
        }
    }
}
