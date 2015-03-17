using RSG.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RSG
{
    /// <summary>
    /// A helper class to log system information.
    /// </summary>
    public class LogSystemInfo
    {
        /// <summary>
        /// Used to log the information.
        /// </summary>
        private RSG.Utils.ILogger logger;

        /// <summary>
        /// Path to output the system reports.
        /// </summary>
        private string reportsPath;

        public LogSystemInfo(RSG.Utils.ILogger logger, string reportsPath)
        {
            Argument.NotNull(() => logger);
            Argument.StringNotNullOrEmpty(() => reportsPath);

            this.logger = logger;
            this.reportsPath = reportsPath;
        }

        /// <summary>
        /// Log system information.
        /// </summary>
        public void Output()
        {
            try
            {
                if (!Directory.Exists(reportsPath))
                {
                    logger.LogInfo("Creating directory {SystemReportsDirectoryPath}", reportsPath);
                    Directory.CreateDirectory(reportsPath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create system reports path.");
            }

            var systemReportFile = Path.Combine(reportsPath, "Unity - System Info.txt");

            var fileLoggerConfig = new LoggerConfiguration()
                .WriteTo.File(systemReportFile, Serilog.Events.LogEventLevel.Debug)
                .Enrich.With<RSGLogEnricher>();
            var fileLogger = new SerilogLogger(fileLoggerConfig.CreateLogger());
            var reportLogger = new MultiLogger(logger, fileLogger);

            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                var outputPath = Path.Combine(reportsPath, "dxdiag.txt");
                logger.LogInfo("Saving dxdiag output to " + outputPath);

                try
                {
                    System.Diagnostics.Process.Start("dxdiag", "/t " + outputPath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to run dxdiag");
                }
            }

            logger.LogInfo("Saving system report to {SystemReportPath}", systemReportFile);

            try
            {
                reportLogger.LogInfo("=== Unity System Information ===");
                reportLogger.LogInfo("Application");
                reportLogger.LogInfo("    dataPath: " + Application.dataPath);
                reportLogger.LogInfo("    internetReachability: " + Application.internetReachability);
                reportLogger.LogInfo("    loadedLevelName: " + Application.loadedLevelName);
                reportLogger.LogInfo("    persistentDataPath: " + Application.persistentDataPath);
                reportLogger.LogInfo("    platform: " + Application.platform);
                reportLogger.LogInfo("    unityVersion: " + Application.unityVersion);
                reportLogger.LogInfo("SystemInfo");
                reportLogger.LogInfo("    deviceModel: " + SystemInfo.deviceModel);
                reportLogger.LogInfo("    deviceName: " + SystemInfo.deviceName);
                reportLogger.LogInfo("    deviceType: " + SystemInfo.deviceType);
                reportLogger.LogInfo("    deviceUniqueIdentifier: " + SystemInfo.deviceUniqueIdentifier);
                reportLogger.LogInfo("    graphicsDeviceID: " + SystemInfo.graphicsDeviceID);
                reportLogger.LogInfo("    graphicsDeviceName: " + SystemInfo.graphicsDeviceName);
                reportLogger.LogInfo("    graphicsDeviceVendor: " + SystemInfo.graphicsDeviceVendor);
                reportLogger.LogInfo("    graphicsDeviceVendorID: " + SystemInfo.graphicsDeviceVendorID);
                reportLogger.LogInfo("    graphicsDeviceVersion: " + SystemInfo.graphicsDeviceVersion);
                reportLogger.LogInfo("    graphicsMemorySize: " + SystemInfo.graphicsMemorySize);
                reportLogger.LogInfo("    graphicsPixelFillrate: " + SystemInfo.graphicsPixelFillrate);
                reportLogger.LogInfo("    graphicsShaderLevel: " + SystemInfo.graphicsShaderLevel);
                reportLogger.LogInfo("    npotSupport: " + SystemInfo.npotSupport);
                reportLogger.LogInfo("    operatingSystem: " + SystemInfo.operatingSystem);
                reportLogger.LogInfo("    processorCount: " + SystemInfo.processorCount);
                reportLogger.LogInfo("    processorType: " + SystemInfo.processorType);
                reportLogger.LogInfo("    supportedRenderTargetCount: " + SystemInfo.supportedRenderTargetCount);
                reportLogger.LogInfo("    supports3DTextures: " + SystemInfo.supports3DTextures);
                reportLogger.LogInfo("    supportsAccelerometer: " + SystemInfo.supportsAccelerometer);
                reportLogger.LogInfo("    supportsComputeShaders: " + SystemInfo.supportsComputeShaders);
                reportLogger.LogInfo("    supportsGyroscope: " + SystemInfo.supportsGyroscope);
                reportLogger.LogInfo("    supportsImageEffects: " + SystemInfo.supportsImageEffects);
                reportLogger.LogInfo("    supportsInstancing: " + SystemInfo.supportsInstancing);
                reportLogger.LogInfo("    supportsLocationService: " + SystemInfo.supportsLocationService);
                reportLogger.LogInfo("    supportsRenderTextures: " + SystemInfo.supportsRenderTextures);
                reportLogger.LogInfo("    supportsRenderToCubemap: " + SystemInfo.supportsRenderToCubemap);
                reportLogger.LogInfo("    supportsShadows: " + SystemInfo.supportsShadows);
                reportLogger.LogInfo("    supportsStencil: " + SystemInfo.supportsStencil);
                reportLogger.LogInfo("    supportsVibration: " + SystemInfo.supportsVibration);
                reportLogger.LogInfo("    systemMemorySize: " + SystemInfo.systemMemorySize);
                reportLogger.LogInfo("QualitySettings");
                reportLogger.LogInfo("    Current quality level: " + QualitySettings.names[QualitySettings.GetQualityLevel()] + " (" + QualitySettings.GetQualityLevel() + ")");
                reportLogger.LogInfo("    Quality level names: " + QualitySettings.names.Join(", "));
                reportLogger.LogInfo("    activeColorSpace: " + QualitySettings.activeColorSpace);
                reportLogger.LogInfo("    anisotropicFiltering: " + QualitySettings.anisotropicFiltering);
                reportLogger.LogInfo("    antiAliasing: " + QualitySettings.antiAliasing);
                reportLogger.LogInfo("    blendWeights: " + QualitySettings.blendWeights);
                reportLogger.LogInfo("    desiredColorSpace: " + QualitySettings.desiredColorSpace);
                reportLogger.LogInfo("    lodBias: " + QualitySettings.lodBias);
                reportLogger.LogInfo("    masterTextureLimit: " + QualitySettings.masterTextureLimit);
                reportLogger.LogInfo("    maximumLODLevel: " + QualitySettings.desiredColorSpace);
                reportLogger.LogInfo("    maxQueuedFrames: " + QualitySettings.maxQueuedFrames);
                reportLogger.LogInfo("    particleRaycastBudget: " + QualitySettings.particleRaycastBudget);
                reportLogger.LogInfo("    pixelLightCount: " + QualitySettings.pixelLightCount);
                reportLogger.LogInfo("    shadowCascades: " + QualitySettings.shadowCascades);
                reportLogger.LogInfo("    shadowDistance: " + QualitySettings.desiredColorSpace);
                reportLogger.LogInfo("    shadowProjection: " + QualitySettings.desiredColorSpace);
                reportLogger.LogInfo("    softVegetation: " + QualitySettings.desiredColorSpace);
                reportLogger.LogInfo("    vSyncCount: " + QualitySettings.desiredColorSpace);

                reportLogger.LogInfo("Screen");
                reportLogger.LogInfo("    currentResolution: " + Screen.currentResolution.width + "x" + Screen.currentResolution.height + " (" + Screen.currentResolution.refreshRate + "hz)");
                reportLogger.LogInfo("    fullScreen: " + Screen.fullScreen);

                reportLogger.LogInfo("    Resolutions");

                foreach (var resolution in Screen.resolutions)
                {
                    reportLogger.LogInfo("        " + resolution.width + "x" + resolution.height + " (" + resolution.refreshRate + "hz)");
                }
            }
            catch (Exception ex)
            {
                reportLogger.LogError(ex, "Failed to get Unity system info");
            }
        }
    }
}
