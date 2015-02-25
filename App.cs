using RSG.Factory;
using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG.Unity
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
        ILogger Logger { get; }
    }

    /// <summary>
    /// Singleton application class. Used AppInit to bootstrap in a Unity scene.
    /// </summary>
    public class App : IApp
    {
        /// <summary>
        /// Accessor for the singleton app instance.
        /// </summary>
        public static IApp Instance { get; private set; }

        public static void Init()
        {
            if (Instance != null)
            {
                // Already initialised.
                return;
            }

            Instance = new App();
        }

        public App()
        {
            var logger = new UnityLogger();
            var factory = new Factory.Factory("App", logger);
            factory.Dep<ILogger>(logger);

            var reflection = new Reflection();

            //todo: all this code should merge into RSG.Factory.
            factory.AutoRegisterTypes(reflection);

            var singletonManager = new SingletonManager(reflection, logger, factory);

            factory.Dep<IReflection>(reflection);
            factory.AddDependencyProvider(singletonManager);

            var singletonScanner = new SingletonScanner(reflection, logger, singletonManager);
            singletonScanner.ScanSingletonTypes();

            this.Factory = factory;
            this.Logger = logger;
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
        public ILogger Logger
        {
            get;
            private set;
        }
    }
}
