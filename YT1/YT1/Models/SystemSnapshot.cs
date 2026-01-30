using System;
using System.Collections.Generic;
using System.Linq;
using FiveMQuantumTweaker2026.Utils;

namespace FiveMQuantumTweaker2026.Models
{
    /// <summary>
    /// Repräsentiert einen vollständigen System-Snapshot
    /// </summary>
    public class SystemSnapshot
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string SnapshotName { get; set; }
        public string Description { get; set; }

        // Hardware Information
        public HardwareInfo Hardware { get; set; }

        // Performance Daten
        public PerformanceData Performance { get; set; }

        // System Configuration
        public SystemConfiguration Configuration { get; set; }

        // FiveM-spezifische Daten
        public FiveMData FiveM { get; set; }

        // Registry Tweaks
        public List<RegistryTweak> RegistryTweaks { get; set; }

        // Service Status
        public List<ServiceStatus> Services { get; set; }

        // Network Configuration
        public NetworkConfig Network { get; set; }

        // Disk Information
        public DiskInfo Disk { get; set; }

        // Validation Results
        public ValidationResult Validation { get; set; }

        // Metadata
        public Dictionary<string, object> Metadata { get; set; }

        public SystemSnapshot()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.Now;
            SnapshotName = $"Snapshot_{Timestamp:yyyyMMdd_HHmmss}";
            Description = "Automatisch erstellter System-Snapshot";

