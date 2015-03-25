using Newtonsoft.Json;
using RSG.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RSG
{
    /// <summary>
    /// Represents a particular settings file.
    /// </summary>
    public interface ISettings<SettingsT>
        where SettingsT : new()
    {
        /// <summary>
        /// The loaded settings data.
        /// </summary>
        SettingsT Data
        {
            get;
        }

        /// <summary>
        /// Event raised when the underlying settings file has changed and the settings have been reloaded.
        /// </summary>
        event EventHandler<EventArgs> SettingsChanged;
    }

    /// <summary>
    /// Represents a particular settings file.
    /// </summary>
    public class Settings<SettingsT> : ISettings<SettingsT>
        where SettingsT : new()
    {
        /// <summary>
        /// The settings file that is loaded and watched for reload.
        /// </summary>
        private string settingsFilePath;

        private ILogger logger;

        /// <summary>
        /// The loaded settings data.
        /// </summary>
        public SettingsT Data
        {
            get;
            private set;
        }

        /// <summary>
        /// Event raised when the underlying settings file has changed and the settings have been reloaded.
        /// </summary>
        public event EventHandler<EventArgs> SettingsChanged;

        public Settings(string settingsFilePath, ILogger logger)
        {
            Argument.StringNotNullOrEmpty(() => settingsFilePath);
            Argument.NotNull(() => logger);

            this.settingsFilePath = settingsFilePath;
            this.logger = logger;

            try
            {
                LoadSettings();
            }
            catch (Exception ex)
            {
                this.Data = new SettingsT(); // Error has already been reported.
            }

            WatchConfigFile();
        }

        /// <summary>
        /// Load (or reload) the settings files.
        /// </summary>
        private void LoadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    this.Data = JsonConvert.DeserializeObject<SettingsT>(File.ReadAllText(settingsFilePath));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to load settings file {SettingsFilePath}", settingsFilePath);
                    throw ex;
                }
            }
            else
            {
                this.Data = new SettingsT();
            }
        }

        /// <summary>
        /// Watch the config file for changes, and reload as necessary.
        /// </summary>
        private void WatchConfigFile()
        {
            try
            {
                var watcher = new FileSystemWatcher();
                watcher.Path = Path.GetDirectoryName(settingsFilePath);
                watcher.Filter = Path.GetFileName(settingsFilePath);
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Changed += 
                    (object sender, FileSystemEventArgs e) =>
                    {
                        try
                        {
                            LoadSettings();
                        }
                        catch
                        {
                            // Error has already been reported, swallow errors but don't replace the data.
                        }

                        if (SettingsChanged != null)
                        {
                            SettingsChanged(this, EventArgs.Empty);
                        }
                    };
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to set watch on file {SettingsFilePath}", SettingsFilePath)
            }
        }
    }

    /// <summary>
    /// For loading, saving and managing settings.
    /// </summary>
    public interface ISettingsLoader
    {
        /// <summary>
        /// Load typed settings.
        /// If requested file doesn't exist, a default settings object will be created.
        /// </summary>
        ISettings<SettingsT> Load<SettingsT>(string settingsName)
            where SettingsT : new();
    }

    /// <summary>
    /// For loading, saving and managing settings.
    /// </summary>
    [LazySingleton(typeof(ISettingsLoader))]
    public class SettingsLoader : ISettingsLoader
    {
        [Dependency]
        public ILogger Logger { get; set; }

        /// <summary>
        /// The path that contains settings.
        /// </summary>
        public string settingsFolderPath = Path.Combine(Application.dataPath, "Settings");

        /// <summary>
        /// Load typed settings.
        /// If requested file doesn't exist, a default settings object will be created.
        /// </summary>
        public ISettings<SettingsT> Load<SettingsT>(string settingsName)
            where SettingsT : new()
        {
            var settingsFilePath = Path.Combine(settingsFolderPath, settingsName + ".json");
            return new Settings<SettingsT>(settingsFilePath, Logger);
        }
    }
}
