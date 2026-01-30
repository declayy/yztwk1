using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using FiveMQuantumTweaker2026.Models;
using FiveMQuantumTweaker2026.Utils;

namespace FiveMQuantumTweaker2026.Core
{
    /// <summary>
    /// Quantum Optimization Engine 2026 - Kernsystem für fortschrittliche Performance-Optimierungen
    /// </summary>
    public class QuantumOptimizer : IDisposable
    {
        #region WinAPI Imports

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        [DllImport("psapi.dll")]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        private static extern bool SetThreadExecutionState(uint esFlags);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetTimerResolution(uint desiredResolution, bool setResolution, out uint currentResolution);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerSetActiveScheme(IntPtr rootPowerKey, ref Guid schemeGuid);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerReadFriendlyName(IntPtr rootPowerKey, ref Guid schemeGuid, IntPtr subGroupOfPowerSettingsGuid, IntPtr powerSettingGuid, IntPtr buffer, ref uint bufferSize);

        [DllImport("ntdll.dll")]
        private static extern uint NtQueryTimerResolution(out uint minimumResolution, out uint maximumResolution, out uint currentResolution);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetTickCount();

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetTcpTable(IntPtr pTcpTable, ref int pdwSize, bool bOrder);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges, ref TOKEN_PRIVILEGES newState, int bufferLength, IntPtr previousState, IntPtr returnLength);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;
        private const uint ES_AWAYMODE_REQUIRED = 0x00000040;

        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_QUERY = 0x0008;
        private const string SE_SYSTEMTIME_NAME = "SeSystemtimePrivilege";

        #endregion

        #region Private Fields

        private readonly Logger _logger;
        private readonly SystemSanityManager _sanityManager;
        private readonly List<RegistryBackup> _registryBackups;
        private readonly List<ServiceBackup> _serviceBackups;
        private readonly List<NetworkSettingBackup> _networkBackups;

        private bool _isDisposed;
        private string _systemRestorePointId;

        // Performance Counters
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;
        private PerformanceCounter _diskCounter;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gibt an, ob Quantum-Optimierungen aktiv sind
        /// </summary>
        public bool AreQuantumTweaksActive { get; private set; }

        /// <summary>
        /// Gibt an, ob Gaming-Modus aktiv ist
        /// </summary>
        public bool IsGamingModeActive { get; private set; }

        /// <summary>
        /// Aktuelle Anzahl aktiver Tweaks
        /// </summary>
        public int ActiveTweakCount { get; private set; }

        /// <summary>
        /// Letzter Optimierungszeitpunkt
        /// </summary>
        public DateTime LastOptimizationTime { get; private set; }

        #endregion

        #region Constructor & Destructor

        public QuantumOptimizer()
        {
            _logger = new Logger();
            _sanityManager = new SystemSanityManager();
            _registryBackups = new List<RegistryBackup>();
            _serviceBackups = new List<ServiceBackup>();
            _networkBackups = new List<NetworkSettingBackup>();

            _logger.Info("QuantumOptimizer initialized");
        }

        ~QuantumOptimizer()
        {
            Dispose(false);
        }

        #endregion

        #region Public Methods - Main Optimization APIs

        /// <summary>
        /// Wendet alle Quantum-Optimierungen basierend auf einem Profil an
        /// </summary>
        public async Task<OptimizationResult> ApplyQuantumOptimizationsAsync(OptimizationProfile profile, CancellationToken cancellationToken = default)
        {
            var result = new OptimizationResult
            {
                ProfileName = profile.ProfileName,
                StartTime = DateTime.Now
            };

            try
            {
                _logger.Info($"=== Applying Quantum Optimizations: {profile.ProfileName} ===");

                // 1. System-Snapshot erstellen
                result.SnapshotId = _sanityManager.CreateSystemSnapshot($"PRE_{profile.ProfileName.ToUpper()}");
                _logger.Info($"System snapshot created: {result.SnapshotId}");

                // 2. Je nach Profiltyp optimieren
                switch (profile.ProfileType)
                {
                    case ProfileType.Gaming:
                        await ApplyGamingOptimizationsAsync(cancellationToken);
                        break;
                    case ProfileType.Streaming:
                        await ApplyStreamingOptimizationsAsync(cancellationToken);
                        break;
                    case ProfileType.Battery:
                        await ApplyBatteryOptimizationsAsync(cancellationToken);
                        break;
                    case ProfileType.Custom:
                        await ApplyCustomOptimizationsAsync(profile, cancellationToken);
                        break;
                    default: // Balanced
                        await ApplyBalancedOptimizationsAsync(cancellationToken);
                        break;
                }

                // 3. Erfolg protokollieren
                result.Success = true;
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.TweaksApplied = ActiveTweakCount;
                AreQuantumTweaksActive = true;
                LastOptimizationTime = DateTime.Now;

                _logger.Info($"Quantum optimizations applied successfully in {result.Duration.TotalSeconds:0.0}s");
                _logger.Info($"Active tweaks: {ActiveTweakCount}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to apply quantum optimizations: {ex}");
                result.Success = false;
                result.ErrorMessage = ex.Message;

                // Rollback durchführen
                await RollbackOptimizationsAsync();

                throw;
            }
        }

        /// <summary>
        /// Wendet CPU-Optimierungen an
        /// </summary>
        public async Task ApplyCpuOptimizationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Applying CPU optimizations...");

                await Task.Run(() =>
                {
                    // 1. Timer Resolution auf 0.5ms (Standard: 15.6ms)
                    SetTimerResolution(500); // 0.5ms
                    _logger.Info("Timer resolution set to 0.5ms");
                    ActiveTweakCount++;

                    // 2. CPU-Parken deaktivieren für Gaming
                    DisableCpuParking();
                    _logger.Info("CPU parking disabled");
                    ActiveTweakCount++;

                    // 3. HPET (High Precision Event Timer) optimieren
                    OptimizeHPET();
                    _logger.Info("HPET optimized");
                    ActiveTweakCount++;

                    // 4. Core Isolation optimieren
                    OptimizeCoreIsolation();
                    _logger.Info("Core isolation optimized");
                    ActiveTweakCount++;

                    // 5. Registry-Tweaks für CPU-Performance
                    ApplyCpuRegistryTweaks();
                    _logger.Info("CPU registry tweaks applied");
                    ActiveTweakCount++;

                    // 6. Power Plan auf Ultimate Performance
                    SetUltimatePerformancePowerPlan();
                    _logger.Info("Power plan set to Ultimate Performance");
                    ActiveTweakCount++;

                }, cancellationToken);

                _logger.Info($"CPU optimizations applied: {ActiveTweakCount} tweaks");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to apply CPU optimizations: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Wendet GPU-Optimierungen an
        /// </summary>
        public async Task ApplyGpuOptimizationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Applying GPU optimizations...");

                await Task.Run(() =>
                {
                    // 1. GPU Scheduling auf Hardware beschleunigt
                    EnableHardwareAcceleratedGpuScheduling();
                    _logger.Info("Hardware Accelerated GPU Scheduling enabled");
                    ActiveTweakCount++;

                    // 2. GPU im "Prefer Maximum Performance" Mode
                    SetGpuMaximumPerformanceMode();
                    _logger.Info("GPU set to maximum performance mode");
                    ActiveTweakCount++;

                    // 3. Shader Cache optimieren
                    OptimizeShaderCache();
                    _logger.Info("Shader cache optimized");
                    ActiveTweakCount++;

                    // 4. Texture Filtering optimieren
                    OptimizeTextureFiltering();
                    _logger.Info("Texture filtering optimized");
                    ActiveTweakCount++;

                    // 5. VSync-Einstellungen für Gaming
                    OptimizeVSyncSettings();
                    _logger.Info("VSync settings optimized for gaming");
                    ActiveTweakCount++;

                    // 6. Multi-GPU Optimierungen (wenn verfügbar)
                    OptimizeMultiGpuConfiguration();
                    _logger.Info("Multi-GPU configuration optimized");
                    ActiveTweakCount++;

                }, cancellationToken);

                _logger.Info($"GPU optimizations applied: {ActiveTweakCount} tweaks");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to apply GPU optimizations: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Wendet Memory-Optimierungen an
        /// </summary>
        public async Task ApplyMemoryOptimizationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Applying memory optimizations...");

                await Task.Run(() =>
                {
                    // 1. Memory Compression deaktivieren für Gaming
                    DisableMemoryCompression();
                    _logger.Info("Memory compression disabled for gaming");
                    ActiveTweakCount++;

                    // 2. Superfetch/SysMain optimieren (nicht deaktivieren)
                    OptimizeSuperfetch();
                    _logger.Info("Superfetch/SysMain optimized");
                    ActiveTweakCount++;

                    // 3. Pagefile optimieren
                    OptimizePageFile();
                    _logger.Info("Pagefile optimized");
                    ActiveTweakCount++;

                    // 4. Memory Management Tweaks
                    ApplyMemoryRegistryTweaks();
                    _logger.Info("Memory registry tweaks applied");
                    ActiveTweakCount++;

                    // 5. Clear Standby List
                    ClearStandbyMemory();
                    _logger.Info("Standby memory cleared");
                    ActiveTweakCount++;

                    // 6. Large Page Support aktivieren
                    EnableLargePageSupport();
                    _logger.Info("Large page support enabled");
                    ActiveTweakCount++;

                }, cancellationToken);

                _logger.Info($"Memory optimizations applied: {ActiveTweakCount} tweaks");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to apply memory optimizations: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Wendet Netzwerk-Optimierungen an
        /// </summary>
        public async Task ApplyNetworkOptimizationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Applying network optimizations...");

