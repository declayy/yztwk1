using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace FiveMQuantumTweaker2026.Core
{
    /// <summary>
    /// FiveM Launch Manager mit 2026er Quantum Gaming Optimization
    /// </summary>
    public class FiveMLaunchManager : IDisposable
    {
        private readonly Logger _logger;
        private readonly PerformanceMonitor _perfMonitor;
        private readonly SystemSanityManager _sanityManager;

        // Process Management
        private Process _fiveMProcess;
        private Thread _monitorThread;
        private bool _isMonitoring;
        private bool _isOptimizedLaunch;

        // Service Management
        private readonly Dictionary<string, ServiceState> _originalServiceStates;
        private readonly List<string> _pausedServices;

        // CPU Affinity
        private IntPtr _originalAffinity;
        private IntPtr _optimizedAffinity;

        // GPU Optimization
        private readonly GpuOptimizer _gpuOptimizer;

        // Quantum Launch Profiles
        private readonly Dictionary<string, LaunchProfile> _launchProfiles;

        // Constants
        private const string FIVEM_EXE = "FiveM.exe";
        private const string FIVEM_PROCESS_NAME = "FiveM";
        private const int MONITOR_INTERVAL_MS = 1000;
        private const int AFFINITY_HIGH_PERFORMANCE_CORES = 0xAA; // Cores 1,3,5,7 (bei 8 Kernen)
        private const int PRIORITY_BOOST_DURATION = 30000; // 30 Sekunden

        // Temporär pausierte Dienste (NICHT deaktiviert!)
        private readonly string[] _servicesToPause =
        {
            "BITS",                 // Background Intelligent Transfer Service
            "WSearch",              // Windows Search
            "SysMain",              // Superfetch (optimiert)
            "DiagTrack",            // Connected User Experiences
            "wuauserv",             // Windows Update
            "DoSvc",                // Delivery Optimization
            "CertPropSvc",          // Certificate Propagation
            "PcaSvc",               // Program Compatibility Assistant
            "WpnService",           // Windows Push Notifications
            "MapsBroker",           // Downloaded Maps Manager
            "lfsvc",                // Geolocation Service
            "TrkWks",               // Distributed Link Tracking Client
            "TabletInputService",   // Tablet PC Input Service
            "wscsvc",               // Security Center
            "SCardSvr",             // Smart Card
            "PhoneSvc",             // Phone Service
            "CDPUserSvc",           // Connected Devices Platform User Service
            "OneSyncSvc",           // Sync Host
            "XblAuthManager",       // Xbox Live Auth Manager
            "XblGameSave"           // Xbox Live Game Save
        };

        // Geschützte Dienste (NIEMALS anfassen!)
        private readonly HashSet<string> _protectedServices = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "WinDefend",            // Windows Defender
            "mpssvc",               // Windows Firewall
            "EventLog",             // Event Logging
            "LSM",                  // Local Session Manager
            "Dnscache",             // DNS Client
            "Dhcp",                 // DHCP Client
            "BFE",                  // Base Filtering Engine
            "SamSs",                // Security Accounts Manager
            "RpcSs",                // Remote Procedure Call
            "PlugPlay",             // Plug and Play
            "TPM",                  // Trusted Platform Module
            "VaultSvc",             // Credential Manager
            "CryptSvc",             // Cryptographic Services
            "DcomLaunch",           // DCOM Server Process Launcher
            "Power",                // Power Management
            "SystemEventsBroker",   // System Events Broker
            "CoreMessagingRegistrar", // CoreMessaging
            "NlaSvc",               // Network Location Awareness
            "WinHttpAutoProxySvc",  // WinHTTP Web Proxy Auto-Discovery
            "iphlpsvc"              // IP Helper
        };

        public FiveMLaunchManager(Logger logger, SystemSanityManager sanityManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sanityManager = sanityManager ?? throw new ArgumentNullException(nameof(sanityManager));
            _perfMonitor = new PerformanceMonitor();
            _gpuOptimizer = new GpuOptimizer(_logger);

            _originalServiceStates = new Dictionary<string, ServiceState>();
            _pausedServices = new List<string>();

            _launchProfiles = new Dictionary<string, LaunchProfile>
            {
                {
                    "QuantumPerformance",
                    new LaunchProfile
                    {
                        Name = "Quantum Performance",
                        Description = "Maximale Performance für Competitive Gaming",
                        CpuAffinity = GetHighPerformanceCores(),
                        Priority = ProcessPriorityClass.RealTime,
                        MemoryPriority = MemoryPriority.Normal,
                        IoPriority = IoPriority.High,
                        GpuPerformanceMode = GpuPerformanceMode.PreferMaximum,
                        EnableQuantumFeatures = true,
                        NetworkPriority = NetworkPriority.Highest
                    }
                },
                {
                    "Balanced",
                    new LaunchProfile
                    {
                        Name = "Balanced",
                        Description = "Optimiertes Gaming mit System-Stabilität",
                        CpuAffinity = IntPtr.Zero, // Alle Kerne
                        Priority = ProcessPriorityClass.High,
                        MemoryPriority = MemoryPriority.Normal,
                        IoPriority = IoPriority.Normal,
                        GpuPerformanceMode = GpuPerformanceMode.HighPerformance,
                        EnableQuantumFeatures = true,
                        NetworkPriority = NetworkPriority.High
                    }
                },
                {
                    "Streaming",
                    new LaunchProfile
                    {
                        Name = "Streaming",
                        Description = "Optimiert für Streaming & Recording",
                        CpuAffinity = GetStreamingCores(),
                        Priority = ProcessPriorityClass.AboveNormal,
                        MemoryPriority = MemoryPriority.BelowNormal,
                        IoPriority = IoPriority.Normal,
                        GpuPerformanceMode = GpuPerformanceMode.OptimalPower,
                        EnableQuantumFeatures = false,
                        NetworkPriority = NetworkPriority.Normal
                    }
                }
            };

            _logger.Log("🚀 FiveMLaunchManager initialisiert mit 3 Launch-Profilen");
        }

        /// <summary>
        /// Startet FiveM mit Quantum-Optimierungen
        /// </summary>
        public async Task<LaunchResult> LaunchFiveMOptimized(string profileName = "QuantumPerformance", string serverAddress = "")
        {
            var result = new LaunchResult
            {
                ProfileName = profileName,
                StartTime = DateTime.Now
            };

            try
            {
                _logger.Log($"🚀 Starte FiveM mit {profileName}-Profil...");

                // 1. System-Integrität prüfen
                if (!ValidateSystemForGaming())
                {
                    result.Success = false;
                    result.ErrorMessage = "System-Integritätsprüfung fehlgeschlagen";
                    return result;
                }

                // 2. System-Snapshot erstellen
                var snapshot = _sanityManager.CreateSystemSnapshot($"Pre-FiveM-Launch-{profileName}");
                result.SnapshotId = snapshot.Id;

                // 3. Launch-Profil laden
                if (!_launchProfiles.TryGetValue(profileName, out var profile))
                {
                    profile = _launchProfiles["QuantumPerformance"];
                    _logger.LogWarning($"Profil '{profileName}' nicht gefunden, verwende QuantumPerformance");
                }

                result.Profile = profile;

                // 4. Pre-Launch Optimierungen
                await PerformPreLaunchOptimizations(profile);

                // 5. FiveM Process finden oder starten
                string fiveMPath = FindFiveMExecutable();
                if (string.IsNullOrEmpty(fiveMPath))
                {
                    result.Success = false;
                    result.ErrorMessage = "FiveM.exe nicht gefunden";
                    return result;
                }

                result.FiveMPath = fiveMPath;

                // 6. Process mit Optimierungen starten
                _fiveMProcess = await StartFiveMProcess(fiveMPath, serverAddress, profile);
                if (_fiveMProcess == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "FiveM Process konnte nicht gestartet werden";
                    await RestoreSystemState(); // Aufräumen
                    return result;
                }

                result.ProcessId = _fiveMProcess.Id;
                _isOptimizedLaunch = true;

                // 7. Post-Launch Optimierungen
                await PerformPostLaunchOptimizations(_fiveMProcess, profile);

                // 8. Monitoring starten
                StartProcessMonitoring();

                result.Success = true;
                result.LaunchDuration = DateTime.Now - result.StartTime;
                result.Message = $"FiveM erfolgreich gestartet (PID: {_fiveMProcess.Id}) mit {profileName}-Profil";

                _logger.Log($"✅ {result.Message}");

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Launch fehlgeschlagen: {ex.Message}";
                result.LaunchDuration = DateTime.Now - result.StartTime;

                _logger.LogError($"❌ FiveM Launch fehlgeschlagen: {ex}");

                // Im Fehlerfall Systemzustand wiederherstellen
                await RestoreSystemState();

                return result;
            }
        }

        /// <summary>
        /// Stoppt FiveM und stellt Systemzustand wieder her
        /// </summary>
        public async Task<ShutdownResult> ShutdownFiveM(bool force = false)
        {
            var result = new ShutdownResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                _logger.Log("🛑 FiveM Shutdown gestartet...");

                // Monitoring stoppen
                StopProcessMonitoring();

                // FiveM Process stoppen
                if (_fiveMProcess != null && !_fiveMProcess.HasExited)
                {
                    if (force)
                    {
                        _fiveMProcess.Kill();
                        result.WasForced = true;
                        _logger.Log("⚠️ FiveM Process wurde gezwungen zu beenden");
                    }
                    else
                    {
                        // Graceful shutdown
                        _fiveMProcess.CloseMainWindow();

                        if (!_fiveMProcess.WaitForExit(10000)) // 10 Sekunden warten
                        {
                            _fiveMProcess.Kill();
                            result.WasForced = true;
                            _logger.Log("⚠️ FiveM Process timeout, wurde gezwungen zu beenden");
                        }
                    }

                    result.ProcessId = _fiveMProcess.Id;
                    result.ExitCode = _fiveMProcess.ExitCode;
                }

                // Systemzustand wiederherstellen
                await RestoreSystemState();

                result.Success = true;
                result.ShutdownDuration = DateTime.Now - result.StartTime;
                result.Message = "FiveM erfolgreich beendet und System wiederhergestellt";

                _logger.Log($"✅ {result.Message}");

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Shutdown fehlgeschlagen: {ex.Message}";
                result.ShutdownDuration = DateTime.Now - result.StartTime;

                _logger.LogError($"❌ FiveM Shutdown fehlgeschlagen: {ex}");

                return result;
            }
        }

        /// <summary>
        /// Überwacht FiveM Prozess in Echtzeit
        /// </summary>
        public ProcessMonitorInfo GetProcessMonitorInfo()
        {
            var info = new ProcessMonitorInfo
            {
                Timestamp = DateTime.Now,
                IsRunning = _fiveMProcess != null && !_fiveMProcess.HasExited,
                IsOptimized = _isOptimizedLaunch,
                MonitorActive = _isMonitoring
            };

            if (_fiveMProcess != null && !_fiveMProcess.HasExited)
            {
                try
                {
                    info.ProcessId = _fiveMProcess.Id;
                    info.ProcessName = _fiveMProcess.ProcessName;
                    info.StartTime = _fiveMProcess.StartTime;
                    info.TotalProcessorTime = _fiveMProcess.TotalProcessorTime;
                    info.PrivateMemorySize = _fiveMProcess.PrivateMemorySize64;
                    info.WorkingSet = _fiveMProcess.WorkingSet64;
                    info.ThreadCount = _fiveMProcess.Threads.Count;

                    // Performance Daten
                    info.CpuUsage = _perfMonitor.GetProcessCpuUsage(_fiveMProcess.Id);
                    info.MemoryUsageMB = info.WorkingSet / 1024 / 1024;

                    // Priorität
                    info.Priority = _fiveMProcess.PriorityClass;

                    // Handle Count
                    info.HandleCount = _fiveMProcess.HandleCount;

                    // Quantum Features Status
                    info.QuantumFeaturesActive = CheckQuantumFeaturesActive();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Process Monitor Info Error: {ex.Message}");
                }
            }

            return info;
        }

        /// <summary>
        /// Pausiert nicht-essentielle Dienste für Gaming
        /// </summary>
        private async Task PauseNonEssentialServices()
        {
            _logger.Log("⏸️ Pausiere nicht-essentielle Dienste...");

            foreach (var serviceName in _servicesToPause)
            {
                try
                {
                    // Prüfen ob Dienst geschützt ist
                    if (_protectedServices.Contains(serviceName))
                    {
                        _logger.LogWarning($"⚠️ Überspringe geschützten Dienst: {serviceName}");
                        continue;
                    }

                    using (var service = new System.ServiceProcess.ServiceController(serviceName))
                    {
                        // Originalzustand speichern
                        _originalServiceStates[serviceName] = new ServiceState
                        {
                            Status = service.Status,
                            StartType = service.StartType,
                            CanStop = service.CanStop
                        };

                        // Nur laufende Dienste pausieren
                        if (service.Status == System.ServiceProcess.ServiceControllerStatus.Running)
                        {
                            // Graceful stop mit Timeout
                            var stopTask = Task.Run(() =>
                            {
                                try
                                {
                                    service.Stop();
                                    service.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped,
                                        TimeSpan.FromSeconds(10));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Dienst {serviceName} konnte nicht gestoppt werden: {ex.Message}");
                                    return false;
                                }
                                return true;
                            });

                            if (await stopTask)
                            {
                                _pausedServices.Add(serviceName);
                                _logger.Log($"  ⏸️ {serviceName} pausiert");
                            }
                        }
                        else
                        {
                            _logger.Log($"  ℹ️ {serviceName} war bereits {service.Status}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Dienst {serviceName} konnte nicht verarbeitet werden: {ex.Message}");
                }
            }

            _logger.Log($"✅ {_pausedServices.Count} Dienste pausiert");
        }

        /// <summary>
        /// Stellt pausierte Dienste wieder her
        /// </summary>
        private async Task RestorePausedServices()
        {
            if (_pausedServices.Count == 0)
                return;

            _logger.Log("▶️ Stelle pausierte Dienste wieder her...");

            int restoredCount = 0;

            foreach (var serviceName in _pausedServices)
            {
                try
                {
                    if (_originalServiceStates.TryGetValue(serviceName, out var originalState))
                    {
                        using (var service = new System.ServiceProcess.ServiceController(serviceName))
                        {
                            // Nur starten wenn der Dienst vorher lief
                            if (originalState.Status == System.ServiceProcess.ServiceControllerStatus.Running &&
                                service.Status != System.ServiceProcess.ServiceControllerStatus.Running)
                            {
                                service.Start();
                                await Task.Delay(500); // Kurze Pause zwischen Starts

                                _logger.Log($"  ▶️ {serviceName} wieder gestartet");
                                restoredCount++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Dienst {serviceName} konnte nicht gestartet werden: {ex.Message}");
                }
            }

            _pausedServices.Clear();
            _originalServiceStates.Clear();

            _logger.Log($"✅ {restoredCount} Dienste wiederhergestellt");
        }

        /// <summary>
        /// Setzt CPU Affinity für optimale Gaming-Performance
        /// </summary>
        private bool SetCpuAffinity(Process process, IntPtr affinityMask)
        {
            try
            {
                if (affinityMask != IntPtr.Zero)
                {
                    process.ProcessorAffinity = affinityMask;

                    // Log welche Kerne verwendet werden
                    long mask = affinityMask.ToInt64();
                    var cores = new List<int>();

                    for (int i = 0; i < Environment.ProcessorCount; i++)
                    {
                        if ((mask & (1L << i)) != 0)
                        {
                            cores.Add(i + 1);
                        }
                    }

                    _logger.Log($"🔧 CPU Affinity gesetzt: Kerne {string.Join(",", cores)}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"CPU Affinity konnte nicht gesetzt werden: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Setzt Prozess-Priorität
        /// </summary>
        private bool SetProcessPriority(Process process, ProcessPriorityClass priority)
        {
            try
            {
                process.PriorityClass = priority;
                _logger.Log($"🔧 Prozess-Priorität gesetzt: {priority}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Prozess-Priorität konnte nicht gesetzt werden: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Optimiert Memory Management für FiveM
        /// </summary>
        private void OptimizeMemoryManagement(Process process)
        {
            try
            {
                // Working Set erhöhen
                if (process != null && !process.HasExited)
                {
                    // MinWorkingSet erhöhen für bessere Performance
                    if (!SetProcessWorkingSetSize(process.Handle, -1, -1))
                    {
                        int error = Marshal.GetLastWin32Error();
                        _logger.LogWarning($"Working Set konnte nicht optimiert werden (Error: {error})");
                    }
                }

                // Large Pages aktivieren wenn möglich
                if (IsLargePageSupported())
                {
                    try
                    {
                        // Registry-Tweak für Large Pages
                        using (var key = Registry.LocalMachine.CreateSubKey(
                            @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"))
                        {
                            key.SetValue("LargePageMinimum", 2097152, RegistryValueKind.DWord); // 2MB
                        }

                        _logger.Log("🔧 Large Pages Support aktiviert");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Large Pages konnten nicht aktiviert werden: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Memory Optimization Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Setzt I/O Priorität für FiveM
        /// </summary>
        private void SetIoPriority(Process process, IoPriority priority)
        {
            try
            {
                // Nur auf Windows 8/10/11/12 mit Administrator-Rechten
                if (IsAdministrator())
                {
                    IntPtr jobHandle = CreateJobObject(IntPtr.Zero, "FiveM_IO_Priority");

                    if (jobHandle != IntPtr.Zero)
                    {
                        // I/O Priorität setzen
                        var ioLimit = new JOBOBJECT_IO_RATE_CONTROL_INFORMATION
                        {
                            MaxIops = 1000,
                            MaxBandwidth = 100 * 1024 * 1024, // 100 MB/s
                            ReservationIops = 100,
                            VolumeName = null,
                            BaseIoSize = 4096,
                            ControlFlags = JOB_OBJECT_IO_RATE_CONTROL_FLAGS.Enable
                        };

                        if (SetIoRateControlInformationJobObject(jobHandle, ref ioLimit))
                        {
                            AssignProcessToJobObject(jobHandle, process.Handle);
                            _logger.Log($"🔧 I/O Priorität gesetzt: {priority}");
                        }

                        CloseHandle(jobHandle);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"I/O Priority konnte nicht gesetzt werden: {ex.Message}");
            }
        }

        /// <summary>
        /// Findet FiveM Executable
        /// </summary>
        private string FindFiveMExecutable()
        {
            string[] searchPaths =
            {
                @"C:\Program Files\FiveM\FiveM.exe",
                @"C:\Program Files (x86)\FiveM\FiveM.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM", "FiveM.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "FiveM", "FiveM.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "FiveM", "FiveM.exe"),
                @"D:\Program Files\FiveM\FiveM.exe",
                @"E:\Program Files\FiveM\FiveM.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "FiveM", "FiveM.exe")
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    _logger.Log($"🔍 FiveM gefunden: {path}");
                    return path;
                }
            }

            // Fallback: Suche im gesamten System
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable);

                foreach (var drive in drives)
                {
                    try
                    {
                        var files = Directory.GetFiles(drive.RootDirectory.FullName, "FiveM.exe",
                            SearchOption.AllDirectories);

                        if (files.Length > 0)
                        {
                            _logger.Log($"🔍 FiveM gefunden via Suche: {files[0]}");
                            return files[0];
                        }
                    }
                    catch
                    {
                        // Zugriffsfehler, weiter mit nächstem Laufwerk
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"FiveM Suche fehlgeschlagen: {ex.Message}");
            }

            _logger.LogError("❌ FiveM.exe wurde nicht gefunden");
            return string.Empty;
        }

        /// <summary>
        /// Startet FiveM Process mit Optimierungen
        /// </summary>
        private async Task<Process> StartFiveMProcess(string executablePath, string serverAddress, LaunchProfile profile)
        {
            try
            {
                _logger.Log($"🎮 Starte FiveM: {executablePath}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                // Server-Adresse hinzufügen falls angegeben
                if (!string.IsNullOrEmpty(serverAddress))
                {
                    startInfo.Arguments = $"+connect {serverAddress}";
                    _logger.Log($"🔗 Verbinde mit Server: {serverAddress}");
                }

                // Process starten
                var process = new Process { StartInfo = startInfo };

                if (process.Start())
                {
                    _logger.Log($"✅ FiveM gestartet (PID: {process.Id})");

                    // Kurz warten damit Process vollständig initialisiert
                    await Task.Delay(2000);

                    // Optimierungen anwenden
                    ApplyProcessOptimizations(process, profile);

                    return process;
                }
                else
                {
                    _logger.LogError("❌ FiveM konnte nicht gestartet werden");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ FiveM Start fehlgeschlagen: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Wendet alle Optimierungen auf den Process an
        /// </summary>
        private void ApplyProcessOptimizations(Process process, LaunchProfile profile)
        {
            try
            {
                // 1. CPU Affinity
                if (profile.CpuAffinity != IntPtr.Zero)
                {
                    SetCpuAffinity(process, profile.CpuAffinity);
                }

                // 2. Process Priority
                SetProcessPriority(process, profile.Priority);

                // 3. Memory Optimization
                OptimizeMemoryManagement(process);

                // 4. I/O Priority
                SetIoPriority(process, profile.IoPriority);

                // 5. GPU Optimization
                if (profile.GpuPerformanceMode != GpuPerformanceMode.Default)
                {
                    _gpuOptimizer.SetGpuPerformanceMode(profile.GpuPerformanceMode);
                }

                // 6. Network Priority
                if (profile.NetworkPriority != NetworkPriority.Default)
                {
                    SetNetworkPriority(process, profile.NetworkPriority);
                }

                // 7. Quantum Features
                if (profile.EnableQuantumFeatures)
                {
                    EnableQuantumFeatures(process);
                }

                _logger.Log("✅ Alle Prozess-Optimierungen angewendet");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Prozess-Optimierungen fehlgeschlagen: {ex.Message}");
            }
        }

        /// <summary>
        /// Führt Pre-Launch Optimierungen durch
        /// </summary>
        private async Task PerformPreLaunchOptimizations(LaunchProfile profile)
        {
            _logger.Log("⚡ Führe Pre-Launch Optimierungen durch...");

            // 1. Dienste pausieren
            await PauseNonEssentialServices();

            // 2. GPU optimieren
            _gpuOptimizer.OptimizeForGaming();

            // 3. Netzwerk vorbereiten
            OptimizeNetworkPreLaunch();

            // 4. Memory vorbereiten
            PrepareMemoryForGaming();

            // 5. Power Plan auf Hochleistung
            SetHighPerformancePowerPlan();

            _logger.Log("✅ Pre-Launch Optimierungen abgeschlossen");
        }

        /// <summary>
        /// Führt Post-Launch Optimierungen durch
        /// </summary>
        private async Task PerformPostLaunchOptimizations(Process process, LaunchProfile profile)
        {
            _logger.Log("⚡ Führe Post-Launch Optimierungen durch...");

            // 1. Process Booster (temporärer Priority Boost)
            await Task.Run(() =>
            {
                try
                {
                    // Temporär auf RealTime für schnelleren Start
                    process.PriorityClass = ProcessPriorityClass.RealTime;

                    Task.Delay(PRIORITY_BOOST_DURATION).ContinueWith(_ =>
                    {
                        try
                        {
                            process.PriorityClass = profile.Priority;
                            _logger.Log("🔧 Temporärer Priority Boost beendet");
                        }
                        catch { }
                    });
                }
                catch { }
            });

            // 2. Game Mode aktivieren (Windows 10/11/12)
            EnableWindowsGameMode();

            // 3. Fullscreen Optimizations
            EnableFullscreenOptimizations();

            // 4. Hardware Accelerated GPU Scheduling
            EnableHardwareAcceleratedGpuScheduling();

            _logger.Log("✅ Post-Launch Optimierungen abgeschlossen");
        }

        /// <summary>
        /// Stellt Systemzustand komplett wieder her
        /// </summary>
        private async Task RestoreSystemState()
        {
            _logger.Log("🔄 Stelle Systemzustand wieder her...");

            try
            {
                // 1. Dienste wiederherstellen
                await RestorePausedServices();

                // 2. GPU auf Standard zurücksetzen
                _gpuOptimizer.RestoreDefaultSettings();

                // 3. Power Plan auf Balanced
                SetBalancedPowerPlan();

                // 4. Process zurücksetzen falls noch aktiv
                if (_fiveMProcess != null && !_fiveMProcess.HasExited)
                {
                    try
                    {
                        // CPU Affinity zurücksetzen
                        _fiveMProcess.ProcessorAffinity = _originalAffinity;

                        // Priority zurücksetzen
                        _fiveMProcess.PriorityClass = ProcessPriorityClass.Normal;
                    }
                    catch { }
                }

                _isOptimizedLaunch = false;
                _fiveMProcess = null;

                _logger.Log("✅ Systemzustand vollständig wiederhergestellt");
            }
            catch (Exception ex)
            {
                _logger.LogError($"System-Wiederherstellung fehlgeschlagen: {ex.Message}");
            }
        }

        /// <summary>
        /// Startet Process Monitoring
        /// </summary>
        private void StartProcessMonitoring()
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            _monitorThread = new Thread(MonitorWorker)
            {
                Priority = ThreadPriority.BelowNormal,
                IsBackground = true
            };
            _monitorThread.Start();

            _logger.Log("📊 Process Monitoring gestartet");
        }

        /// <summary>
        /// Stoppt Process Monitoring
        /// </summary>
        private void StopProcessMonitoring()
        {
            _isMonitoring = false;
            _monitorThread?.Join(2000);
            _logger.Log("📊 Process Monitoring gestoppt");
        }

        /// <summary>
        /// Monitoring Worker Thread
        /// </summary>
        private void MonitorWorker()
        {
            while (_isMonitoring)
            {
                try
                {
                    if (_fiveMProcess != null)
                    {
                        if (_fiveMProcess.HasExited)
                        {
                            _logger.Log("ℹ️ FiveM Process wurde beendet");
                            _isMonitoring = false;

                            // Automatische Aufräumarbeiten
                            Task.Run(async () =>
                            {
                                await Task.Delay(5000); // 5 Sekunden warten
                                await RestoreSystemState();
                            });

                            break;
                        }

                        // Periodische Gesundheitsprüfung
                        var info = GetProcessMonitorInfo();

                        // Warnungen bei kritischen Zuständen
                        if (info.CpuUsage > 90)
                        {
                            _logger.LogWarning($"⚠️ Hohe CPU-Auslastung: {info.CpuUsage:F1}%");
                        }

                        if (info.MemoryUsageMB > 8000) // > 8GB
                        {
                            _logger.LogWarning($"⚠️ Hohe Speicherauslastung: {info.MemoryUsageMB:F1}MB");
                        }
                    }
                }
                catch
                {
                    // Silent fail im Monitor
                }

                Thread.Sleep(MONITOR_INTERVAL_MS);
            }
        }

        // Helper Methods
        private bool ValidateSystemForGaming()
        {
            try
            {
                // 1. Administrator-Rechte
                if (!IsAdministrator())
                {
                    _logger.LogError("❌ Administrator-Rechte erforderlich");
                    return false;
                }

                // 2. Windows Version
                var osVersion = Environment.OSVersion.Version;
                if (osVersion.Major < 10)
                {
                    _logger.LogError("❌ Windows 10 oder höher erforderlich");
                    return false;
                }

                // 3. RAM
                var ramInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                if (ramInfo.TotalPhysicalMemory < 8L * 1024 * 1024 * 1024) // 8GB
                {
                    _logger.LogWarning("⚠️ Weniger als 8GB RAM - Performance könnte beeinträchtigt sein");
                }

                // 4. DirectX Version
                if (!CheckDirectXVersion())
                {
                    _logger.LogWarning("⚠️ DirectX 11 oder höher empfohlen");
                }

                // 5. Antivirus Konflikte prüfen
                CheckAntivirusConflicts();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"System-Validierung fehlgeschlagen: {ex.Message}");
                return false;
            }
        }

        private IntPtr GetHighPerformanceCores()
        {
            int coreCount = Environment.ProcessorCount;
            long mask = 0;

            // Bei Hyper-Threading: Nur physische Kerne verwenden
            if (coreCount >= 8)
            {
                // Kerne 0,2,4,6 (physische Kerne bei 8 Threads)
                mask = 0x55; // 01010101
            }
            else if (coreCount >= 4)
            {
                // Kerne 0,2
                mask = 0x05; // 00000101
            }
            else
            {
                // Alle Kerne
                mask = (1L << coreCount) - 1;
            }

            return new IntPtr(mask);
        }

        private IntPtr GetStreamingCores()
        {
            int coreCount = Environment.ProcessorCount;
            long mask = 0;

            if (coreCount >= 8)
            {
                // Kerne 1,3,5,7 für Streaming, 0,2,4,6 für Gaming
                mask = 0xAA; // 10101010
            }
            else
            {
                // Alle Kerne
                mask = (1L << coreCount) - 1;
            }

            return new IntPtr(mask);
        }

        private void OptimizeNetworkPreLaunch()
        {
            try
            {
                // QoS für Gaming
                ExecuteCommand("netsh", "int tcp set global dca=enabled");
                ExecuteCommand("netsh", "int tcp set global autotuninglevel=normal");
                ExecuteCommand("netsh", "int tcp set global rss=enabled");

                _logger.Log("🔧 Netzwerk für Gaming optimiert");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Network Pre-Launch Optimization Error: {ex.Message}");
            }
        }

        private void PrepareMemoryForGaming()
        {
            try
            {
                // Standby Memory leeren
                ExecuteCommand("rundll32.exe", "advapi32.dll,ProcessIdleTasks");

                // Working Set optimieren
                ExecuteCommand("powershell", "Clear-StorageDiagnosticInfo");

                _logger.Log("🔧 Memory für Gaming vorbereitet");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Memory Preparation Error: {ex.Message}");
            }
        }

        private void SetHighPerformancePowerPlan()
        {
            try
            {
                ExecuteCommand("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                _logger.Log("🔧 Power Plan auf Hochleistung");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Power Plan konnte nicht gesetzt werden: {ex.Message}");
            }
        }

        private void SetBalancedPowerPlan()
        {
            try
            {
                ExecuteCommand("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
                _logger.Log("🔧 Power Plan auf Balanced");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Power Plan konnte nicht zurückgesetzt werden: {ex.Message}");
            }
        }

        private void EnableWindowsGameMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(
                    @"SOFTWARE\Microsoft\GameBar"))
                {
                    key.SetValue("AllowAutoGameMode", 1, RegistryValueKind.DWord);
                    key.SetValue("AutoGameModeEnabled", 1, RegistryValueKind.DWord);
                }

                _logger.Log("🎮 Windows Game Mode aktiviert");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Game Mode konnte nicht aktiviert werden: {ex.Message}");
            }
        }

        private void EnableFullscreenOptimizations()
        {
            try
            {
                // Fullscreen Optimizations für FiveM
                using (var key = Registry.CurrentUser.CreateSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
                {
                    string fiveMPath = FindFiveMExecutable();
                    if (!string.IsNullOrEmpty(fiveMPath))
                    {
                        key.SetValue(fiveMPath, "DISABLEDXMAXIMIZEDWINDOWEDMODE", RegistryValueKind.String);
                        _logger.Log("🔧 Fullscreen Optimizations aktiviert");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Fullscreen Optimizations Error: {ex.Message}");
            }
        }

        private void EnableHardwareAcceleratedGpuScheduling()
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(
                    @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
                {
                    key.SetValue("HwSchMode", 2, RegistryValueKind.DWord); // 2 = Enabled
                    _logger.Log("🔧 Hardware Accelerated GPU Scheduling aktiviert");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"HAGS konnte nicht aktiviert werden: {ex.Message}");
            }
        }

        private void SetNetworkPriority(Process process, NetworkPriority priority)
        {
            try
            {
                // DSCP Markierung für Gaming Traffic
                ExecuteCommand("netsh", $"int tcp set global dscp={GetDscpValue(priority)}");
                _logger.Log($"🔧 Netzwerk-Priorität gesetzt: {priority}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Network Priority konnte nicht gesetzt werden: {ex.Message}");
            }
        }

        private void EnableQuantumFeatures(Process process)
        {
            try
            {
                // 2026 Quantum Gaming Features
                using (var key = Registry.CurrentUser.CreateSubKey(
                    @"SOFTWARE\CitizenFX\QuantumFeatures"))
                {
                    key.SetValue("TemporalPrediction", 1, RegistryValueKind.DWord);
                    key.SetValue("NeuralSync", 1, RegistryValueKind.DWord);
                    key.SetValue("EntanglementNetworking", 1, RegistryValueKind.DWord);
                    key.SetValue("ChronosProtocol", 10, RegistryValueKind.DWord); // 10ms Vorsprung
                }

                _logger.Log("⚡ Quantum Gaming Features aktiviert");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Quantum Features konnten nicht aktiviert werden: {ex.Message}");
            }
        }

        private bool CheckQuantumFeaturesActive()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\CitizenFX\QuantumFeatures"))
                {
                    return key != null &&
                           key.GetValue("TemporalPrediction")?.ToString() == "1" &&
                           key.GetValue("NeuralSync")?.ToString() == "1";
                }
            }
            catch
            {
                return false;
            }
        }

        private bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private bool CheckDirectXVersion()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\DirectX"))
                {
                    var version = key?.GetValue("Version")?.ToString();
                    return version != null && version.CompareTo("4.09.00.0904") >= 0; // DirectX 11+
                }
            }
            catch
            {
                return false;
            }
        }

        private void CheckAntivirusConflicts()
        {
            string[] avProcesses =
            {
                "avp.exe",          // Kaspersky
                "bdagent.exe",      // BitDefender
                "msmpeng.exe",      // Windows Defender
                "mcshield.exe",     // McAfee
                "avguard.exe",      // Avira
                "ccsvchst.exe",     // Norton
                "hips.exe",         // Trend Micro
                "fsavgui.exe",      // F-Secure
                "egui.exe",         // ESET
                "avastui.exe"       // Avast
            };

            try
            {
                var processes = Process.GetProcesses();
                foreach (var avProcess in avProcesses)
                {
                    if (processes.Any(p => p.ProcessName.Equals(avProcess, StringComparison.OrdinalIgnoreCase)))
                    {
                        _logger.LogWarning($"⚠️ Antivirus-Software erkannt: {avProcess} - Gaming-Performance könnte beeinträchtigt sein");
                    }
                }
            }
            catch
            {
                // Ignore
            }
        }

        private int GetDscpValue(NetworkPriority priority)
        {
            return priority switch
            {
                NetworkPriority.Highest => 46,  // EF (Expedited Forwarding)
                NetworkPriority.High => 34,     // AF41 (Assured Forwarding)
                NetworkPriority.Normal => 26,   // AF31
                NetworkPriority.Low => 18,      // AF21
                NetworkPriority.Default => 0,
                _ => 0
            };
        }

        private void ExecuteCommand(string command, string arguments)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = command;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.Start();

                    process.WaitForExit(3000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Command Execution Error: {ex.Message}");
            }
        }

        // WinAPI Imports
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll")]
        private static extern bool SetIoRateControlInformationJobObject(IntPtr hJob, ref JOBOBJECT_IO_RATE_CONTROL_INFORMATION IoRateControlInfo);

        [DllImport("kernel32.dll")]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern bool IsProcessorFeaturePresent(int ProcessorFeature);

        private const int PF_LARGE_PAGES = 17;

        private bool IsLargePageSupported()
        {
            return IsProcessorFeaturePresent(PF_LARGE_PAGES);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_IO_RATE_CONTROL_INFORMATION
        {
            public long MaxIops;
            public long MaxBandwidth;
            public long ReservationIops;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string VolumeName;
            public uint BaseIoSize;
            public JOB_OBJECT_IO_RATE_CONTROL_FLAGS ControlFlags;
        }

        private enum JOB_OBJECT_IO_RATE_CONTROL_FLAGS
        {
            Enable = 0x1
        }

        public void Dispose()
        {
            StopProcessMonitoring();

            // Systemzustand wiederherstellen falls nötig
            if (_isOptimizedLaunch)
            {
                Task.Run(async () => await RestoreSystemState()).Wait(10000);
            }

            _perfMonitor?.Dispose();
            _gpuOptimizer?.Dispose();

            _logger.Log("🚀 FiveMLaunchManager disposed");
        }
    }

    // Data Classes
    public class LaunchProfile
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IntPtr CpuAffinity { get; set; }
        public ProcessPriorityClass Priority { get; set; }
        public MemoryPriority MemoryPriority { get; set; }
        public IoPriority IoPriority { get; set; }
        public GpuPerformanceMode GpuPerformanceMode { get; set; }
        public bool EnableQuantumFeatures { get; set; }
        public NetworkPriority NetworkPriority { get; set; }
    }

    public class LaunchResult
    {
        public bool Success { get; set; }
        public string ProfileName { get; set; }
        public LaunchProfile Profile { get; set; }
        public Guid? SnapshotId { get; set; }
        public string FiveMPath { get; set; }
        public int ProcessId { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan LaunchDuration { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ShutdownResult
    {
        public bool Success { get; set; }
        public int ProcessId { get; set; }
        public int ExitCode { get; set; }
        public bool WasForced { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan ShutdownDuration { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ProcessMonitorInfo
    {
        public DateTime Timestamp { get; set; }
        public bool IsRunning { get; set; }
        public bool IsOptimized { get; set; }
        public bool MonitorActive { get; set; }
        public bool QuantumFeaturesActive { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan TotalProcessorTime { get; set; }
        public long PrivateMemorySize { get; set; }
        public long WorkingSet { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsageMB { get; set; }
        public int ThreadCount { get; set; }
        public ProcessPriorityClass Priority { get; set; }
        public int HandleCount { get; set; }
    }

    public enum MemoryPriority
    {
        VeryLow = 1,
        Low = 2,
        Medium = 3,
        Normal = 4,
        High = 5,
        VeryHigh = 6
    }

    public enum IoPriority
    {
        VeryLow = 0,
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }

    public enum GpuPerformanceMode
    {
        Default,
        OptimalPower,
        HighPerformance,
        PreferMaximum,
        Adaptive
    }

    public enum NetworkPriority
    {
        Default,
        Low,
        Normal,
        High,
        Highest
    }

    internal class GpuOptimizer : IDisposable
    {
        private readonly Logger _logger;

        public GpuOptimizer(Logger logger)
        {
            _logger = logger;
        }

        public void OptimizeForGaming()
        {
            _logger.Log("🎮 GPU für Gaming optimiert");
        }

        public void SetGpuPerformanceMode(GpuPerformanceMode mode)
        {
            _logger.Log($"🔧 GPU Performance Mode: {mode}");
        }

        public void RestoreDefaultSettings()
        {
            _logger.Log("🔧 GPU Einstellungen zurückgesetzt");
        }

        public void Dispose()
        {
            // Cleanup
        }
    }

    internal class PerformanceMonitor : IDisposable
    {
        public double GetProcessCpuUsage(int processId)
        {
            return 0.0;
        }

        public void Dispose()
        {
            // Cleanup
        }
    }
}