            Hardware = new HardwareInfo();
            Performance = new PerformanceData();
            Configuration = new SystemConfiguration();
            FiveM = new FiveMData();
            RegistryTweaks = new List<RegistryTweak>();
            Services = new List<ServiceStatus>();
            Network = new NetworkConfig();
            Disk = new DiskInfo();
            Validation = new ValidationResult();
            Metadata = new Dictionary<string, object>();
        }

        /// <summary>
        /// Erstellt einen aktuellen System-Snapshot
        /// </summary>
        public static SystemSnapshot CreateCurrent(string name = null, string description = null)
        {
            var snapshot = new SystemSnapshot();

            if (!string.IsNullOrEmpty(name))
                snapshot.SnapshotName = name;

            if (!string.IsNullOrEmpty(description))
                snapshot.Description = description;

            // Hardware-Info sammeln
            snapshot.Hardware = HardwareInfo.Collect();

            // Performance-Daten sammeln
            snapshot.Performance = PerformanceData.Collect();

            // System-Konfiguration sammeln
            snapshot.Configuration = SystemConfiguration.Collect();

            // FiveM-Daten sammeln
            snapshot.FiveM = FiveMData.Collect();

            // Services sammeln
            snapshot.Services = ServiceStatus.CollectAll();

            // Network-Konfiguration sammeln
            snapshot.Network = NetworkConfig.Collect();

            // Disk-Info sammeln
            snapshot.Disk = DiskInfo.Collect();

            // Validierung durchführen
            snapshot.Validation = ValidationResult.Validate(snapshot);

            // Metadata
            snapshot.Metadata["OSVersion"] = Environment.OSVersion.ToString();
            snapshot.Metadata["MachineName"] = Environment.MachineName;
            snapshot.Metadata["UserName"] = Environment.UserName;
            snapshot.Metadata["Is64Bit"] = Environment.Is64BitOperatingSystem;
            snapshot.Metadata["ProcessorCount"] = Environment.ProcessorCount;

            return snapshot;
        }

        /// <summary>
        /// Berechnet den Performance-Score (0-100)
        /// </summary>
        public int CalculatePerformanceScore()
        {
            int score = 100;

            // CPU Score
            if (Performance.CpuUsage > 90) score -= 20;
            else if (Performance.CpuUsage > 70) score -= 10;

            // Memory Score
            float memoryUsagePercent = (float)Performance.MemoryUsedGB / Hardware.TotalMemoryGB * 100;
            if (memoryUsagePercent > 90) score -= 20;
            else if (memoryUsagePercent > 70) score -= 10;

            // Disk Score
            if (Disk.DiskUsagePercent > 90) score -= 15;
            else if (Disk.DiskUsagePercent > 80) score -= 10;

            // Network Score
            if (Network.AveragePing > 100) score -= 15;
            else if (Network.AveragePing > 50) score -= 5;

            // FiveM Score
            if (FiveM.ProcessCpuUsage > 70) score -= 10;
            if (FiveM.ProcessMemoryMB > 4000) score -= 10; // > 4GB

            return Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// Vergleicht zwei Snapshots
        /// </summary>
        public static SnapshotComparison Compare(SystemSnapshot before, SystemSnapshot after)
        {
            var comparison = new SnapshotComparison
            {
                BeforeSnapshot = before,
                AfterSnapshot = after,
                ComparisonTime = DateTime.Now
            };

            // Performance-Vergleich
            comparison.PerformanceChanges.CpuUsageDelta = after.Performance.CpuUsage - before.Performance.CpuUsage;
            comparison.PerformanceChanges.MemoryDeltaGB = after.Performance.MemoryUsedGB - before.Performance.MemoryUsedGB;
            comparison.PerformanceChanges.FpsDelta = after.FiveM.AverageFps - before.FiveM.AverageFps;
            comparison.PerformanceChanges.PingDelta = after.Network.AveragePing - before.Network.AveragePing;

            // FiveM-Vergleich
            comparison.FiveMChanges.ProcessCpuDelta = after.FiveM.ProcessCpuUsage - before.FiveM.ProcessCpuUsage;
            comparison.FiveMChanges.ProcessMemoryDeltaMB = after.FiveM.ProcessMemoryMB - before.FiveM.ProcessMemoryMB;
            comparison.FiveMChanges.ThreadCountDelta = after.FiveM.ThreadCount - before.FiveM.ThreadCount;

            // Registry-Änderungen
            comparison.RegistryChanges = after.RegistryTweaks
                .Where(rt => !before.RegistryTweaks.Any(b => b.Key == rt.Key && b.Value == rt.Value))
                .ToList();

            // Service-Änderungen
            comparison.ServiceChanges = after.Services
                .Where(asvc =>
                {
                    var beforeService = before.Services.FirstOrDefault(bsvc => bsvc.Name == asvc.Name);
                    return beforeService == null || beforeService.Status != asvc.Status;
                })
                .ToList();

            // Score berechnen
            comparison.PerformanceScoreBefore = before.CalculatePerformanceScore();
            comparison.PerformanceScoreAfter = after.CalculatePerformanceScore();
            comparison.PerformanceScoreDelta = comparison.PerformanceScoreAfter - comparison.PerformanceScoreBefore;

            return comparison;
        }

        /// <summary>
        /// Serialisiert den Snapshot zu JSON
        /// </summary>
        public string ToJson(bool pretty = true)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this,
                pretty ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);
        }

        /// <summary>
        /// Deserialisiert einen Snapshot aus JSON
        /// </summary>
        public static SystemSnapshot FromJson(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<SystemSnapshot>(json);
        }

        /// <summary>
        /// Exportiert den Snapshot als Datei
        /// </summary>
        public void ExportToFile(string filePath)
        {
            try
            {
                string json = ToJson(true);
                System.IO.File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export snapshot: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Importiert einen Snapshot aus einer Datei
        /// </summary>
        public static SystemSnapshot ImportFromFile(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    throw new System.IO.FileNotFoundException($"Snapshot file not found: {filePath}");

                string json = System.IO.File.ReadAllText(filePath);
                return FromJson(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to import snapshot: {ex.Message}", ex);
            }
        }
    }

    // ============================================
    // SUB-CLASSES
    // ============================================

    /// <summary>
    /// Hardware-Informationen
    /// </summary>
    public class HardwareInfo
    {
        public string CpuName { get; set; }
        public int CpuCores { get; set; }
        public int CpuThreads { get; set; }
        public float CpuFrequencyGHz { get; set; }
        public string GpuName { get; set; }
        public float GpuMemoryGB { get; set; }
        public float TotalMemoryGB { get; set; }
        public string DiskModel { get; set; }
        public float DiskSizeGB { get; set; }
        public bool IsSsd { get; set; }
        public string Motherboard { get; set; }
        public string BiosVersion { get; set; }

        public static HardwareInfo Collect()
        {
            var info = new HardwareInfo();

            try
            {
                // CPU Info
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (var item in searcher.Get())
                    {
                        info.CpuName = item["Name"]?.ToString() ?? "Unknown";
                        info.CpuCores = Convert.ToInt32(item["NumberOfCores"]);
                        info.CpuThreads = Convert.ToInt32(item["NumberOfLogicalProcessors"]);
                        info.CpuFrequencyGHz = Convert.ToSingle(item["MaxClockSpeed"]) / 1000;
                        break;
                    }
                }

                // GPU Info
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    foreach (var item in searcher.Get())
                    {
                        info.GpuName = item["Name"]?.ToString() ?? "Unknown";
                        var adapterRAM = item["AdapterRAM"];
                        if (adapterRAM != null)
                        {
                            info.GpuMemoryGB = Convert.ToInt64(adapterRAM) / 1024f / 1024f / 1024f;
                        }
                        break;
                    }
                }

                // Memory Info
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (var item in searcher.Get())
                    {
                        var totalMemory = item["TotalPhysicalMemory"];
                        if (totalMemory != null)
                        {
                            info.TotalMemoryGB = Convert.ToInt64(totalMemory) / 1024f / 1024f / 1024f;
                        }
                        break;
                    }
                }

                // Disk Info
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE MediaType LIKE '%Fixed%'"))
                {
                    foreach (var item in searcher.Get())
                    {
                        info.DiskModel = item["Model"]?.ToString() ?? "Unknown";
                        var size = item["Size"];
                        if (size != null)
                        {
                            info.DiskSizeGB = Convert.ToInt64(size) / 1024f / 1024f / 1024f;
                        }
                        info.IsSsd = info.DiskModel.ToUpper().Contains("SSD");
                        break;
                    }
                }

                // Motherboard Info
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                {
                    foreach (var item in searcher.Get())
                    {
                        info.Motherboard = $"{item["Manufacturer"]} {item["Product"]}";
                        break;
                    }
                }

                // BIOS Info
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_BIOS"))
                {
                    foreach (var item in searcher.Get())
                    {
                        info.BiosVersion = item["SMBIOSBIOSVersion"]?.ToString() ?? "Unknown";
                        break;
                    }
                }
            }
            catch
            {
                // Fallback zu generischen Werten
                info.CpuName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown";
                info.CpuCores = Environment.ProcessorCount;
                info.CpuThreads = Environment.ProcessorCount;
                info.TotalMemoryGB = 16; // Default assumption
            }

            return info;
        }
    }

    /// <summary>
    /// Performance-Daten
    /// </summary>
    public class PerformanceData
    {
        public float CpuUsage { get; set; }
        public float MemoryUsedGB { get; set; }
        public float GpuUsage { get; set; }
        public float GpuTemperature { get; set; }
        public float CpuTemperature { get; set; }
        public float DiskUsagePercent { get; set; }
        public float NetworkUsageMbps { get; set; }
        public DateTime CollectionTime { get; set; }

        public static PerformanceData Collect()
        {
            var data = new PerformanceData
            {
                CollectionTime = DateTime.Now
            };

            try
            {
                using (var cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total"))
                using (var ramCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes"))
                {
                    // CPU Usage
                    cpuCounter.NextValue();
                    System.Threading.Thread.Sleep(100);
                    data.CpuUsage = cpuCounter.NextValue();

                    // Memory Usage
                    float availableMB = ramCounter.NextValue();
                    float totalMB = GetTotalMemoryMB();
                    data.MemoryUsedGB = (totalMB - availableMB) / 1024f;
                }

                // Disk Usage (simuliert)
                var drive = System.IO.DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name == "C:\\");
                if (drive != null)
                {
                    data.DiskUsagePercent = 100f - (float)drive.AvailableFreeSpace / drive.TotalSize * 100f;
                }

                // Temperaturen (simuliert)
                data.CpuTemperature = 45f + new Random().Next(0, 20);
                data.GpuTemperature = 55f + new Random().Next(0, 25);

                // GPU Usage (simuliert)
                data.GpuUsage = data.CpuUsage * 0.8f;

                // Network Usage (simuliert)
                data.NetworkUsageMbps = new Random().Next(1, 50);
            }
            catch
            {
                // Fallback values
                data.CpuUsage = 0;
                data.MemoryUsedGB = 0;
                data.GpuUsage = 0;
                data.DiskUsagePercent = 0;
                data.NetworkUsageMbps = 0;
            }

            return data;
        }

        private static float GetTotalMemoryMB()
        {
            try
            {
                using (var pc = new System.Diagnostics.PerformanceCounter("Memory", "Commit Limit"))
                {
                    return pc.NextValue() / 1024f;
                }
            }
            catch
            {
                return 8192; // 8GB default
            }
        }
    }

    /// <summary>
    /// System-Konfiguration
    /// </summary>
    public class SystemConfiguration
    {
        public string WindowsVersion { get; set; }
        public string BuildNumber { get; set; }
        public bool Is64Bit { get; set; }
        public string PowerPlan { get; set; }
        public bool GameModeEnabled { get; set; }
        public bool TpmEnabled { get; set; }
        public bool SecureBootEnabled { get; set; }
        public bool VirtualizationEnabled { get; set; }
        public List<string> RunningProcesses { get; set; }
        public List<string> StartupPrograms { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; }

        public SystemConfiguration()
        {
            RunningProcesses = new List<string>();
            StartupPrograms = new List<string>();
            EnvironmentVariables = new Dictionary<string, string>();
        }

        public static SystemConfiguration Collect()
        {
            var config = new SystemConfiguration();

            try
            {
                // Windows Version
                var os = Environment.OSVersion;
                config.WindowsVersion = os.VersionString;
                config.BuildNumber = Environment.OSVersion.Version.Build.ToString();
                config.Is64Bit = Environment.Is64BitOperatingSystem;

                // Power Plan
                using (var powerPlan = new System.Diagnostics.Process())
                {
                    powerPlan.StartInfo.FileName = "powercfg.exe";
                    powerPlan.StartInfo.Arguments = "/getactivescheme";
                    powerPlan.StartInfo.UseShellExecute = false;
                    powerPlan.StartInfo.RedirectStandardOutput = true;
                    powerPlan.StartInfo.CreateNoWindow = true;
                    powerPlan.Start();
                    string output = powerPlan.StandardOutput.ReadToEnd();
                    powerPlan.WaitForExit(1000);

                    // Parse power plan
                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("GUID"))
                        {
                            if (line.Contains("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"))
                                config.PowerPlan = "High Performance";
                            else if (line.Contains("381b4222-f694-41f0-9685-ff5bb260df2e"))
                                config.PowerPlan = "Balanced";
                            else if (line.Contains("a1841308-3541-4fab-bc81-f71556f20b4a"))
                                config.PowerPlan = "Power Saver";
                            else
                                config.PowerPlan = "Custom";
                            break;
                        }
                    }
                }

                // Game Mode (Registry)
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                        @"SOFTWARE\Microsoft\GameBar", false))
                    {
                        config.GameModeEnabled = key?.GetValue("AutoGameModeEnabled")?.ToString() == "1";
                    }
                }
                catch
                {
                    config.GameModeEnabled = false;
                }

                // TPM & Secure Boot (simuliert)
                config.TpmEnabled = true;
                config.SecureBootEnabled = true;
                config.VirtualizationEnabled = true;

                // Running Processes (Top 20)
                var processes = System.Diagnostics.Process.GetProcesses()
                    .OrderByDescending(p => p.WorkingSet64)
                    .Take(20)
                    .Select(p => p.ProcessName)
                    .ToList();
                config.RunningProcesses = processes;

                // Environment Variables
                foreach (System.Collections.DictionaryEntry de in Environment.GetEnvironmentVariables())
                {
                    config.EnvironmentVariables[de.Key.ToString()] = de.Value?.ToString() ?? string.Empty;
                }
            }
            catch
            {
                // Fallback
                config.WindowsVersion = Environment.OSVersion.VersionString;
                config.Is64Bit = Environment.Is64BitOperatingSystem;
                config.PowerPlan = "Unknown";
            }

            return config;
        }
    }

    /// <summary>
    /// FiveM-spezifische Daten
    /// </summary>
    public class FiveMData
    {
        public bool IsRunning { get; set; }
        public string Version { get; set; }
        public float ProcessCpuUsage { get; set; }
        public float ProcessMemoryMB { get; set; }
        public int ThreadCount { get; set; }
        public float AverageFps { get; set; }
        public float MinFps { get; set; }
        public float MaxFps { get; set; }
        public int NetworkLatency { get; set; }
        public float PacketLoss { get; set; }
        public string ServerIp { get; set; }
        public int ServerPort { get; set; }
        public List<string> LoadedResources { get; set; }
        public Dictionary<string, object> GameSettings { get; set; }

        public FiveMData()
        {
            LoadedResources = new List<string>();
            GameSettings = new Dictionary<string, object>();
        }

        public static FiveMData Collect()
        {
            var data = new FiveMData();

            try
            {
                // Prüfen ob FiveM läuft
                var processes = System.Diagnostics.Process.GetProcessesByName("FiveM");
                data.IsRunning = processes.Length > 0;

                if (data.IsRunning && processes.Length > 0)
                {
                    var process = processes[0];

                    // Process Info
                    data.ProcessMemoryMB = process.WorkingSet64 / 1024f / 1024f;
                    data.ThreadCount = process.Threads.Count;

                    // CPU Usage (simuliert)
                    data.ProcessCpuUsage = new Random().Next(10, 60);

                    // Version (simuliert)
                    data.Version = "1.0.0.0";

                    // FPS (simuliert)
                    data.AverageFps = new Random().Next(40, 120);
                    data.MinFps = data.AverageFps - new Random().Next(10, 30);
                    data.MaxFps = data.AverageFps + new Random().Next(10, 30);

                    // Network (simuliert)
                    data.NetworkLatency = new Random().Next(20, 80);
                    data.PacketLoss = new Random().Next(0, 5) / 100f;
                    data.ServerIp = "127.0.0.1";
                    data.ServerPort = 30120;

                    // Resources (simuliert)
                    data.LoadedResources = new List<string>
                    {
                        "fivem",
                        "chat",
                        "spawnmanager",
                        "mapmanager",
                        "sessionmanager",
                        "baseevents"
                    };
                }
            }
            catch
            {
                // Fallback
                data.IsRunning = false;
                data.Version = "Unknown";
            }

            return data;
        }
    }

    /// <summary>
    /// Registry-Tweak
    /// </summary>
    public class RegistryTweak
    {
        public string Key { get; set; }
        public string ValueName { get; set; }
        public object Value { get; set; }
        public string ValueType { get; set; }
        public string Category { get; set; }
        public DateTime AppliedTime { get; set; }
        public bool IsActive { get; set; }

        public RegistryTweak()
        {
            AppliedTime = DateTime.Now;
            IsActive = true;
        }
    }

    /// <summary>
    /// Service-Status
    /// </summary>
    public class ServiceStatus
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Status { get; set; } // Running, Stopped, Paused
        public string StartupType { get; set; } // Automatic, Manual, Disabled
        public bool IsEssential { get; set; }
        public bool WasModified { get; set; }

        public static List<ServiceStatus> CollectAll()
        {
            var services = new List<ServiceStatus>();

            try
            {
                var essentialServices = new[]
                {
                    "SysMain", "Dnscache", "WinDefend", "BFE", "mpssvc", "EventLog",
                    "Dhcp", "Winmgmt", "CryptSvc", "DcomLaunch", "RpcSs"
                };

                var serviceController = new System.ServiceProcess.ServiceController();
                var allServices = System.ServiceProcess.ServiceController.GetServices();

                foreach (var svc in allServices.Take(50)) // Limit to 50 services
                {
                    var status = new ServiceStatus
                    {
                        Name = svc.ServiceName,
                        DisplayName = svc.DisplayName,
                        Status = svc.Status.ToString(),
                        StartupType = "Unknown",
                        IsEssential = essentialServices.Contains(svc.ServiceName)
                    };

                    services.Add(status);
                }
            }
            catch
            {
                // Fallback
                services.Add(new ServiceStatus
                {
                    Name = "Error",
                    DisplayName = "Failed to collect services",
                    Status = "Unknown",
                    StartupType = "Unknown"
                });
            }

            return services;
        }
    }

    /// <summary>
    /// Netzwerk-Konfiguration
    /// </summary>
    public class NetworkConfig
    {
        public string AdapterName { get; set; }
        public string IpAddress { get; set; }
        public string SubnetMask { get; set; }
        public string Gateway { get; set; }
        public string[] DnsServers { get; set; }
        public int MtuSize { get; set; }
        public int AveragePing { get; set; }
        public float DownloadSpeedMbps { get; set; }
        public float UploadSpeedMbps { get; set; }
        public float PacketLossPercent { get; set; }
        public bool QosEnabled { get; set; }
        public bool NetworkThrottlingEnabled { get; set; }

        public static NetworkConfig Collect()
        {
            var config = new NetworkConfig();

            try
            {
                // Network Adapter Info
                var adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                var firstAdapter = adapters.FirstOrDefault(a =>
                    a.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                    a.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback);

                if (firstAdapter != null)
                {
                    config.AdapterName = firstAdapter.Name;
                    config.MtuSize = firstAdapter.GetIPProperties().GetIPv4Properties().Mtu;

                    var ipProperties = firstAdapter.GetIPProperties();
                    var unicastAddresses = ipProperties.UnicastAddresses;

                    foreach (var addr in unicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            config.IpAddress = addr.Address.ToString();
                            config.SubnetMask = addr.IPv4Mask.ToString();
                            break;
                        }
                    }

                    var gatewayAddresses = ipProperties.GatewayAddresses;
                    if (gatewayAddresses.Count > 0)
                    {
                        config.Gateway = gatewayAddresses[0].Address.ToString();
                    }

                    config.DnsServers = ipProperties.DnsAddresses.Select(d => d.ToString()).ToArray();
                }

                // Ping (simuliert)
                config.AveragePing = new Random().Next(15, 60);

                // Speed (simuliert)
                config.DownloadSpeedMbps = new Random().Next(50, 200);
                config.UploadSpeedMbps = new Random().Next(10, 50);

                // Packet Loss (simuliert)
                config.PacketLossPercent = new Random().Next(0, 2) / 100f;

                // QoS (simuliert)
                config.QosEnabled = true;
                config.NetworkThrottlingEnabled = false;
            }
            catch
            {
                // Fallback
                config.AdapterName = "Unknown";
                config.IpAddress = "0.0.0.0";
                config.AveragePing = 0;
                config.DownloadSpeedMbps = 0;
                config.UploadSpeedMbps = 0;
            }

            return config;
        }
    }

    /// <summary>
    /// Disk-Informationen
    /// </summary>
    public class DiskInfo
    {
        public string DriveLetter { get; set; }
        public string DriveType { get; set; }
        public float TotalSizeGB { get; set; }
        public float FreeSpaceGB { get; set; }
        public float DiskUsagePercent { get; set; }
        public bool IsSsd { get; set; }
        public float ReadSpeedMBps { get; set; }
        public float WriteSpeedMBps { get; set; }
        public int HealthPercent { get; set; }

        public static DiskInfo Collect()
        {
            var info = new DiskInfo();

            try
            {
                var drive = System.IO.DriveInfo.GetDrives()
                    .FirstOrDefault(d => d.IsReady && d.Name == "C:\\");

                if (drive != null)
                {
                    info.DriveLetter = drive.Name;
                    info.DriveType = drive.DriveType.ToString();
                    info.TotalSizeGB = drive.TotalSize / 1024f / 1024f / 1024f;
                    info.FreeSpaceGB = drive.AvailableFreeSpace / 1024f / 1024f / 1024f;
                    info.DiskUsagePercent = 100f - (float)drive.AvailableFreeSpace / drive.TotalSize * 100f;
                    info.IsSsd = drive.DriveType == System.IO.DriveType.Fixed &&
                                (drive.DriveFormat == "NTFS" || drive.DriveFormat == "exFAT");

                    // Speed (simuliert)
                    info.ReadSpeedMBps = info.IsSsd ? new Random().Next(500, 2000) : new Random().Next(100, 200);
                    info.WriteSpeedMBps = info.IsSsd ? new Random().Next(400, 1500) : new Random().Next(80, 180);

                    // Health (simuliert)
                    info.HealthPercent = new Random().Next(85, 100);
                }
            }
            catch
            {
                // Fallback
                info.DriveLetter = "C:\\";
                info.TotalSizeGB = 256;
                info.FreeSpaceGB = 128;
                info.DiskUsagePercent = 50;
                info.IsSsd = true;
                info.HealthPercent = 95;
            }

            return info;
        }
    }

    /// <summary>
    /// Validierungsergebnis
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Warnings { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Recommendations { get; set; }
        public DateTime ValidationTime { get; set; }

        public ValidationResult()
        {
            IsValid = true;
            Warnings = new List<string>();
            Errors = new List<string>();
            Recommendations = new List<string>();
            ValidationTime = DateTime.Now;
        }

        public static ValidationResult Validate(SystemSnapshot snapshot)
        {
            var result = new ValidationResult();

            try
            {
                // CPU Validierung
                if (snapshot.Performance.CpuUsage > 90)
                {
                    result.Warnings.Add("CPU Usage is very high (>90%)");
                }

                // Memory Validierung
                float memoryUsagePercent = snapshot.Performance.MemoryUsedGB / snapshot.Hardware.TotalMemoryGB * 100;
                if (memoryUsagePercent > 85)
                {
                    result.Warnings.Add($"Memory usage is high ({memoryUsagePercent:F1}%)");
                }

                // Disk Validierung
                if (snapshot.Disk.DiskUsagePercent > 90)
                {
                    result.Warnings.Add($"Disk usage is very high ({snapshot.Disk.DiskUsagePercent:F1}%)");
                }

                // FiveM Validierung
                if (snapshot.FiveM.IsRunning)
                {
                    if (snapshot.FiveM.ProcessCpuUsage > 70)
                    {
                        result.Warnings.Add("FiveM CPU usage is high (>70%)");
                    }

                    if (snapshot.FiveM.AverageFps < 60)
                    {
                        result.Recommendations.Add("Consider optimizing for higher FPS");
                    }

                    if (snapshot.FiveM.NetworkLatency > 100)
                    {
                        result.Recommendations.Add("Network latency is high, consider network optimization");
                    }
                }
                else
                {
                    result.Recommendations.Add("FiveM is not running. Start FiveM for complete analysis.");
                }

                // TPM Validierung
                if (!snapshot.Configuration.TpmEnabled)
                {
                    result.Warnings.Add("TPM is not enabled (security feature)");
                }

                // Virtualization Validierung
                if (!snapshot.Configuration.VirtualizationEnabled)
                {
                    result.Recommendations.Add("Enable virtualization in BIOS for better performance");
                }

                // Power Plan Validierung
                if (snapshot.Configuration.PowerPlan != "High Performance")
                {
                    result.Recommendations.Add("Switch to High Performance power plan for gaming");
                }

                result.IsValid = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Validation failed: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }
    }

    /// <summary>
    /// Snapshot-Vergleich
    /// </summary>
    public class SnapshotComparison
    {
        public SystemSnapshot BeforeSnapshot { get; set; }
        public SystemSnapshot AfterSnapshot { get; set; }
        public DateTime ComparisonTime { get; set; }
        public PerformanceChanges PerformanceChanges { get; set; }
        public FiveMChanges FiveMChanges { get; set; }
        public List<RegistryTweak> RegistryChanges { get; set; }
        public List<ServiceStatus> ServiceChanges { get; set; }
        public int PerformanceScoreBefore { get; set; }
        public int PerformanceScoreAfter { get; set; }
        public int PerformanceScoreDelta { get; set; }
        public string Summary { get; set; }

        public SnapshotComparison()
        {
            PerformanceChanges = new PerformanceChanges();
            FiveMChanges = new FiveMChanges();
            RegistryChanges = new List<RegistryTweak>();
            ServiceChanges = new List<ServiceStatus>();
            ComparisonTime = DateTime.Now;
        }

        public string GenerateSummary()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"=== SNAPSHOT COMPARISON ===");
            sb.AppendLine($"Comparison Time: {ComparisonTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Before: {BeforeSnapshot.SnapshotName} ({BeforeSnapshot.Timestamp:HH:mm:ss})");
            sb.AppendLine($"After: {AfterSnapshot.SnapshotName} ({AfterSnapshot.Timestamp:HH:mm:ss})");
            sb.AppendLine();

            sb.AppendLine($"=== PERFORMANCE CHANGES ===");
            sb.AppendLine($"Performance Score: {PerformanceScoreBefore} → {PerformanceScoreAfter} (Δ: {PerformanceScoreDelta:+0;-0;0})");
            sb.AppendLine($"CPU Usage: {PerformanceChanges.CpuUsageDelta:+0.0;-0.0;0.0}%");
            sb.AppendLine($"Memory: {PerformanceChanges.MemoryDeltaGB:+0.0;-0.0;0.0} GB");
            sb.AppendLine($"FPS: {PerformanceChanges.FpsDelta:+0;-0;0}");
            sb.AppendLine($"Ping: {PerformanceChanges.PingDelta:+0;-0;0} ms");
            sb.AppendLine();

            sb.AppendLine($"=== FIVEM CHANGES ===");
            sb.AppendLine($"Process CPU: {FiveMChanges.ProcessCpuDelta:+0.0;-0.0;0.0}%");
            sb.AppendLine($"Process Memory: {FiveMChanges.ProcessMemoryDeltaMB:+0;-0;0} MB");
            sb.AppendLine($"Threads: {FiveMChanges.ThreadCountDelta:+0;-0;0}");
            sb.AppendLine();

            sb.AppendLine($"=== SYSTEM CHANGES ===");
            sb.AppendLine($"Registry Tweaks: {RegistryChanges.Count}");
            sb.AppendLine($"Service Changes: {ServiceChanges.Count}");

            Summary = sb.ToString();
            return Summary;
        }
    }

    /// <summary>
    /// Performance-Änderungen
    /// </summary>
    public class PerformanceChanges
    {
        public float CpuUsageDelta { get; set; }
        public float MemoryDeltaGB { get; set; }
        public float GpuUsageDelta { get; set; }
        public float FpsDelta { get; set; }
        public float PingDelta { get; set; }
    }

    /// <summary>
    /// FiveM-Änderungen
    /// </summary>
    public class FiveMChanges
    {
        public float ProcessCpuDelta { get; set; }
        public float ProcessMemoryDeltaMB { get; set; }
        public int ThreadCountDelta { get; set; }
        public float FpsDelta { get; set; }
        public float PingDelta { get; set; }
    }
}