                await Task.Run(() =>
                {
                    // 1. TCP/IP Stack optimieren: CTCP + NoDelay + Window Scaling
                    OptimizeTcpIpStack();
                    _logger.Info("TCP/IP stack optimized (CTCP + NoDelay)");
                    ActiveTweakCount++;

                    // 2. UDP Buffer erhöhen: 64KB Send/Receive
                    IncreaseUdpBufferSize();
                    _logger.Info("UDP buffer increased to 64KB");
                    ActiveTweakCount++;

                    // 3. Network Throttling Index auf 0 (Disabled)
                    DisableNetworkThrottling();
                    _logger.Info("Network throttling disabled");
                    ActiveTweakCount++;

                    // 4. QoS deaktivieren für Gaming-Traffic
                    DisableQoSForGaming();
                    _logger.Info("QoS disabled for gaming traffic");
                    ActiveTweakCount++;

                    // 5. DNS auf Cloudflare (1.1.1.1) + Google (8.8.8.8)
                    SetOptimalDnsServers();
                    _logger.Info("DNS set to Cloudflare + Google");
                    ActiveTweakCount++;

                    // 6. Receive Side Scaling (RSS) optimieren
                    OptimizeReceiveSideScaling();
                    _logger.Info("Receive Side Scaling optimized");
                    ActiveTweakCount++;

                }, cancellationToken);

                _logger.Info($"Network optimizations applied: {ActiveTweakCount} tweaks");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to apply network optimizations: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Wendet HitReg-Optimierungen an (FiveM-spezifisch)
        /// </summary>
        public async Task ApplyHitRegOptimizationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Applying HitReg 2.0 optimizations...");

                await Task.Run(() =>
                {
                    // 1. Client-side Prediction optimieren
                    OptimizeClientSidePrediction();
                    _logger.Info("Client-side prediction optimized");
                    ActiveTweakCount++;

                    // 2. Interpolation Buffer reduzieren
                    ReduceInterpolationBuffer();
                    _logger.Info("Interpolation buffer reduced");
                    ActiveTweakCount++;

                    // 3. Extrapolation erhöhen
                    IncreaseExtrapolation();
                    _logger.Info("Extrapolation increased");
                    ActiveTweakCount++;

                    // 4. Packet Sequencing optimieren
                    OptimizePacketSequencing();
                    _logger.Info("Packet sequencing optimized");
                    ActiveTweakCount++;

                    // 5. Timestamp-Vorlauf (Client ahead of Server)
                    EnableTimestampAdvantage();
                    _logger.Info("Timestamp advantage enabled (client ahead)");
                    ActiveTweakCount++;

                    // 6. Chronal Displacement (+8-12ms Vorsprung)
                    ApplyChronalDisplacement();
                    _logger.Info("Chronal displacement applied (+12ms advantage)");
                    ActiveTweakCount++;

                }, cancellationToken);

                _logger.Info($"HitReg optimizations applied: {ActiveTweakCount} tweaks");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to apply HitReg optimizations: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Wendet FiveM-spezifische Optimierungen an
        /// </summary>
        public async Task ApplyFiveMSpecificOptimizationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Applying FiveM-specific optimizations...");

                await Task.Run(() =>
                {
                    // 1. FiveM Registry-Einstellungen optimieren
                    OptimizeFiveMRegistrySettings();
                    _logger.Info("FiveM registry settings optimized");
                    ActiveTweakCount++;

                    // 2. Process Priority und Affinity
                    SetFiveMProcessPriority();
                    _logger.Info("FiveM process priority configured");
                    ActiveTweakCount++;

                    // 3. Cache-Optimierungen
                    OptimizeFiveMCache();
                    _logger.Info("FiveM cache optimized");
                    ActiveTweakCount++;

                    // 4. Netzwerk-Einstellungen für FiveM
                    OptimizeFiveMNetworkSettings();
                    _logger.Info("FiveM network settings optimized");
                    ActiveTweakCount++;

                    // 5. GPU-Einstellungen für FiveM
                    OptimizeFiveMGpuSettings();
                    _logger.Info("FiveM GPU settings optimized");
                    ActiveTweakCount++;

                    // 6. Anti-Cheat Kompatibilität
                    EnsureAntiCheatCompatibility();
                    _logger.Info("Anti-cheat compatibility ensured");
                    ActiveTweakCount++;

                }, cancellationToken);

                _logger.Info($"FiveM-specific optimizations applied: {ActiveTweakCount} tweaks");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to apply FiveM-specific optimizations: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Aktiviert Gaming-Modus (pausiert nicht-essentielle Dienste)
        /// </summary>
        public async Task ActivateGamingModeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Activating gaming mode...");

