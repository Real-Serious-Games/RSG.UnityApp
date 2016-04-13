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
            RSG.Utils.Argument.NotNull(() => logger);
            RSG.Utils.Argument.StringNotNullOrEmpty(() => reportsPath);

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

            try
            {
                var msg = msgTemplate + 
                    Screen.resolutions
                        .Select((resolution, i) => "[{ResolutionIndex}] {resolutionWidth} x {resolutionHeight} ({resolutionRefreshRate} hz)")
                        .Join("\r\n");

                object[] msgParams = LinqExts.FromItems<object>(
                    Application.dataPath,
                    Application.internetReachability,
                    Application.loadedLevelName,
                    Application.persistentDataPath,
                    Application.platform,
                    Application.unityVersion,
                    SystemInfo.deviceModel,
                    SystemInfo.deviceName,
                    SystemInfo.deviceType,
                    SystemInfo.deviceUniqueIdentifier,
                    SystemInfo.graphicsDeviceID,
                    SystemInfo.graphicsDeviceName,
                    SystemInfo.graphicsDeviceVendor,
                    SystemInfo.graphicsDeviceVendorID,
                    SystemInfo.graphicsDeviceVersion,
                    SystemInfo.graphicsMemorySize,
                    SystemInfo.graphicsPixelFillrate,
                    SystemInfo.graphicsShaderLevel,
                    SystemInfo.npotSupport,
                    SystemInfo.operatingSystem,
                    SystemInfo.processorCount,
                    SystemInfo.processorType,
                    SystemInfo.supportedRenderTargetCount,
                    SystemInfo.supports3DTextures,
                    SystemInfo.supportsAccelerometer,
                    SystemInfo.supportsComputeShaders,
                    SystemInfo.supportsGyroscope,
                    SystemInfo.supportsImageEffects,
                    SystemInfo.supportsInstancing,
                    SystemInfo.supportsLocationService,
                    SystemInfo.supportsRenderTextures,
                    SystemInfo.supportsRenderToCubemap,
                    SystemInfo.supportsShadows,
                    SystemInfo.supportsStencil,
                    SystemInfo.supportsVibration,
                    SystemInfo.systemMemorySize,
                    QualitySettings.names[QualitySettings.GetQualityLevel()] + " (" + QualitySettings.GetQualityLevel() + ")",
                    QualitySettings.names,
                    QualitySettings.activeColorSpace,
                    QualitySettings.anisotropicFiltering,
                    QualitySettings.antiAliasing,
                    QualitySettings.blendWeights,
                    QualitySettings.desiredColorSpace,
                    QualitySettings.lodBias,
                    QualitySettings.masterTextureLimit,
                    QualitySettings.maximumLODLevel,
                    QualitySettings.maxQueuedFrames,
                    QualitySettings.particleRaycastBudget,
                    QualitySettings.pixelLightCount,
                    QualitySettings.shadowCascades,
                    QualitySettings.shadowDistance,
                    QualitySettings.shadowProjection,
                    QualitySettings.softVegetation,
                    QualitySettings.vSyncCount,
                    Screen.currentResolution.width, Screen.currentResolution.height,
                    Screen.currentResolution.refreshRate,
                    Screen.fullScreen
                ).
                Concat(
                    Screen.resolutions
                        .SelectMany(resolution => 
                            LinqExts.FromItems<object>(resolution.width, resolution.height, resolution.refreshRate)
                        )
                )
                .ToArray();

                logger.LogInfo(msg, msgParams);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get Unity system info");
            }
        }

        public static readonly string msgTemplate = 
            "=== Unity System Information ===\r\n" +
            "Application\r\n" + 
            "    dataPath: {dataPath}\r\n" + 
            "    internetReachability: {internetReachability}\r\n" + 
            "    loadedLevelName: {loadedLevelName}\r\n" + 
            "    persistentDataPath: {persistentDataPath}\r\n" + 
            "    platform: {platform}\r\n" + 
            "    unityVersion: {unityVersion}\r\n" + 
            "SystemInfo\r\n" + 
            "    deviceModel: {deviceModel}\r\n" + 
            "    deviceName: {deviceName}\r\n" + 
            "    deviceType: {deviceType}\r\n" + 
            "    deviceUniqueIdentifier: {deviceUniqueIdentifier}\r\n" + 
            "    graphicsDeviceID: {graphicsDeviceID}\r\n" + 
            "    graphicsDeviceName: {graphicsDeviceName}\r\n" + 
            "    graphicsDeviceVendor: {graphicsDeviceVendor}\r\n" + 
            "    graphicsDeviceVendorID: {graphicsDeviceVendorID}\r\n" + 
            "    graphicsDeviceVersion: {graphicsDeviceVersion}\r\n" + 
            "    graphicsMemorySize: {graphicsMemorySize}\r\n" + 
            "    graphicsPixelFillrate: {graphicsPixelFillrate}\r\n" + 
            "    graphicsShaderLevel: {graphicsShaderLevel}\r\n" + 
            "    npotSupport: {npotSupport}\r\n" + 
            "    operatingSystem: {operatingSystem}\r\n" + 
            "    processorCount: {processorCount}\r\n" + 
            "    processorType: {processorType}\r\n" + 
            "    supportedRenderTargetCount: {supportedRenderTargetCount}\r\n" + 
            "    supports3DTextures: {supports3DTextures}\r\n" + 
            "    supportsAccelerometer: {supportsAccelerometer}\r\n" + 
            "    supportsComputeShaders: {supportsComputeShaders}\r\n" + 
            "    supportsGyroscope: {supportsGyroscope}\r\n" + 
            "    supportsImageEffects: {supportsImageEffects}\r\n" + 
            "    supportsInstancing: {supportsInstancing}\r\n" + 
            "    supportsLocationService: {supportsLocationService}\r\n" + 
            "    supportsRenderTextures: {supportsRenderTextures}\r\n" + 
            "    supportsRenderToCubemap: {supportsRenderToCubemap}\r\n" + 
            "    supportsShadows: {supportsShadows}\r\n" + 
            "    supportsStencil: {supportsStencil}\r\n" + 
            "    supportsVibration: {supportsVibration}\r\n" + 
            "    systemMemorySize: {systemMemorySize}\r\n" + 
            "QualitySettings\r\n" + 
            "    Current quality level: {qualityLevel}\r\n" + 
            "    Quality level names: {qualityLevels}\r\n" + 
            "    activeColorSpace: {activeColorSpace}\r\n" + 
            "    anisotropicFiltering: {anisotropicFiltering}\r\n" + 
            "    antiAliasing: {antiAliasing}\r\n" + 
            "    blendWeights: {blendWeights}\r\n" + 
            "    desiredColorSpace: {desiredColorSpace}\r\n" + 
            "    lodBias: {lodBias}\r\n" + 
            "    masterTextureLimit: {masterTextureLimit}\r\n" + 
            "    maximumLODLevel: {maximumLODLevel}\r\n" + 
            "    maxQueuedFrames: {maxQueuedFrames}\r\n" + 
            "    particleRaycastBudget: {particleRaycastBudget}\r\n" + 
            "    pixelLightCount: {pixelLightCount}\r\n" + 
            "    shadowCascades: {shadowCascades}\r\n" + 
            "    shadowDistance: {shadowDistance}\r\n" + 
            "    shadowProjection: {shadowProjection}\r\n" + 
            "    softVegetation: {softVegetation}\r\n" + 
            "    vSyncCount: {vSyncCount}\r\n" + 
            "Screen\r\n" + 
            "    currentResolution: {resolutionX} x {resolutionY}\r\n" + 
            "    refreshRate: {refreshRate} hz\r\n" + 
            "    fullScreen: {fullScreen}\r\n" + 
            "Resolutions\r\n";
    }
}
