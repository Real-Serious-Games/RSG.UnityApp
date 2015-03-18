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
                reportLogger.LogInfo("    dataPath: {dataPath}", Application.dataPath);
                reportLogger.LogInfo("    internetReachability: {internetReachability}", Application.internetReachability);
                reportLogger.LogInfo("    loadedLevelName: {loadedLevelName}", Application.loadedLevelName);
                reportLogger.LogInfo("    persistentDataPath: {persistentDataPath}", Application.persistentDataPath);
                reportLogger.LogInfo("    platform: {platform}", Application.platform);
                reportLogger.LogInfo("    unityVersion: {unityVersion}", Application.unityVersion);
                reportLogger.LogInfo("SystemInfo");
                reportLogger.LogInfo("    deviceModel: {deviceModel}", SystemInfo.deviceModel);
                reportLogger.LogInfo("    deviceName: {deviceName}", SystemInfo.deviceName);
                reportLogger.LogInfo("    deviceType: {deviceType}", SystemInfo.deviceType);
                reportLogger.LogInfo("    deviceUniqueIdentifier: {deviceUniqueIdentifier}", SystemInfo.deviceUniqueIdentifier);
                reportLogger.LogInfo("    graphicsDeviceID: {graphicsDeviceID}", SystemInfo.graphicsDeviceID);
                reportLogger.LogInfo("    graphicsDeviceName: {graphicsDeviceName}", SystemInfo.graphicsDeviceName);
                reportLogger.LogInfo("    graphicsDeviceVendor: {graphicsDeviceVendor}", SystemInfo.graphicsDeviceVendor);
                reportLogger.LogInfo("    graphicsDeviceVendorID: {graphicsDeviceVendorID}", SystemInfo.graphicsDeviceVendorID);
                reportLogger.LogInfo("    graphicsDeviceVersion: {graphicsDeviceVersion}", SystemInfo.graphicsDeviceVersion);
                reportLogger.LogInfo("    graphicsMemorySize: {graphicsMemorySize}", SystemInfo.graphicsMemorySize);
                reportLogger.LogInfo("    graphicsPixelFillrate: {graphicsPixelFillrate}", SystemInfo.graphicsPixelFillrate);
                reportLogger.LogInfo("    graphicsShaderLevel: {graphicsShaderLevel}", SystemInfo.graphicsShaderLevel);
                reportLogger.LogInfo("    npotSupport: {npotSupport}", SystemInfo.npotSupport);
                reportLogger.LogInfo("    operatingSystem: {operatingSystem}", SystemInfo.operatingSystem);
                reportLogger.LogInfo("    processorCount: {processorCount}", SystemInfo.processorCount);
                reportLogger.LogInfo("    processorType: {processorType}", SystemInfo.processorType);
                reportLogger.LogInfo("    supportedRenderTargetCount: {supportedRenderTargetCount}", SystemInfo.supportedRenderTargetCount);
                reportLogger.LogInfo("    supports3DTextures: {supports3DTextures}", SystemInfo.supports3DTextures);
                reportLogger.LogInfo("    supportsAccelerometer: {supportsAccelerometer}", SystemInfo.supportsAccelerometer);
                reportLogger.LogInfo("    supportsComputeShaders: {supportsComputeShaders}", SystemInfo.supportsComputeShaders);
                reportLogger.LogInfo("    supportsGyroscope: {supportsGyroscope}", SystemInfo.supportsGyroscope);
                reportLogger.LogInfo("    supportsImageEffects: {supportsImageEffects}", SystemInfo.supportsImageEffects);
                reportLogger.LogInfo("    supportsInstancing: {supportsInstancing}", SystemInfo.supportsInstancing);
                reportLogger.LogInfo("    supportsLocationService: {supportsLocationService}", SystemInfo.supportsLocationService);
                reportLogger.LogInfo("    supportsRenderTextures: {supportsRenderTextures}", SystemInfo.supportsRenderTextures);
                reportLogger.LogInfo("    supportsRenderToCubemap: {supportsRenderToCubemap}", SystemInfo.supportsRenderToCubemap);
                reportLogger.LogInfo("    supportsShadows: {supportsShadows}", SystemInfo.supportsShadows);
                reportLogger.LogInfo("    supportsStencil: {supportsStencil}", SystemInfo.supportsStencil);
                reportLogger.LogInfo("    supportsVibration: {supportsVibration}", SystemInfo.supportsVibration);
                reportLogger.LogInfo("    systemMemorySize: {systemMemorySize}", SystemInfo.systemMemorySize);
                reportLogger.LogInfo("QualitySettings");
                reportLogger.LogInfo("    Current quality level: {qualityLevel}", QualitySettings.names[QualitySettings.GetQualityLevel()] + " (" + QualitySettings.GetQualityLevel() + ")");
                reportLogger.LogInfo("    Quality level names: {qualityLevels}", QualitySettings.names);
                reportLogger.LogInfo("    activeColorSpace: {activeColorSpace}", QualitySettings.activeColorSpace);
                reportLogger.LogInfo("    anisotropicFiltering: {anisotropicFiltering}", QualitySettings.anisotropicFiltering);
                reportLogger.LogInfo("    antiAliasing: {antiAliasing}", QualitySettings.antiAliasing);
                reportLogger.LogInfo("    blendWeights: {blendWeights}", QualitySettings.blendWeights);
                reportLogger.LogInfo("    desiredColorSpace: {desiredColorSpace}", QualitySettings.desiredColorSpace);
                reportLogger.LogInfo("    lodBias: {lodBias}", QualitySettings.lodBias);
                reportLogger.LogInfo("    masterTextureLimit: {masterTextureLimit}", QualitySettings.masterTextureLimit);
                reportLogger.LogInfo("    maximumLODLevel: {desiredColorSpace}", QualitySettings.desiredColorSpace);
                reportLogger.LogInfo("    maxQueuedFrames: {maxQueuedFrames}", QualitySettings.maxQueuedFrames);
                reportLogger.LogInfo("    particleRaycastBudget: {particleRaycastBudget}", QualitySettings.particleRaycastBudget);
                reportLogger.LogInfo("    pixelLightCount: {pixelLightCount}", QualitySettings.pixelLightCount);
                reportLogger.LogInfo("    shadowCascades: {shadowCascades}", QualitySettings.shadowCascades);
                reportLogger.LogInfo("    shadowDistance: {shadowDistance}", QualitySettings.shadowDistance);
                reportLogger.LogInfo("    shadowProjection: {shadowProjection}", QualitySettings.shadowProjection);
                reportLogger.LogInfo("    softVegetation: {softVegetation}", QualitySettings.softVegetation);
                reportLogger.LogInfo("    vSyncCount: {vSyncCount}", QualitySettings.vSyncCount);
                reportLogger.LogInfo("Screen");
                reportLogger.LogInfo("    currentResolution: {resolutionX} x {resolutionY}", Screen.currentResolution.width, Screen.currentResolution.height);
                reportLogger.LogInfo("    refreshRate: {refreshRate} hz", Screen.currentResolution.refreshRate);
                reportLogger.LogInfo("    fullScreen: {fullScreen}", Screen.fullScreen);

                reportLogger.LogInfo("    Resolutions");

                Screen.resolutions.Each((resolution, i) => 
                {
                    reportLogger.LogInfo("        [{ResolutionIndex}] {resolutionWidth} x {resolutionHeight} ({resolutionRefreshRate} hz)", i, resolution.width, resolution.height, resolution.refreshRate);
                });
            }
            catch (Exception ex)
            {
                reportLogger.LogError(ex, "Failed to get Unity system info");
            }
        }
    }
}