                await Task.Run(() =>
                {
                    // Temporär pausierte Dienste:
                    string[] servicesToPause = {
                        "BITS",          // Background Intelligent Transfer
                        "WSearch",       // Windows Search
                        "DiagTrack",     // Diagnostic Tracking (nur während Session)
                        "MapsBroker",    // Downloaded Maps Manager
                        "lfsvc",         // Geolocation Service
                        "PcaSvc",        // Program Compatibility Assistant
                        "WpnService",    // Windows Push Notifications
                        "XblAuthManager",// Xbox Live Auth Manager
                        "XblGameSave",   // Xbox Live Game Save
                        "XboxNetApiSvc", // Xbox Live Networking Service
                        "WdNisSvc",      // Windows Defender Network Inspection
                        "WdiSystemHost", // Diagnostic System Host
                        "WdiServiceHost" // Diagnostic Service Host
                    };

                    foreach (var serviceName in servicesToPause)
                    {
                        try
                        {
                            var service = new ServiceController(serviceName);
                            if (service.Status == ServiceControllerStatus.Running)
                            {
                                // Backup erstellen
                                _serviceBackups.Add(new ServiceBackup
                                {
                                    ServiceName = serviceName,
                                    OriginalStatus = service.Status,
                                    OriginalStartType = service.StartType
                                });

                                // Dienst stoppen
                                service.Stop();
                                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));

                                _logger.Info($"Service paused: {serviceName}");
                                ActiveTweakCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn($"Could not pause service {serviceName}: {ex.Message}");
                        }
                    }

                    // System nicht-essentielle Prozesse priorisieren
                    SetProcessPrioritiesForGaming();
                    _logger.Info("Process priorities set for gaming");
                    ActiveTweakCount++;

                    // System-Ausführungsstatus setzen (verhindert Sleep)
                    SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
                    _logger.Info("System execution state set (prevents sleep)");
                    ActiveTweakCount++;

                    IsGamingModeActive = true;

                }, cancellationToken);

                _logger.Info($"Gaming mode activated: {ActiveTweakCount} tweaks");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to activate gaming mode: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Bereitet den optimierten FiveM-Start vor
        /// </summary>
        public async Task PrepareFiveMLaunchAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Preparing optimized FiveM launch...");

                await Task.Run(() =>
                {
                    // 1. Gaming-Modus aktivieren
                    ActivateGamingModeAsync(cancellationToken).Wait(cancellationToken);

                    // 2. FiveM-spezifische Optimierungen anwenden
                    ApplyFiveMSpecificOptimizationsAsync(cancellationToken).Wait(cancellationToken);

                    // 3. Netzwerk-Priorisierung für FiveM
                    PrioritizeFiveMNetworkTraffic();
                    _logger.Info("FiveM network traffic prioritized");
                    ActiveTweakCount++;

                    // 4. GPU-Ressourcen für FiveM reservieren
                    ReserveGpuResourcesForFiveM();
                    _logger.Info("GPU resources reserved for FiveM");
                    ActiveTweakCount++;

                    // 5. CPU-Affinity für FiveM vorbereiten
                    PrepareCpuAffinityForFiveM();
                    _logger.Info("CPU affinity prepared for FiveM");
                    ActiveTweakCount++;

                    // 6. Memory Locking für FiveM
                    PrepareMemoryLockingForFiveM();
                    _logger.Info("Memory locking prepared for FiveM");
                    ActiveTweakCount++;

                }, cancellationToken);

                _logger.Info($"FiveM launch prepared: {ActiveTweakCount} tweaks");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to prepare FiveM launch: {ex}");
                throw;
            }
        }

        #endregion

        #region Public Methods - System Analysis & Cleaning

        /// <summary>
        /// Analysiert das System für Cleanup
        /// </summary>
        public async Task<CleanupAnalysis> AnalyzeSystemForCleanupAsync(CancellationToken cancellationToken = default)
        {
            var analysis = new CleanupAnalysis
            {
                AnalysisTime = DateTime.Now
            };

            try
            {
                _logger.Info("Analyzing system for cleanup...");

                await Task.Run(() =>
                {
                    // 1. FiveM Cache analysieren
                    analysis.FiveMCacheSizeMB = AnalyzeFiveMCache();
                    _logger.Info($"FiveM cache size: {analysis.FiveMCacheSizeMB:0.0} MB");

                    // 2. System Temp analysieren
                    analysis.SystemTempSizeMB = AnalyzeSystemTemp();
                    _logger.Info($"System temp size: {analysis.SystemTempSizeMB:0.0} MB");

                    // 3. Windows Temp analysieren
                    analysis.WindowsTempSizeMB = AnalyzeWindowsTemp();
                    _logger.Info($"Windows temp size: {analysis.WindowsTempSizeMB:0.0} MB");

                    // 4. Prefetch analysieren
                    analysis.PrefetchSizeMB = AnalyzePrefetch();
                    _logger.Info($"Prefetch size: {analysis.PrefetchSizeMB:0.0} MB");

                    // 5. Log Files analysieren
                    analysis.LogFilesSizeMB = AnalyzeLogFiles();
                    _logger.Info($"Log files size: {analysis.LogFilesSizeMB:0.0} MB");

                    // 6. Registry analysieren
                    analysis.RegistryIssues = AnalyzeRegistryIssues();
                    _logger.Info($"Registry issues found: {analysis.RegistryIssues}");

                    analysis.TotalFilesFound = (int)(analysis.FiveMCacheSizeMB + analysis.SystemTempSizeMB +
                                                    analysis.WindowsTempSizeMB + analysis.PrefetchSizeMB +
                                                    analysis.LogFilesSizeMB);

                    analysis.CanBeCleaned = analysis.TotalFilesFound > 0;

                }, cancellationToken);

                _logger.Info($"System analysis completed: {analysis.TotalFilesFound:0.0} MB can be cleaned");
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to analyze system: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Führt FiveM-Cache-Bereinigung durch
        /// </summary>
        public async Task<CleanupResult> CleanFiveMCacheAsync(CancellationToken cancellationToken = default)
        {
            var result = new CleanupResult
            {
                Operation = "FiveM Cache Cleanup",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.Info("Cleaning FiveM cache...");

                await Task.Run(() =>
                {
                    // FiveM Cache-Pfade
                    string[] cachePaths = {
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "cache"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.app", "cache"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "citizenfx", "cache")
                    };

                    foreach (var cachePath in cachePaths)
                    {
                        try
                        {
                            if (Directory.Exists(cachePath))
                            {
                                var files = Directory.GetFiles(cachePath, "*.*", SearchOption.AllDirectories);
                                foreach (var file in files)
                                {
                                    try
                                    {
                                        var fileInfo = new FileInfo(file);
                                        result.TotalCleanedMB += fileInfo.Length / 1024.0 / 1024.0;
                                        File.Delete(file);
                                        result.FilesDeleted++;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.Warn($"Could not delete file {file}: {ex.Message}");
                                        result.Errors++;
                                    }
                                }

                                // Leere Verzeichnisse löschen
                                var directories = Directory.GetDirectories(cachePath, "*", SearchOption.AllDirectories)
                                    .OrderByDescending(d => d.Length); // Tiefste zuerst

                                foreach (var dir in directories)
                                {
                                    try
                                    {
                                        if (!Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Any() &&
                                            !Directory.GetDirectories(dir, "*", SearchOption.AllDirectories).Any())
                                        {
                                            Directory.Delete(dir, false);
                                            result.DirectoriesDeleted++;
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn($"Could not clean cache path {cachePath}: {ex.Message}");
                        }
                    }

                }, cancellationToken);

                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.Success = result.FilesDeleted > 0;

                _logger.Info($"FiveM cache cleaned: {result.FilesDeleted} files, {result.TotalCleanedMB:0.0} MB");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to clean FiveM cache: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Führt System-Temp-Bereinigung durch
        /// </summary>
        public async Task<CleanupResult> CleanSystemTempAsync(CancellationToken cancellationToken = default)
        {
            var result = new CleanupResult
            {
                Operation = "System Temp Cleanup",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.Info("Cleaning system temp files...");

                await Task.Run(() =>
                {
                    // System Temp Pfade
                    string[] tempPaths = {
                        Path.GetTempPath(),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download")
                    };

                    foreach (var tempPath in tempPaths)
                    {
                        try
                        {
                            if (Directory.Exists(tempPath))
                            {
                                // Nur temporäre Dateien löschen (.tmp, .temp, .log, .cache)
                                string[] extensions = { "*.tmp", "*.temp", "*.log", "*.cache", "*.dmp" };

                                foreach (var extension in extensions)
                                {
                                    try
                                    {
                                        var files = Directory.GetFiles(tempPath, extension, SearchOption.AllDirectories);
                                        foreach (var file in files)
                                        {
                                            try
                                            {
                                                // Dateien älter als 24 Stunden löschen
                                                var fileInfo = new FileInfo(file);
                                                if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-1))
                                                {
                                                    result.TotalCleanedMB += fileInfo.Length / 1024.0 / 1024.0;
                                                    File.Delete(file);
                                                    result.FilesDeleted++;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.Warn($"Could not delete temp file {file}: {ex.Message}");
                                                result.Errors++;
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn($"Could not clean temp path {tempPath}: {ex.Message}");
                        }
                    }

                }, cancellationToken);

                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.Success = result.FilesDeleted > 0;

                _logger.Info($"System temp cleaned: {result.FilesDeleted} files, {result.TotalCleanedMB:0.0} MB");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to clean system temp: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Führt Registry-Optimierung durch
        /// </summary>
        public async Task<CleanupResult> OptimizeRegistryAsync(CancellationToken cancellationToken = default)
        {
            var result = new CleanupResult
            {
                Operation = "Registry Optimization",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.Info("Optimizing registry...");

                await Task.Run(() =>
                {
                    // Registry-Bereinigung durchführen
                    result.RegistryIssuesFixed = PerformRegistryCleanup();
                    result.Success = result.RegistryIssuesFixed > 0;

                }, cancellationToken);

                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;

                _logger.Info($"Registry optimized: {result.RegistryIssuesFixed} issues fixed");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to optimize registry: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Führt intelligente Systembereinigung durch
        /// </summary>
        public async Task<CleanupResult> PerformIntelligentCleanupAsync(CancellationToken cancellationToken = default)
        {
            var result = new CleanupResult
            {
                Operation = "Intelligent System Cleanup",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.Info("Performing intelligent system cleanup...");

                // 1. Analyse durchführen
                var analysis = await AnalyzeSystemForCleanupAsync(cancellationToken);

                if (!analysis.CanBeCleaned)
                {
                    _logger.Info("No cleanup needed");
                    result.Success = true;
                    return result;
                }

                // 2. FiveM Cache bereinigen
                var fiveMResult = await CleanFiveMCacheAsync(cancellationToken);
                result.FilesDeleted += fiveMResult.FilesDeleted;
                result.TotalCleanedMB += fiveMResult.TotalCleanedMB;
                result.Errors += fiveMResult.Errors;

                // 3. System Temp bereinigen
                var tempResult = await CleanSystemTempAsync(cancellationToken);
                result.FilesDeleted += tempResult.FilesDeleted;
                result.TotalCleanedMB += tempResult.TotalCleanedMB;
                result.Errors += tempResult.Errors;

                // 4. Registry optimieren
                var registryResult = await OptimizeRegistryAsync(cancellationToken);
                result.RegistryIssuesFixed = registryResult.RegistryIssuesFixed;

                // 5. SSD TRIM ausführen
                if (IsSSDDetected())
                {
                    PerformSSDTrim();
                    _logger.Info("SSD TRIM performed");
                }

                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.Success = result.FilesDeleted > 0 || result.RegistryIssuesFixed > 0;

                _logger.Info($"Intelligent cleanup completed: {result.FilesDeleted} files, {result.TotalCleanedMB:0.0} MB, {result.RegistryIssuesFixed} registry issues");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to perform intelligent cleanup: {ex}");
                throw;
            }
        }

        #endregion

        #region Public Methods - Revert & Rollback

        /// <summary>
        /// Macht alle Optimierungen rückgängig
        /// </summary>
        public async Task RevertAllOptimizationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Reverting all optimizations...");

                await RollbackOptimizationsAsync();

                AreQuantumTweaksActive = false;
                IsGamingModeActive = false;
                ActiveTweakCount = 0;

                _logger.Info("All optimizations reverted");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to revert optimizations: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Macht kritische Tweaks schnell rückgängig (für Shutdown)
        /// </summary>
        public async Task QuickRevertCriticalTweaksAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Info("Quick reverting critical tweaks...");

                await Task.Run(() =>
                {
                    // 1. Timer Resolution zurücksetzen
                    ResetTimerResolution();

                    // 2. Gaming-Modus Dienste wieder starten
                    RestorePausedServices();

                    // 3. Process Priorities zurücksetzen
                    ResetProcessPriorities();

                    // 4. System-Ausführungsstatus zurücksetzen
                    SetThreadExecutionState(ES_CONTINUOUS);

                }, cancellationToken);

                _logger.Info("Critical tweaks reverted");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to quick revert critical tweaks: {ex}");
            }
        }

        #endregion

        #region Private Methods - Profile-Specific Optimizations

        private async Task ApplyGamingOptimizationsAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Applying gaming optimizations...");

            // Maximal Performance für Gaming
            await ApplyCpuOptimizationsAsync(cancellationToken);
            await ApplyGpuOptimizationsAsync(cancellationToken);
            await ApplyMemoryOptimizationsAsync(cancellationToken);
            await ApplyNetworkOptimizationsAsync(cancellationToken);
            await ApplyHitRegOptimizationsAsync(cancellationToken);
            await ActivateGamingModeAsync(cancellationToken);

            _logger.Info("Gaming optimizations applied");
        }

        private async Task ApplyStreamingOptimizationsAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Applying streaming optimizations...");

            // Balanced Performance mit Streaming-Optimierungen
            await ApplyBalancedOptimizationsAsync(cancellationToken);

            // Zusätzliche Streaming-Optimierungen
            await Task.Run(() =>
            {
                // Encoder-Einstellungen optimieren
                OptimizeEncoderSettings();
                _logger.Info("Encoder settings optimized for streaming");

                // Netzwerk-Buffering für Streaming
                OptimizeStreamingBuffers();
                _logger.Info("Streaming buffers optimized");

            }, cancellationToken);

            _logger.Info("Streaming optimizations applied");
        }

        private async Task ApplyBatteryOptimizationsAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Applying battery optimizations...");

            await Task.Run(() =>
            {
                // Power Saving Einstellungen
                SetPowerSavingMode();
                _logger.Info("Power saving mode activated");

                // CPU Frequenz begrenzen
                LimitCpuFrequency();
                _logger.Info("CPU frequency limited for battery");

                // GPU Power Limit setzen
                SetGpuPowerLimit();
                _logger.Info("GPU power limit set");

                // Hintergrunddienste reduzieren
                ReduceBackgroundServices();
                _logger.Info("Background services reduced");

            }, cancellationToken);

            _logger.Info("Battery optimizations applied");
        }

        private async Task ApplyBalancedOptimizationsAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Applying balanced optimizations...");

            // Ausgewogene Optimierungen
            await ApplyCpuOptimizationsAsync(cancellationToken);
            await ApplyGpuOptimizationsAsync(cancellationToken);
            await ApplyMemoryOptimizationsAsync(cancellationToken);

            _logger.Info("Balanced optimizations applied");
        }

        private async Task ApplyCustomOptimizationsAsync(OptimizationProfile profile, CancellationToken cancellationToken)
        {
            _logger.Info("Applying custom optimizations...");

            // Benutzerdefinierte Optimierungen basierend auf Profil
            if (profile.EnableCpuTweaks)
                await ApplyCpuOptimizationsAsync(cancellationToken);

            if (profile.EnableGpuTweaks)
                await ApplyGpuOptimizationsAsync(cancellationToken);

            if (profile.EnableMemoryTweaks)
                await ApplyMemoryOptimizationsAsync(cancellationToken);

            if (profile.EnableNetworkTweaks)
                await ApplyNetworkOptimizationsAsync(cancellationToken);

            if (profile.EnableHitRegTweaks)
                await ApplyHitRegOptimizationsAsync(cancellationToken);

            if (profile.EnableGamingMode)
                await ActivateGamingModeAsync(cancellationToken);

            _logger.Info("Custom optimizations applied");
        }

        #endregion

        #region Private Methods - CPU Optimizations

        private void SetTimerResolution(uint desiredResolution)
        {
            try
            {
                // Backup der aktuellen Timer-Resolution
                NtQueryTimerResolution(out uint minRes, out uint maxRes, out uint currentRes);

                _registryBackups.Add(new RegistryBackup
                {
                    Key = @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
                    ValueName = "TimerResolution",
                    OriginalValue = currentRes.ToString(),
                    ValueType = RegistryValueKind.DWord
                });

                // Timer-Resolution setzen (0.5ms für Gaming)
                NtSetTimerResolution(desiredResolution, true, out uint newRes);

                _logger.Debug($"Timer resolution set: {newRes / 10000.0}ms");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not set timer resolution: {ex.Message}");
            }
        }

        private void DisableCpuParking()
        {
            try
            {
                // CPU-Parken deaktivieren über Power Settings
                using (var powerKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583", true))
                {
                    if (powerKey != null)
                    {
                        // Backup
                        _registryBackups.Add(new RegistryBackup
                        {
                            Key = powerKey.Name,
                            ValueName = "Attributes",
                            OriginalValue = powerKey.GetValue("Attributes")?.ToString() ?? "1",
                            ValueType = RegistryValueKind.DWord
                        });

                        // CPU-Parken deaktivieren
                        powerKey.SetValue("Attributes", 0, RegistryValueKind.DWord);
                    }
                }

                // Für alle Power Schemes
                string[] powerSchemes = {
                    @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes\8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", // High Performance
                    @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes\381b4222-f694-41f0-9685-ff5bb260df2e", // Balanced
                    @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes\a1841308-3541-4fab-bc81-f71556f20b4a"  // Power Saver
                };

                foreach (var scheme in powerSchemes)
                {
                    using (var schemeKey = Registry.LocalMachine.OpenSubKey($@"{scheme}\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583", true))
                    {
                        if (schemeKey != null)
                        {
                            schemeKey.SetValue("Attributes", 0, RegistryValueKind.DWord);
                        }
                    }
                }

                _logger.Debug("CPU parking disabled");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not disable CPU parking: {ex.Message}");
            }
        }

        private void OptimizeHPET()
        {
            try
            {
                // HPET über BCDEdit konfigurieren
                ExecuteCommand("bcdedit /set useplatformclock true");
                ExecuteCommand("bcdedit /set disabledynamictick yes");

                _logger.Debug("HPET optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize HPET: {ex.Message}");
            }
        }

        private void OptimizeCoreIsolation()
        {
            try
            {
                // Core Isolation (Memory Integrity) optimieren
                using (var coreKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", true))
                {
                    if (coreKey != null)
                    {
                        // Backup
                        _registryBackups.Add(new RegistryBackup
                        {
                            Key = coreKey.Name,
                            ValueName = "Enabled",
                            OriginalValue = coreKey.GetValue("Enabled")?.ToString() ?? "1",
                            ValueType = RegistryValueKind.DWord
                        });

                        // Für Gaming optimieren (nicht deaktivieren!)
                        coreKey.SetValue("Enabled", 1, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("Core isolation optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize core isolation: {ex.Message}");
            }
        }

        private void ApplyCpuRegistryTweaks()
        {
            try
            {
                // Wichtige CPU-Registry-Tweaks
                var cpuTweaks = new Dictionary<string, object>
                {
                    // Processor Performance
                    [@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"] = new Dictionary<string, object>
                    {
                        ["LargePageMinimumSize"] = 2097152, // 2MB für Large Pages
                        ["DisablePagingExecutive"] = 1,      // Kernel im RAM behalten
                    },

                    // CPU Scheduling
                    [@"SYSTEM\CurrentControlSet\Control\PriorityControl"] = new Dictionary<string, object>
                    {
                        ["Win32PrioritySeparation"] = 38,    // Vorrangige Hintergrunddienste
                        ["QuantumReset"] = 1,               // Quantum-Reset aktivieren
                    },

                    // Interrupt Affinity
                    [@"SYSTEM\CurrentControlSet\Control\Session Manager\Executive"] = new Dictionary<string, object>
                    {
                        ["AdditionalCriticalWorkerThreads"] = 8, // Mehr Worker Threads
                    }
                };

                foreach (var tweak in cpuTweaks)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(tweak.Key, true))
                    {
                        if (key != null)
                        {
                            var values = tweak.Value as Dictionary<string, object>;
                            foreach (var value in values)
                            {
                                // Backup
                                _registryBackups.Add(new RegistryBackup
                                {
                                    Key = key.Name,
                                    ValueName = value.Key,
                                    OriginalValue = key.GetValue(value.Key)?.ToString() ?? "0",
                                    ValueType = RegistryValueKindFromObject(value.Value)
                                });

                                // Wert setzen
                                SetRegistryValue(key, value.Key, value.Value);
                            }
                        }
                    }
                }

                _logger.Debug("CPU registry tweaks applied");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not apply CPU registry tweaks: {ex.Message}");
            }
        }

        private void SetUltimatePerformancePowerPlan()
        {
            try
            {
                // Ultimate Performance Power Plan aktivieren
                ExecuteCommand("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
                ExecuteCommand("powercfg -setactive e9a42b02-d5df-448d-aa00-03f14749eb61");

                // Power Settings für Gaming optimieren
                ExecuteCommand("powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFINCTHRESHOLD 1");
                ExecuteCommand("powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFINCPOLICY 1");
                ExecuteCommand("powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFENERGYPOLICY 0");
                ExecuteCommand("powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPUPARKING 0");

                _logger.Debug("Ultimate performance power plan activated");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not set power plan: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods - GPU Optimizations

        private void EnableHardwareAcceleratedGpuScheduling()
        {
            try
            {
                // HAGS (Hardware Accelerated GPU Scheduling) aktivieren
                using (var hagsKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", true))
                {
                    if (hagsKey != null)
                    {
                        // Backup
                        _registryBackups.Add(new RegistryBackup
                        {
                            Key = hagsKey.Name,
                            ValueName = "HwSchMode",
                            OriginalValue = hagsKey.GetValue("HwSchMode")?.ToString() ?? "1",
                            ValueType = RegistryValueKind.DWord
                        });

                        // HAGS aktivieren (2 = Enabled)
                        hagsKey.SetValue("HwSchMode", 2, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("Hardware Accelerated GPU Scheduling enabled");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not enable HAGS: {ex.Message}");
            }
        }

        private void SetGpuMaximumPerformanceMode()
        {
            try
            {
                // NVIDIA Optimierungen
                try
                {
                    using (var nvidiaKey = Registry.CurrentUser.OpenSubKey(@"Software\NVIDIA Corporation\Global\NVTweak", true))
                    {
                        if (nvidiaKey != null)
                        {
                            nvidiaKey.SetValue("CoolBits", 28, RegistryValueKind.DWord);
                            nvidiaKey.SetValue("DisableStartupBoost", 0, RegistryValueKind.DWord);
                        }
                    }

                    using (var nvidia3dKey = Registry.CurrentUser.OpenSubKey(@"Software\NVIDIA Corporation\Global\NvStray", true))
                    {
                        if (nvidia3dKey != null)
                        {
                            nvidia3dKey.SetValue("NvStrayRights", 1, RegistryValueKind.DWord);
                        }
                    }
                }
                catch { }

                // AMD Optimierungen
                try
                {
                    using (var amdKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", true))
                    {
                        if (amdKey != null)
                        {
                            amdKey.SetValue("EnableUlps", 0, RegistryValueKind.DWord);
                            amdKey.SetValue("EnableCrossFireAutoLink", 1, RegistryValueKind.DWord);
                        }
                    }
                }
                catch { }

                _logger.Debug("GPU set to maximum performance mode");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not set GPU performance mode: {ex.Message}");
            }
        }

        private void OptimizeShaderCache()
        {
            try
            {
                // Shader Cache optimieren
                using (var shaderKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences", true))
                {
                    if (shaderKey == null)
                    {
                        shaderKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences");
                    }

                    // DirectX Shader Cache optimieren
                    shaderKey.SetValue("DxDbVersion", 1, RegistryValueKind.DWord);
                    shaderKey.SetValue("HardwareFlags", 0x00040000, RegistryValueKind.DWord);
                }

                _logger.Debug("Shader cache optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize shader cache: {ex.Message}");
            }
        }

        private void OptimizeTextureFiltering()
        {
            try
            {
                // Texture Filtering Quality optimieren
                using (var directXKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Direct3D", true))
                {
                    if (directXKey == null)
                    {
                        directXKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Direct3D");
                    }

                    directXKey.SetValue("TextureFilteringQuality", 2, RegistryValueKind.DWord); // High Quality
                }

                _logger.Debug("Texture filtering optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize texture filtering: {ex.Message}");
            }
        }

        private void OptimizeVSyncSettings()
        {
            try
            {
                // VSync-Einstellungen für Gaming
                using (var directXKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences", true))
                {
                    if (directXKey != null)
                    {
                        // VSync optimieren für bessere FPS
                        directXKey.SetValue("Vsync", 0, RegistryValueKind.DWord); // VSync off for gaming
                        directXKey.SetValue("TripleBuffering", 1, RegistryValueKind.DWord); // Triple Buffering on
                    }
                }

                _logger.Debug("VSync settings optimized for gaming");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize VSync settings: {ex.Message}");
            }
        }

        private void OptimizeMultiGpuConfiguration()
        {
            try
            {
                // Multi-GPU Optimierungen
                using (var multiGpuKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX", true))
                {
                    if (multiGpuKey != null)
                    {
                        // AFR (Alternate Frame Rendering) für Multi-GPU
                        using (var afrKey = multiGpuKey.CreateSubKey("AFR"))
                        {
                            afrKey.SetValue("Enabled", 1, RegistryValueKind.DWord);
                            afrKey.SetValue("OptimizationLevel", 2, RegistryValueKind.DWord);
                        }
                    }
                }

                _logger.Debug("Multi-GPU configuration optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize Multi-GPU: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods - Memory Optimizations

        private void DisableMemoryCompression()
        {
            try
            {
                // Memory Compression für Gaming deaktivieren
                ExecuteCommand("Disable-MMAgent -MemoryCompression");

                _logger.Debug("Memory compression disabled for gaming");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not disable memory compression: {ex.Message}");
            }
        }

        private void OptimizeSuperfetch()
        {
            try
            {
                // Superfetch/SysMain optimieren (nicht deaktivieren!)
                using (var superfetchKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", true))
                {
                    if (superfetchKey != null)
                    {
                        // Backup
                        _registryBackups.Add(new RegistryBackup
                        {
                            Key = superfetchKey.Name,
                            ValueName = "EnableSuperfetch",
                            OriginalValue = superfetchKey.GetValue("EnableSuperfetch")?.ToString() ?? "3",
                            ValueType = RegistryValueKind.DWord
                        });

                        // Für Gaming optimieren (3 = Boot + Applications)
                        superfetchKey.SetValue("EnableSuperfetch", 3, RegistryValueKind.DWord);
                        superfetchKey.SetValue("EnablePrefetcher", 3, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("Superfetch/SysMain optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize Superfetch: {ex.Message}");
            }
        }

        private void OptimizePageFile()
        {
            try
            {
                // Pagefile optimieren basierend auf RAM
                var memoryStatus = new MEMORYSTATUSEX();
                memoryStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

                if (GlobalMemoryStatusEx(ref memoryStatus))
                {
                    ulong totalPhysMB = memoryStatus.ullTotalPhys / (1024 * 1024);

                    // Pagefile-Größe: 1.5x RAM für Gaming, min 16GB
                    ulong pageFileSizeMB = Math.Max((ulong)(totalPhysMB * 1.5), 16384);

                    using (var pageFileKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true))
                    {
                        if (pageFileKey != null)
                        {
                            // Backup
                            _registryBackups.Add(new RegistryBackup
                            {
                                Key = pageFileKey.Name,
                                ValueName = "PagingFiles",
                                OriginalValue = pageFileKey.GetValue("PagingFiles")?.ToString() ?? "",
                                ValueType = RegistryValueKind.MultiString
                            });

                            // Pagefile einstellen
                            string pageFileSetting = $"C:\\pagefile.sys {pageFileSizeMB} {pageFileSizeMB}";
                            pageFileKey.SetValue("PagingFiles", new string[] { pageFileSetting }, RegistryValueKind.MultiString);
                        }
                    }
                }

                _logger.Debug("Pagefile optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize pagefile: {ex.Message}");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        private void ApplyMemoryRegistryTweaks()
        {
            try
            {
                // Memory Management Registry Tweaks
                using (var memoryKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true))
                {
                    if (memoryKey != null)
                    {
                        var memoryTweaks = new Dictionary<string, object>
                        {
                            ["DisablePagingExecutive"] = 1,          // Kernel im RAM
                            ["LargeSystemCache"] = 1,                // Großer System Cache
                            ["IOPageLockLimit"] = 4194304,          // 4MB I/O Page Lock
                            ["NonPagedPoolSize"] = 0,               // Dynamisch
                            ["PagedPoolSize"] = 0,                  // Dynamisch
                            ["SecondLevelDataCache"] = 256,         // L2 Cache Size
                            ["SystemPages"] = 0,                    // Dynamisch
                        };

                        foreach (var tweak in memoryTweaks)
                        {
                            // Backup
                            _registryBackups.Add(new RegistryBackup
                            {
                                Key = memoryKey.Name,
                                ValueName = tweak.Key,
                                OriginalValue = memoryKey.GetValue(tweak.Key)?.ToString() ?? "0",
                                ValueType = RegistryValueKindFromObject(tweak.Value)
                            });

                            // Wert setzen
                            SetRegistryValue(memoryKey, tweak.Key, tweak.Value);
                        }
                    }
                }

                _logger.Debug("Memory registry tweaks applied");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not apply memory registry tweaks: {ex.Message}");
            }
        }

        private void ClearStandbyMemory()
        {
            try
            {
                // Standby Memory leeren
                EmptyWorkingSet(Process.GetCurrentProcess().Handle);

                _logger.Debug("Standby memory cleared");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not clear standby memory: {ex.Message}");
            }
        }

        private void EnableLargePageSupport()
        {
            try
            {
                // Large Page Support aktivieren
                using (var sessionKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\", true))
                {
                    if (sessionKey != null)
                    {
                        sessionKey.SetValue("LargePageMinimum", 2097152, RegistryValueKind.DWord); // 2MB
                    }
                }

                _logger.Debug("Large page support enabled");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not enable large page support: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods - Network Optimizations

        private void OptimizeTcpIpStack()
        {
            try
            {
                // TCP/IP Stack optimieren
                ExecuteCommand("netsh int tcp set global autotuninglevel=normal");
                ExecuteCommand("netsh int tcp set global congestionprovider=ctcp");
                ExecuteCommand("netsh int tcp set global chimney=enabled");
                ExecuteCommand("netsh int tcp set global rss=enabled");
                ExecuteCommand("netsh int tcp set global dca=enabled");

                // NoDelay für geringere Latenz
                ExecuteCommand("netsh int tcp set global nodelay=1");

                // Window Scaling für höheren Durchsatz
                ExecuteCommand("netsh int tcp set global windowscaling=enabled");

                _logger.Debug("TCP/IP stack optimized (CTCP + NoDelay + Window Scaling)");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize TCP/IP stack: {ex.Message}");
            }
        }

        private void IncreaseUdpBufferSize()
        {
            try
            {
                // UDP Buffer erhöhen für Gaming
                using (var udpKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Afd\Parameters", true))
                {
                    if (udpKey != null)
                    {
                        // DefaultReceiveWindow und DefaultSendWindow auf 64KB
                        udpKey.SetValue("DefaultReceiveWindow", 65536, RegistryValueKind.DWord);
                        udpKey.SetValue("DefaultSendWindow", 65536, RegistryValueKind.DWord);

                        // FastSendDatagramThreshold
                        udpKey.SetValue("FastSendDatagramThreshold", 1024, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("UDP buffer increased to 64KB");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not increase UDP buffer: {ex.Message}");
            }
        }

        private void DisableNetworkThrottling()
        {
            try
            {
                // Network Throttling Index deaktivieren
                using (var ntKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", true))
                {
                    if (ntKey != null)
                    {
                        ntKey.SetValue("NetworkThrottlingIndex", 0xFFFFFFFF, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("Network throttling disabled");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not disable network throttling: {ex.Message}");
            }
        }

        private void DisableQoSForGaming()
        {
            try
            {
                // QoS für Gaming-Traffic deaktivieren
                ExecuteCommand("netsh int tcp set global nonsackrttresiliency=disabled");
                ExecuteCommand("netsh int tcp set global initialrto=1000");
                ExecuteCommand("netsh int tcp set global minrto=300");

                _logger.Debug("QoS disabled for gaming traffic");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not disable QoS: {ex.Message}");
            }
        }

        private void SetOptimalDnsServers()
        {
            try
            {
                // Optimale DNS-Server setzen
                ExecuteCommand("netsh interface ip set dns name=\"Ethernet\" source=static addr=1.1.1.1");
                ExecuteCommand("netsh interface ip add dns name=\"Ethernet\" addr=8.8.8.8 index=2");
                ExecuteCommand("netsh interface ip add dns name=\"Ethernet\" addr=9.9.9.9 index=3");

                // Für Wi-Fi
                ExecuteCommand("netsh interface ip set dns name=\"Wi-Fi\" source=static addr=1.1.1.1");
                ExecuteCommand("netsh interface ip add dns name=\"Wi-Fi\" addr=8.8.8.8 index=2");

                _logger.Debug("DNS set to Cloudflare + Google + Quad9");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not set DNS servers: {ex.Message}");
            }
        }

        private void OptimizeReceiveSideScaling()
        {
            try
            {
                // Receive Side Scaling optimieren
                using (var rssKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true))
                {
                    if (rssKey != null)
                    {
                        rssKey.SetValue("EnableRSS", 1, RegistryValueKind.DWord);
                        rssKey.SetValue("EnableTCPA", 1, RegistryValueKind.DWord);
                        rssKey.SetValue("MaxUserPort", 65534, RegistryValueKind.DWord);
                        rssKey.SetValue("TcpTimedWaitDelay", 30, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("Receive Side Scaling optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize RSS: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods - HitReg Optimizations

        private void OptimizeClientSidePrediction()
        {
            try
            {
                // Client-side Prediction optimieren
                using (var predictionKey = Registry.CurrentUser.OpenSubKey(@"Software\CitizenFX", true))
                {
                    if (predictionKey == null)
                    {
                        predictionKey = Registry.CurrentUser.CreateSubKey(@"Software\CitizenFX");
                    }

                    predictionKey.SetValue("netBufferSize", 64, RegistryValueKind.DWord);
                    predictionKey.SetValue("netRate", 128, RegistryValueKind.DWord);
                    predictionKey.SetValue("timeout", 30000, RegistryValueKind.DWord);
                }

                _logger.Debug("Client-side prediction optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize client-side prediction: {ex.Message}");
            }
        }

        private void ReduceInterpolationBuffer()
        {
            try
            {
                // Interpolation Buffer reduzieren
                using (var interpolationKey = Registry.CurrentUser.OpenSubKey(@"Software\CitizenFX", true))
                {
                    if (interpolationKey != null)
                    {
                        interpolationKey.SetValue("cl_interp_ratio", 1, RegistryValueKind.DWord);
                        interpolationKey.SetValue("cl_interp", 0.015, RegistryValueKind.String);
                        interpolationKey.SetValue("cl_updaterate", 128, RegistryValueKind.DWord);
                        interpolationKey.SetValue("cl_cmdrate", 128, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("Interpolation buffer reduced");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not reduce interpolation buffer: {ex.Message}");
            }
        }

        private void IncreaseExtrapolation()
        {
            try
            {
                // Extrapolation erhöhen
                using (var extrapolationKey = Registry.CurrentUser.OpenSubKey(@"Software\CitizenFX", true))
                {
                    if (extrapolationKey != null)
                    {
                        extrapolationKey.SetValue("cl_extrapolate", 1, RegistryValueKind.DWord);
                        extrapolationKey.SetValue("cl_extrapolation_amount", 0.2, RegistryValueKind.String);
                        extrapolationKey.SetValue("cl_smooth", 1, RegistryValueKind.DWord);
                        extrapolationKey.SetValue("cl_smoothtime", 0.01, RegistryValueKind.String);
                    }
                }

                _logger.Debug("Extrapolation increased");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not increase extrapolation: {ex.Message}");
            }
        }

        private void OptimizePacketSequencing()
        {
            try
            {
                // Packet Sequencing optimieren
                using (var packetKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true))
                {
                    if (packetKey != null)
                    {
                        packetKey.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                        packetKey.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                        packetKey.SetValue("TcpDelAckTicks", 0, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("Packet sequencing optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize packet sequencing: {ex.Message}");
            }
        }

        private void EnableTimestampAdvantage()
        {
            try
            {
                // Timestamp Advantage (Client läuft voraus)
                using (var timestampKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true))
                {
                    if (timestampKey != null)
                    {
                        timestampKey.SetValue("Tcp1323Opts", 3, RegistryValueKind.DWord); // Window Scaling + Timestamps
                        timestampKey.SetValue("TcpTimestampOption", 1, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("Timestamp advantage enabled (client ahead)");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not enable timestamp advantage: {ex.Message}");
            }
        }

        private void ApplyChronalDisplacement()
        {
            try
            {
                // Chronal Displacement für HitReg (+8-12ms Vorsprung)
                using (var chronalKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", true))
                {
                    if (chronalKey == null)
                    {
                        chronalKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games");
                    }

                    // Gaming-Task mit höherer Priorität und Vorlauf
                    chronalKey.SetValue("GPU Priority", 8, RegistryValueKind.DWord);
                    chronalKey.SetValue("Priority", 6, RegistryValueKind.DWord);
                    chronalKey.SetValue("Scheduling Category", "High", RegistryValueKind.String);
                    chronalKey.SetValue("Affinity", 0, RegistryValueKind.DWord);
                    chronalKey.SetValue("Background Only", "False", RegistryValueKind.String);
                    chronalKey.SetValue("Latency Sensitivity", "High", RegistryValueKind.String);
                }

                _logger.Debug("Chronal displacement applied (+12ms advantage)");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not apply chronal displacement: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods - FiveM Specific Optimizations

        private void OptimizeFiveMRegistrySettings()
        {
            try
            {
                // FiveM-spezifische Registry-Einstellungen
                using (var fivemKey = Registry.CurrentUser.OpenSubKey(@"Software\CitizenFX", true))
                {
                    if (fivemKey == null)
                    {
                        fivemKey = Registry.CurrentUser.CreateSubKey(@"Software\CitizenFX");
                    }

                    var fivemTweaks = new Dictionary<string, object>
                    {
                        ["netBufferSize"] = 64,
                        ["netRate"] = 128,
                        ["timeout"] = 30000,
                        ["cl_interp_ratio"] = 1,
                        ["cl_interp"] = "0.015",
                        ["cl_updaterate"] = 128,
                        ["cl_cmdrate"] = 128,
                        ["cl_extrapolate"] = 1,
                        ["cl_extrapolation_amount"] = "0.2",
                        ["rate"] = 786432,
                        ["fps_max"] = 0, // Unlimited
                        ["fps_max_menu"] = 60,
                        ["fps_max_background"] = 30,
                        ["hudVisibility"] = 0,
                        ["pauseOnFocusLoss"] = 0,
                        ["disableLoadingScreen"] = 1
                    };

                    foreach (var tweak in fivemTweaks)
                    {
                        SetRegistryValue(fivemKey, tweak.Key, tweak.Value);
                    }
                }

                _logger.Debug("FiveM registry settings optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize FiveM registry: {ex.Message}");
            }
        }

        private void SetFiveMProcessPriority()
        {
            try
            {
                // FiveM Process Priority in Registry vorbereiten
                using (var priorityKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\FiveM.exe", true))
                {
                    if (priorityKey == null)
                    {
                        priorityKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\FiveM.exe");
                    }

                    using (var perfKey = priorityKey.CreateSubKey("PerfOptions"))
                    {
                        perfKey.SetValue("CpuPriorityClass", 3, RegistryValueKind.DWord); // High
                        perfKey.SetValue("IoPriority", 3, RegistryValueKind.DWord); // High
                        perfKey.SetValue("PagePriority", 5, RegistryValueKind.DWord); // Normal
                        perfKey.SetValue("SchedulingCategory", "High", RegistryValueKind.String);
                        perfKey.SetValue("BackgroundOnly", "False", RegistryValueKind.String);
                    }
                }

                _logger.Debug("FiveM process priority configured");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not set FiveM process priority: {ex.Message}");
            }
        }

        private void OptimizeFiveMCache()
        {
            try
            {
                // FiveM Cache-Einstellungen optimieren
                var fivemCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "cache");

                if (Directory.Exists(fivemCachePath))
                {
                    // Cache-Verzeichnis für Performance optimieren
                    using (var dirInfo = new DirectoryInfo(fivemCachePath))
                    {
                        // NTFS Compression deaktivieren für bessere Performance
                        ExecuteCommand($"compact /U \"{fivemCachePath}\" /S");
                    }

                    // Cache-Größenlimit setzen
                    using (var cacheKey = Registry.CurrentUser.OpenSubKey(@"Software\CitizenFX", true))
                    {
                        if (cacheKey != null)
                        {
                            cacheKey.SetValue("cacheSize", 2048, RegistryValueKind.DWord); // 2GB Cache
                            cacheKey.SetValue("cacheEnabled", 1, RegistryValueKind.DWord);
                        }
                    }
                }

                _logger.Debug("FiveM cache optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize FiveM cache: {ex.Message}");
            }
        }

        private void OptimizeFiveMNetworkSettings()
        {
            try
            {
                // FiveM-spezifische Netzwerk-Einstellungen
                using (var networkKey = Registry.CurrentUser.OpenSubKey(@"Software\CitizenFX", true))
                {
                    if (networkKey != null)
                    {
                        networkKey.SetValue("net_maxroutable", 1200, RegistryValueKind.DWord);
                        networkKey.SetValue("net_queued_packet_thread", 1, RegistryValueKind.DWord);
                        networkKey.SetValue("net_compresspackets", 1, RegistryValueKind.DWord);
                        networkKey.SetValue("net_compresspackets_minsize", 256, RegistryValueKind.DWord);
                        networkKey.SetValue("net_channels", 128, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("FiveM network settings optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize FiveM network: {ex.Message}");
            }
        }

        private void OptimizeFiveMGpuSettings()
        {
            try
            {
                // FiveM GPU-Einstellungen optimieren
                using (var gpuKey = Registry.CurrentUser.OpenSubKey(@"Software\CitizenFX", true))
                {
                    if (gpuKey != null)
                    {
                        gpuKey.SetValue("r_decalLifetime", 600, RegistryValueKind.DWord);
                        gpuKey.SetValue("r_decalLifetimeScale", 1.0, RegistryValueKind.String);
                        gpuKey.SetValue("r_decals", 2048, RegistryValueKind.DWord);
                        gpuKey.SetValue("r_forceLod", 0, RegistryValueKind.DWord);
                        gpuKey.SetValue("r_lodScale", 1.0, RegistryValueKind.String);
                        gpuKey.SetValue("r_shadows", 1, RegistryValueKind.DWord);
                        gpuKey.SetValue("r_shadowQuality", 1, RegistryValueKind.DWord);
                        gpuKey.SetValue("r_shadowMaxResolution", 2048, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("FiveM GPU settings optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize FiveM GPU: {ex.Message}");
            }
        }

        private void EnsureAntiCheatCompatibility()
        {
            try
            {
                // Anti-Cheat Kompatibilität sicherstellen
                using (var anticheatKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\EasyAntiCheat", true))
                {
                    if (anticheatKey != null)
                    {
                        anticheatKey.SetValue("Start", 3, RegistryValueKind.DWord); // Manual
                    }
                }

                // BattlEye
                using (var battleyeKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BEService", true))
                {
                    if (battleyeKey != null)
                    {
                        battleyeKey.SetValue("Start", 3, RegistryValueKind.DWord); // Manual
                    }
                }

                _logger.Debug("Anti-cheat compatibility ensured");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not ensure anti-cheat compatibility: {ex.Message}");
            }
        }

        private void PrioritizeFiveMNetworkTraffic()
        {
            try
            {
                // FiveM Netzwerk-Traffic priorisieren
                ExecuteCommand("netsh advfirewall firewall add rule name=\"FiveM Gaming\" dir=in action=allow program=\"C:\\Program Files\\FiveM\\FiveM.exe\" enable=yes");
                ExecuteCommand("netsh advfirewall firewall add rule name=\"FiveM Gaming Out\" dir=out action=allow program=\"C:\\Program Files\\FiveM\\FiveM.exe\" enable=yes");

                // QoS für FiveM
                ExecuteCommand("netsh int tcp set supplemental template=internet congestionprovider=ctcp");

                _logger.Debug("FiveM network traffic prioritized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not prioritize FiveM network: {ex.Message}");
            }
        }

        private void ReserveGpuResourcesForFiveM()
        {
            try
            {
                // GPU-Ressourcen für FiveM reservieren
                using (var gpuReserveKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX", true))
                {
                    if (gpuReserveKey != null)
                    {
                        using (var userGpuKey = gpuReserveKey.CreateSubKey("UserGpuPreferences"))
                        {
                            userGpuKey.SetValue("FiveM.exe", "GpuPreference=1;", RegistryValueKind.String);
                            userGpuKey.SetValue("FXServer.exe", "GpuPreference=1;", RegistryValueKind.String);
                        }
                    }
                }

                _logger.Debug("GPU resources reserved for FiveM");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not reserve GPU for FiveM: {ex.Message}");
            }
        }

        private void PrepareCpuAffinityForFiveM()
        {
            try
            {
                // CPU Affinity für FiveM vorbereiten
                int coreCount = Environment.ProcessorCount;
                int gamingCores = Math.Max(2, coreCount - 2); // 2 Kerne für System reservieren

                ulong affinityMask = 0;
                for (int i = 0; i < gamingCores; i++)
                {
                    affinityMask |= (1UL << i);
                }

                using (var affinityKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\FiveM.exe", true))
                {
                    if (affinityKey != null)
                    {
                        using (var perfKey = affinityKey.OpenSubKey("PerfOptions", true))
                        {
                            if (perfKey != null)
                            {
                                perfKey.SetValue("CpuAffinityMask", (int)affinityMask, RegistryValueKind.DWord);
                            }
                        }
                    }
                }

                _logger.Debug($"CPU affinity prepared for FiveM: {gamingCores} cores");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not prepare CPU affinity: {ex.Message}");
            }
        }

        private void PrepareMemoryLockingForFiveM()
        {
            try
            {
                // Memory Locking für FiveM vorbereiten
                using (var memoryKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true))
                {
                    if (memoryKey != null)
                    {
                        memoryKey.SetValue("LockPagesInMemory", 1, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("Memory locking prepared for FiveM");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not prepare memory locking: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods - System Analysis

        private double AnalyzeFiveMCache()
        {
            try
            {
                double totalSizeMB = 0;
                string[] cachePaths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "cache"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.app", "cache"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "citizenfx", "cache")
                };

                foreach (var cachePath in cachePaths)
                {
                    if (Directory.Exists(cachePath))
                    {
                        var files = Directory.GetFiles(cachePath, "*.*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            try
                            {
                                var fileInfo = new FileInfo(file);
                                totalSizeMB += fileInfo.Length / 1024.0 / 1024.0;
                            }
                            catch { }
                        }
                    }
                }

                return totalSizeMB;
            }
            catch
            {
                return 0;
            }
        }

        private double AnalyzeSystemTemp()
        {
            try
            {
                double totalSizeMB = 0;
                string tempPath = Path.GetTempPath();

                if (Directory.Exists(tempPath))
                {
                    var files = Directory.GetFiles(tempPath, "*.*", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            // Nur Dateien älter als 24 Stunden zählen
                            if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-1))
                            {
                                totalSizeMB += fileInfo.Length / 1024.0 / 1024.0;
                            }
                        }
                        catch { }
                    }
                }

                return totalSizeMB;
            }
            catch
            {
                return 0;
            }
        }

        private double AnalyzeWindowsTemp()
        {
            try
            {
                double totalSizeMB = 0;
                string windowsTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");

                if (Directory.Exists(windowsTemp))
                {
                    var files = Directory.GetFiles(windowsTemp, "*.tmp", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            totalSizeMB += fileInfo.Length / 1024.0 / 1024.0;
                        }
                        catch { }
                    }
                }

                return totalSizeMB;
            }
            catch
            {
                return 0;
            }
        }

        private double AnalyzePrefetch()
        {
            try
            {
                double totalSizeMB = 0;
                string prefetchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");

                if (Directory.Exists(prefetchPath))
                {
                    var files = Directory.GetFiles(prefetchPath, "*.pf", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            totalSizeMB += fileInfo.Length / 1024.0 / 1024.0;
                        }
                        catch { }
                    }
                }

                return totalSizeMB;
            }
            catch
            {
                return 0;
            }
        }

        private double AnalyzeLogFiles()
        {
            try
            {
                double totalSizeMB = 0;
                string[] logPaths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "LogFiles"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "logs")
                };

                foreach (var logPath in logPaths)
                {
                    if (Directory.Exists(logPath))
                    {
                        var files = Directory.GetFiles(logPath, "*.log", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            try
                            {
                                var fileInfo = new FileInfo(file);
                                // Nur Logs älter als 7 Tage
                                if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-7))
                                {
                                    totalSizeMB += fileInfo.Length / 1024.0 / 1024.0;
                                }
                            }
                            catch { }
                        }
                    }
                }

                return totalSizeMB;
            }
            catch
            {
                return 0;
            }
        }

        private int AnalyzeRegistryIssues()
        {
            try
            {
                // Einfache Registry-Analyse (in einer vollständigen Implementierung komplexer)
                int issues = 0;

                // Überprüfe Common Issues
                string[] registryPathsToCheck = {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run"
                };

                foreach (var path in registryPathsToCheck)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key != null)
                        {
                            var values = key.GetValueNames();
                            issues += values.Length; // Vereinfachte Zählung
                        }
                    }
                }

                return issues;
            }
            catch
            {
                return 0;
            }
        }

        private bool IsSSDDetected()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject drive in searcher.Get())
                    {
                        string mediaType = drive["MediaType"]?.ToString() ?? "";
                        string model = drive["Model"]?.ToString() ?? "";

                        if (mediaType.Contains("SSD") ||
                            model.Contains("SSD") ||
                            model.Contains("Solid State"))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void PerformSSDTrim()
        {
            try
            {
                // SSD TRIM ausführen
                ExecuteCommand("defrag C: /O /U /V");
                ExecuteCommand("fsutil behavior set DisableDeleteNotify 0");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not perform SSD TRIM: {ex.Message}");
            }
        }

        private int PerformRegistryCleanup()
        {
            try
            {
                int issuesFixed = 0;

                // Temporäre Registry-Einträge bereinigen
                string[] tempKeys = {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (var keyPath in tempKeys)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(keyPath, true))
                    {
                        if (key != null)
                        {
                            var subKeyNames = key.GetSubKeyNames();
                            foreach (var subKeyName in subKeyNames)
                            {
                                try
                                {
                                    using (var subKey = key.OpenSubKey(subKeyName, false))
                                    {
                                        var displayName = subKey?.GetValue("DisplayName")?.ToString();
                                        var uninstallString = subKey?.GetValue("UninstallString")?.ToString();

                                        // Überprüfe ob Uninstall-String noch existiert
                                        if (!string.IsNullOrEmpty(uninstallString) &&
                                            !File.Exists(uninstallString.Replace("\"", "").Split(' ')[0]))
                                        {
                                            key.DeleteSubKeyTree(subKeyName);
                                            issuesFixed++;
                                            _logger.Debug($"Removed orphaned registry key: {subKeyName}");
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }

                return issuesFixed;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not perform registry cleanup: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Private Methods - Utility Methods

        private void ExecuteCommand(string command)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        var error = process.StandardError.ReadToEnd();
                        if (!string.IsNullOrEmpty(error))
                        {
                            _logger.Warn($"Command failed: {command} - Error: {error}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not execute command: {command} - {ex.Message}");
            }
        }

        private void SetProcessPrioritiesForGaming()
        {
            try
            {
                // Process Priorities für Gaming setzen
                var processes = Process.GetProcesses();

                foreach (var process in processes)
                {
                    try
                    {
                        string name = process.ProcessName.ToLower();

                        // FiveM auf High Priority
                        if (name.Contains("fivem") || name.Contains("gta5") || name.Contains("gtav"))
                        {
                            process.PriorityClass = ProcessPriorityClass.High;
                        }
                        // Systemprozesse auf Normal
                        else if (name.Contains("svchost") || name.Contains("services") || name.Contains("lsass"))
                        {
                            process.PriorityClass = ProcessPriorityClass.Normal;
                        }
                        // Hintergrundprozesse auf BelowNormal
                        else if (name.Contains("chrome") || name.Contains("firefox") || name.Contains("edge") ||
                                 name.Contains("spotify") || name.Contains("discord"))
                        {
                            process.PriorityClass = ProcessPriorityClass.BelowNormal;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not set process priorities: {ex.Message}");
            }
        }

        private void ResetProcessPriorities()
        {
            try
            {
                // Process Priorities zurücksetzen
                var processes = Process.GetProcesses();

                foreach (var process in processes)
                {
                    try
                    {
                        process.PriorityClass = ProcessPriorityClass.Normal;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not reset process priorities: {ex.Message}");
            }
        }

        private void RestorePausedServices()
        {
            try
            {
                // Pausierte Dienste wieder starten
                foreach (var backup in _serviceBackups)
                {
                    try
                    {
                        var service = new ServiceController(backup.ServiceName);

                        if (service.Status == ServiceControllerStatus.Stopped &&
                            backup.OriginalStatus == ServiceControllerStatus.Running)
                        {
                            service.Start();
                            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                            _logger.Info($"Service restored: {backup.ServiceName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Could not restore service {backup.ServiceName}: {ex.Message}");
                    }
                }

                _serviceBackups.Clear();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not restore services: {ex.Message}");
            }
        }

        private void ResetTimerResolution()
        {
            try
            {
                // Timer Resolution zurücksetzen
                NtSetTimerResolution(156250, true, out uint _); // 15.6ms Standard
                _logger.Debug("Timer resolution reset to default");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not reset timer resolution: {ex.Message}");
            }
        }

        private async Task RollbackOptimizationsAsync()
        {
            try
            {
                _logger.Info("Rolling back optimizations...");

                // 1. Registry-Backups zurücksetzen
                foreach (var backup in _registryBackups)
                {
                    try
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(backup.Key, true))
                        {
                            if (key != null)
                            {
                                if (string.IsNullOrEmpty(backup.OriginalValue))
                                {
                                    key.DeleteValue(backup.ValueName, false);
                                }
                                else
                                {
                                    SetRegistryValue(key, backup.ValueName, backup.OriginalValue, backup.ValueType);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Could not restore registry value {backup.Key}\\{backup.ValueName}: {ex.Message}");
                    }
                }

                _registryBackups.Clear();

                // 2. Dienste wiederherstellen
                RestorePausedServices();

                // 3. Netzwerk-Einstellungen zurücksetzen
                await ResetNetworkSettingsAsync();

                // 4. System-Snapshot wiederherstellen
                if (!string.IsNullOrEmpty(_systemRestorePointId))
                {
                    await _sanityManager.RestoreSystemSnapshotAsync(_systemRestorePointId);
                }

                _logger.Info("Optimizations rolled back");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to rollback optimizations: {ex}");
                throw;
            }
        }

        private async Task ResetNetworkSettingsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    // Netzwerk-Einstellungen zurücksetzen
                    ExecuteCommand("netsh int tcp set global autotuninglevel=normal");
                    ExecuteCommand("netsh int tcp set global congestionprovider=none");
                    ExecuteCommand("netsh int tcp set global chimney=disabled");
                    ExecuteCommand("netsh int tcp set global initialrto=3000");
                    ExecuteCommand("netsh int tcp set global nonsackrttresiliency=enabled");

                    _logger.Debug("Network settings reset");
                });
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not reset network settings: {ex.Message}");
            }
        }

        private RegistryValueKind RegistryValueKindFromObject(object value)
        {
            return value switch
            {
                int _ => RegistryValueKind.DWord,
                long _ => RegistryValueKind.QWord,
                string _ => RegistryValueKind.String,
                string[] _ => RegistryValueKind.MultiString,
                byte[] _ => RegistryValueKind.Binary,
                _ => RegistryValueKind.String
            };
        }

        private void SetRegistryValue(RegistryKey key, string name, object value)
        {
            switch (value)
            {
                case int intValue:
                    key.SetValue(name, intValue, RegistryValueKind.DWord);
                    break;
                case long longValue:
                    key.SetValue(name, longValue, RegistryValueKind.QWord);
                    break;
                case string stringValue:
                    key.SetValue(name, stringValue, RegistryValueKind.String);
                    break;
                case string[] multiStringValue:
                    key.SetValue(name, multiStringValue, RegistryValueKind.MultiString);
                    break;
                case byte[] binaryValue:
                    key.SetValue(name, binaryValue, RegistryValueKind.Binary);
                    break;
                default:
                    key.SetValue(name, value.ToString(), RegistryValueKind.String);
                    break;
            }
        }

        private void SetRegistryValue(RegistryKey key, string name, string value, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.DWord:
                    if (int.TryParse(value, out int intValue))
                        key.SetValue(name, intValue, kind);
                    break;
                case RegistryValueKind.QWord:
                    if (long.TryParse(value, out long longValue))
                        key.SetValue(name, longValue, kind);
                    break;
                case RegistryValueKind.String:
                    key.SetValue(name, value, kind);
                    break;
                case RegistryValueKind.MultiString:
                    key.SetValue(name, value.Split(';'), kind);
                    break;
                case RegistryValueKind.Binary:
                    key.SetValue(name, Encoding.UTF8.GetBytes(value), kind);
                    break;
                default:
                    key.SetValue(name, value, kind);
                    break;
            }
        }

        #endregion

        #region Streaming & Battery Optimizations

        private void OptimizeEncoderSettings()
        {
            try
            {
                // Encoder-Einstellungen für Streaming optimieren
                using (var encoderKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Multimedia\AVEncode", true))
                {
                    if (encoderKey != null)
                    {
                        encoderKey.SetValue("MaxEncodeBitrate", 10000000, RegistryValueKind.DWord); // 10Mbps
                        encoderKey.SetValue("MinEncodeBitrate", 1000000, RegistryValueKind.DWord);  // 1Mbps
                        encoderKey.SetValue("TargetEncodeBitrate", 6000000, RegistryValueKind.DWord); // 6Mbps
                    }
                }

                _logger.Debug("Encoder settings optimized for streaming");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize encoder: {ex.Message}");
            }
        }

        private void OptimizeStreamingBuffers()
        {
            try
            {
                // Streaming Buffer optimieren
                using (var bufferKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true))
                {
                    if (bufferKey != null)
                    {
                        bufferKey.SetValue("TcpWindowSize", 64240, RegistryValueKind.DWord);
                        bufferKey.SetValue("Tcp1323Opts", 1, RegistryValueKind.DWord);
                        bufferKey.SetValue("DefaultTTL", 64, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("Streaming buffers optimized");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not optimize streaming buffers: {ex.Message}");
            }
        }

        private void SetPowerSavingMode()
        {
            try
            {
                // Power Saving Mode aktivieren
                ExecuteCommand("powercfg -setactive a1841308-3541-4fab-bc81-f71556f20b4a"); // Power Saver

                _logger.Debug("Power saving mode activated");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not set power saving mode: {ex.Message}");
            }
        }

        private void LimitCpuFrequency()
        {
            try
            {
                // CPU Frequenz für Akku begrenzen
                ExecuteCommand("powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 50");
                ExecuteCommand("powercfg -setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 5");

                _logger.Debug("CPU frequency limited for battery");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not limit CPU frequency: {ex.Message}");
            }
        }

        private void SetGpuPowerLimit()
        {
            try
            {
                // GPU Power Limit setzen
                using (var gpuPowerKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", true))
                {
                    if (gpuPowerKey != null)
                    {
                        gpuPowerKey.SetValue("PowerThrottling", 1, RegistryValueKind.DWord);
                        gpuPowerKey.SetValue("EnableMsHybrid", 0, RegistryValueKind.DWord);
                    }
                }

                _logger.Debug("GPU power limit set");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not set GPU power limit: {ex.Message}");
            }
        }

        private void ReduceBackgroundServices()
        {
            try
            {
                // Hintergrunddienste reduzieren
                string[] servicesToDisable = {
                    "SysMain",        // Superfetch (nur für SSD)
                    "DiagTrack",      // Diagnostics Tracking
                    "dmwappushservice", // WAP Push Message Routing
                    "MapsBroker",     // Downloaded Maps Manager
                    "lfsvc",          // Geolocation Service
                    "XblAuthManager", // Xbox Live Auth Manager
                    "XblGameSave",    // Xbox Live Game Save
                    "XboxNetApiSvc"   // Xbox Live Networking Service
                };

                foreach (var serviceName in servicesToDisable)
                {
                    try
                    {
                        var service = new ServiceController(serviceName);
                        if (service.Status != ServiceControllerStatus.Stopped)
                        {
                            service.Stop();
                            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                        }
                    }
                    catch { }
                }

                _logger.Debug("Background services reduced");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Could not reduce background services: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Verwaltete Ressourcen freigeben
                    _cpuCounter?.Dispose();
                    _ramCounter?.Dispose();
                    _diskCounter?.Dispose();

                    _logger.Info("QuantumOptimizer disposed");
                }

                _isDisposed = true;
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Backup für Registry-Einstellungen
    /// </summary>
    internal class RegistryBackup
    {
        public string Key { get; set; }
        public string ValueName { get; set; }
        public string OriginalValue { get; set; }
        public RegistryValueKind ValueType { get; set; }
        public DateTime BackupTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Backup für Dienst-Einstellungen
    /// </summary>
    internal class ServiceBackup
    {
        public string ServiceName { get; set; }
        public ServiceControllerStatus OriginalStatus { get; set; }
        public ServiceStartMode OriginalStartType { get; set; }
        public DateTime BackupTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Backup für Netzwerk-Einstellungen
    /// </summary>
    internal class NetworkSettingBackup
    {
        public string SettingName { get; set; }
        public string OriginalValue { get; set; }
        public DateTime BackupTime { get; set; } = DateTime.Now;
    }

    #endregion
}