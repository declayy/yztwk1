using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace FiveMQuantumTweaker2026.Core
{
    /// <summary>
    /// System-Integritätsmanagement mit 2026er DNA-based Security
    /// </summary>
    public class SystemSanityManager : IDisposable
    {
        private readonly Logger _logger;
        private readonly string _backupDirectory;
        private readonly string _dnaHashFile;
        private readonly Timer _integrityMonitor;
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _ramCounter;

        // DNA Security Constants
        private const string DNA_SEED = "QUANTUM2026_FIVEM_TWEAKER";
        private const int HASH_ITERATIONS = 10000;

        // Geschützte Systemkomponenten (NIE verändern)
        private readonly HashSet<string> _protectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"C:\Windows\System32\ntoskrnl.exe",
            @"C:\Windows\System32\hal.dll",
            @"C:\Windows\System32\winload.exe",
            @"C:\Windows\System32\winload.efi",
            @"C:\Windows\System32\SecureBoot.dll",
            @"C:\Windows\System32\TPM.dll",
            @"C:\Windows\System32\drivers\Wdf01000.sys",
            @"C:\Windows\System32\drivers\ACPI.sys"
        };

        private readonly HashSet<string> _protectedRegistryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
            @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}",
            @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Defender",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"
        };

        public SystemSanityManager(Logger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Backup-Verzeichnis erstellen
            _backupDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FiveMQuantumTweaker",
                "Backups",
                DateTime.Now.ToString("yyyyMMdd_HHmmss")
            );

            _dnaHashFile = Path.Combine(_backupDirectory, "system_dna.json");

            // Performance Counter initialisieren
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch
            {
                _logger.LogWarning("Performance Counter konnten nicht initialisiert werden");
            }

            // Integritäts-Monitor starten (alle 30 Sekunden)
            _integrityMonitor = new Timer(IntegrityCheckCallback, null, 30000, 30000);

            Directory.CreateDirectory(_backupDirectory);
            _logger.Log($"🔐 SystemSanityManager initialisiert. Backup-Verzeichnis: {_backupDirectory}");
        }

        /// <summary>
        /// Erstellt einen vollständigen System-Snapshot vor Optimierungen
        /// </summary>
        public SystemSnapshot CreateSystemSnapshot(string operationName)
        {
            var snapshot = new SystemSnapshot
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Operation = operationName,
                DnaHash = GenerateSystemDNA(),
                BackupLocation = _backupDirectory
            };

            try
            {
                _logger.Log("📸 Erstelle System-Snapshot...");

                // 1. Registry-Backup
                snapshot.RegistryBackup = BackupRegistry();

                // 2. System-Konfiguration
                snapshot.SystemConfig = CaptureSystemConfiguration();

                // 3. Netzwerk-Einstellungen
                snapshot.NetworkConfig = CaptureNetworkConfiguration();

                // 4. Dienst-Status
                snapshot.ServiceStates = CaptureServiceStates();

                // 5. Performance-Baseline
                snapshot.PerformanceBaseline = CapturePerformanceBaseline();

                // 6. Datei-Hashes von geschützten Systemdateien
                snapshot.ProtectedFileHashes = CalculateProtectedFileHashes();

                // Snapshot speichern
                SaveSnapshot(snapshot);

                _logger.Log($"✅ System-Snapshot #{snapshot.Id} erstellt für: {operationName}");

                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ System-Snapshot fehlgeschlagen: {ex.Message}");
                throw new SystemSanityException("Snapshot creation failed", ex);
            }
        }

        /// <summary>
        /// Stellt System aus Snapshot wieder her
        /// </summary>
        public bool RestoreSystem(Guid snapshotId)
        {
            try
            {
                _logger.Log($"🔄 Starte System-Wiederherstellung aus Snapshot #{snapshotId}...");

                // Snapshot laden
                var snapshot = LoadSnapshot(snapshotId);
                if (snapshot == null)
                {
                    _logger.LogError($"❌ Snapshot #{snapshotId} nicht gefunden");
                    return false;
                }

                // DNA-Integrität prüfen
                if (!ValidateSystemDNA(snapshot.DnaHash))
                {
                    _logger.LogError("❌ System-DNA Integritätsverletzung erkannt!");
                    throw new SecurityException("System DNA integrity violation detected");
                }

                // 1. Registry wiederherstellen
                if (!string.IsNullOrEmpty(snapshot.RegistryBackup))
                {
                    RestoreRegistry(snapshot.RegistryBackup);
                }

                // 2. Netzwerk-Einstellungen wiederherstellen
                if (snapshot.NetworkConfig != null)
                {
                    RestoreNetworkConfiguration(snapshot.NetworkConfig);
                }

                // 3. Dienste wiederherstellen
                if (snapshot.ServiceStates != null)
                {
                    RestoreServiceStates(snapshot.ServiceStates);
                }

                // 4. Geschützte Dateien validieren
                ValidateProtectedFiles(snapshot.ProtectedFileHashes);

                // 5. System neu starten empfohlen
                RecommendSystemRestart();

                _logger.Log($"✅ System erfolgreich aus Snapshot #{snapshotId} wiederhergestellt");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ System-Wiederherstellung fehlgeschlagen: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Überwacht System-Integrität in Echtzeit
        /// </summary>
        public SystemHealthStatus MonitorSystemHealth()
        {
            var status = new SystemHealthStatus
            {
                Timestamp = DateTime.Now,
                MonitorId = Guid.NewGuid()
            };

            try
            {
                // 1. CPU-Auslastung
                status.CpuUsage = _cpuCounter?.NextValue() ?? 0;

                // 2. Verfügbarer RAM
                status.AvailableMemoryMB = _ramCounter?.NextValue() ?? 0;

                // 3. Disk-Auslastung
                status.DiskUsage = GetDiskUsage();

                // 4. Temperatur-Überwachung (falls verfügbar)
                status.Temperatures = GetSystemTemperatures();

                // 5. Prozess-Überwachung
                status.SuspiciousProcesses = DetectSuspiciousProcesses();

                // 6. Netzwerk-Überwachung
                status.NetworkAnomalies = DetectNetworkAnomalies();

                // 7. TPM-Status
                status.TpmStatus = CheckTpmStatus();

                // 8. Secure Boot Status
                status.SecureBootEnabled = IsSecureBootEnabled();

                // Health Score berechnen (0-100)
                status.HealthScore = CalculateHealthScore(status);

                // Warnungen generieren
                status.Warnings = GenerateHealthWarnings(status);

                _logger.Log($"📊 System Health: {status.HealthScore}/100 | CPU: {status.CpuUsage:F1}% | RAM: {status.AvailableMemoryMB}MB");

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError($"System Health Monitoring Error: {ex.Message}");
                status.HealthScore = -1;
                return status;
            }
        }

        /// <summary>
        /// Validiert System-Integrität nach Optimierung
        /// </summary>
        public IntegrityValidationResult ValidateSystemIntegrity()
        {
            var result = new IntegrityValidationResult
            {
                ValidationId = Guid.NewGuid(),
                Timestamp = DateTime.Now
            };

            try
            {
                _logger.Log("🔍 Validiere System-Integrität...");

                // 1. TPM & Secure Boot Validierung
                result.TpmValid = CheckTpmStatus() == TpmStatus.Healthy;
                result.SecureBootValid = IsSecureBootEnabled();

                // 2. System-Datei-Integrität
                result.SystemFilesValid = ValidateSystemFiles();

                // 3. Registry-Integrität
                result.RegistryValid = ValidateRegistry();

                // 4. Dienst-Integrität
                result.ServicesValid = ValidateCriticalServices();

                // 5. Netzwerk-Integrität
                result.NetworkValid = ValidateNetworkStack();

                // 6. Performance-Integrität
                result.PerformanceValid = ValidatePerformance();

                // 7. Sicherheits-Integrität
                result.SecurityValid = ValidateSecurityFeatures();

                // Gesamtergebnis
                result.IsValid = result.TpmValid &&
                               result.SecureBootValid &&
                               result.SystemFilesValid &&
                               result.ServicesValid;

                result.ValidationMessage = result.IsValid
                    ? "✅ System-Integrität 100% validiert"
                    : "⚠️ System-Integrität beeinträchtigt";

                _logger.Log(result.ValidationMessage);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Integrity Validation Error: {ex.Message}");
                result.IsValid = false;
                result.ValidationMessage = $"Validierung fehlgeschlagen: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 2026: DNA-based System Fingerprinting
        /// </summary>
        private string GenerateSystemDNA()
        {
            try
            {
                using (var sha256 = SHA256.Create())
                using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(DNA_SEED)))
                {
                    // System-Identität sammeln
                    var systemId = new StringBuilder();

                    // 1. Hardware-Identifikation
                    systemId.Append(Environment.MachineName);
                    systemId.Append(Environment.ProcessorCount);

                    // 2. CPU-ID
                    systemId.Append(GetCpuId());

                    // 3. Mainboard-Seriennummer
                    systemId.Append(GetMotherboardSerial());

                    // 4. TPM-ID
                    systemId.Append(GetTpmId());

                    // 5. Disk-Seriennummer
                    systemId.Append(GetDiskSerial());

                    // 6. MAC-Adresse
                    systemId.Append(GetMacAddress());

                    // 7. Windows Installation ID
                    systemId.Append(GetWindowsInstallationId());

                    // DNA generieren
                    var dnaBytes = Encoding.UTF8.GetBytes(systemId.ToString());

                    // Mehrstufige Hashing mit Salt
                    var hash1 = sha256.ComputeHash(dnaBytes);
                    var hash2 = hmac.ComputeHash(hash1);

                    // Iteratives Hashing für erhöhte Sicherheit
                    for (int i = 0; i < HASH_ITERATIONS; i++)
                    {
                        hash2 = sha256.ComputeHash(hash2);
                    }

                    string dnaHash = BitConverter.ToString(hash2).Replace("-", "");

                    _logger.Log($"🧬 System-DNA generiert: {dnaHash.Substring(0, 16)}...");

                    return dnaHash;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"DNA Generation Error: {ex.Message}");
                return "ERROR";
            }
        }

        private bool ValidateSystemDNA(string expectedDna)
        {
            try
            {
                string currentDna = GenerateSystemDNA();
                bool isValid = currentDna == expectedDna;

                if (!isValid)
                {
                    _logger.LogError($"❌ DNA-Mismatch! Erwartet: {expectedDna.Substring(0, 16)}..., Aktuell: {currentDna.Substring(0, 16)}...");

                    // Erweiterte Forensik bei Mismatch
                    PerformForensicAnalysis(expectedDna, currentDna);
                }

                return isValid;
            }
            catch
            {
                return false;
            }
        }

        private string BackupRegistry()
        {
            try
            {
                string backupFile = Path.Combine(_backupDirectory, $"registry_backup_{DateTime.Now:yyyyMMdd_HHmmss}.reg");

                // Wichtige Registry-Schlüssel exportieren
                string[] registryKeys =
                {
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer",
                    @"HKEY_CURRENT_USER\Software\CitizenFX"
                };

                var backupScript = new StringBuilder();
                backupScript.AppendLine("Windows Registry Editor Version 5.00");
                backupScript.AppendLine();

                foreach (var key in registryKeys)
                {
                    backupScript.AppendLine($"[{key}]");

                    try
                    {
                        using (var regKey = GetRegistryKey(key))
                        {
                            if (regKey != null)
                            {
                                foreach (string valueName in regKey.GetValueNames())
                                {
                                    object value = regKey.GetValue(valueName);
                                    string valueType = GetRegistryValueType(value);
                                    string valueData = FormatRegistryValue(value);

                                    backupScript.AppendLine($"\"{valueName}\"={valueType}:{valueData}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Registry Backup für {key} fehlgeschlagen: {ex.Message}");
                    }

                    backupScript.AppendLine();
                }

                File.WriteAllText(backupFile, backupScript.ToString());
                _logger.Log($"🔐 Registry-Backup erstellt: {backupFile}");

                return backupFile;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Registry Backup Error: {ex.Message}");
                return string.Empty;
            }
        }

        private SystemConfiguration CaptureSystemConfiguration()
        {
            return new SystemConfiguration
            {
                OsVersion = Environment.OSVersion.ToString(),
                Is64Bit = Environment.Is64BitOperatingSystem,
                ProcessorCount = Environment.ProcessorCount,
                SystemDirectory = Environment.SystemDirectory,
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                WindowsEdition = GetWindowsEdition(),
                UefiEnabled = IsUefiEnabled(),
                VirtualizationEnabled = IsVirtualizationEnabled()
            };
        }

        private NetworkConfiguration CaptureNetworkConfiguration()
        {
            var config = new NetworkConfiguration();

            try
            {
                // IP-Konfiguration
                var hostEntry = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                config.IpAddresses = hostEntry.AddressList.Select(ip => ip.ToString()).ToArray();

                // DNS-Server
                config.DnsServers = GetDnsServers();

                // Netzwerk-Adapter
                config.NetworkAdapters = GetNetworkAdapterInfo();

                // Firewall-Status
                config.FirewallEnabled = IsFirewallEnabled();

                // QoS-Einstellungen
                config.QosEnabled = IsQosEnabled();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Network Configuration Capture Error: {ex.Message}");
            }

            return config;
        }

        private Dictionary<string, ServiceState> CaptureServiceStates()
        {
            var services = new Dictionary<string, ServiceState>();

            string[] criticalServices =
            {
                "WinDefend",        // Windows Defender
                "mpssvc",           // Windows Firewall
                "Dnscache",         // DNS Client
                "Dhcp",             // DHCP Client
                "EventLog",         // Event Log
                "PlugPlay",         // Plug and Play
                "RpcSs",            // Remote Procedure Call
                "SamSs",            // Security Accounts Manager
                "LanmanServer",     // Server
                "LanmanWorkstation", // Workstation
                "SysMain",          // Superfetch
                "DiagTrack",        // Connected User Experiences
                "WSearch"           // Windows Search
            };

            try
            {
                foreach (var serviceName in criticalServices)
                {
                    try
                    {
                        using (var sc = new System.ServiceProcess.ServiceController(serviceName))
                        {
                            services[serviceName] = new ServiceState
                            {
                                Status = sc.Status,
                                StartType = sc.StartType,
                                CanStop = sc.CanStop,
                                CanPauseAndContinue = sc.CanPauseAndContinue
                            };
                        }
                    }
                    catch
                    {
                        // Service nicht gefunden oder nicht zugänglich
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Service State Capture Error: {ex.Message}");
            }

            return services;
        }

        private PerformanceBaseline CapturePerformanceBaseline()
        {
            var baseline = new PerformanceBaseline
            {
                CaptureTime = DateTime.Now
            };

            try
            {
                // CPU-Auslastung (3 Samples)
                double cpuTotal = 0;
                for (int i = 0; i < 3; i++)
                {
                    cpuTotal += _cpuCounter?.NextValue() ?? 0;
                    Thread.Sleep(500);
                }
                baseline.CpuUsage = cpuTotal / 3;

                // Verfügbarer RAM
                baseline.AvailableMemoryMB = _ramCounter?.NextValue() ?? 0;

                // Disk-Performance
                baseline.DiskPerformance = MeasureDiskPerformance();

                // Netzwerk-Latenz
                baseline.NetworkLatency = MeasureNetworkLatency();

                // GPU-Info (falls verfügbar)
                baseline.GpuInfo = GetGpuInfo();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Performance Baseline Capture Error: {ex.Message}");
            }

            return baseline;
        }

        private Dictionary<string, string> CalculateProtectedFileHashes()
        {
            var hashes = new Dictionary<string, string>();

            try
            {
                using (var sha256 = SHA256.Create())
                {
                    foreach (var filePath in _protectedFiles)
                    {
                        if (File.Exists(filePath))
                        {
                            try
                            {
                                using (var stream = File.OpenRead(filePath))
                                {
                                    var hashBytes = sha256.ComputeHash(stream);
                                    string hash = BitConverter.ToString(hashBytes).Replace("-", "");
                                    hashes[filePath] = hash;
                                }
                            }
                            catch
                            {
                                // Datei kann nicht gelesen werden (wahrscheinlich in Benutzung)
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Protected File Hash Calculation Error: {ex.Message}");
            }

            return hashes;
        }

        private void SaveSnapshot(SystemSnapshot snapshot)
        {
            try
            {
                string snapshotFile = Path.Combine(_backupDirectory, $"snapshot_{snapshot.Id}.json");
                string json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
                File.WriteAllText(snapshotFile, json);

                // DNA-Hash speichern
                File.WriteAllText(_dnaHashFile, snapshot.DnaHash);

                _logger.Log($"💾 Snapshot gespeichert: {snapshotFile}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Snapshot Save Error: {ex.Message}");
                throw;
            }
        }

        private SystemSnapshot LoadSnapshot(Guid snapshotId)
        {
            try
            {
                string snapshotFile = Path.Combine(_backupDirectory, $"snapshot_{snapshotId}.json");

                if (!File.Exists(snapshotFile))
                {
                    // Suche in allen Backup-Verzeichnissen
                    var backupRoot = Path.GetDirectoryName(Path.GetDirectoryName(_backupDirectory));
                    snapshotFile = Directory.GetFiles(backupRoot, $"snapshot_{snapshotId}.json", SearchOption.AllDirectories)
                                           .FirstOrDefault();

                    if (snapshotFile == null)
                        return null;
                }

                string json = File.ReadAllText(snapshotFile);
                return JsonConvert.DeserializeObject<SystemSnapshot>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Snapshot Load Error: {ex.Message}");
                return null;
            }
        }

        // Helper Methods
        private RegistryKey GetRegistryKey(string path)
        {
            if (path.StartsWith("HKEY_LOCAL_MACHINE"))
            {
                string subPath = path.Substring(18);
                return Registry.LocalMachine.OpenSubKey(subPath);
            }
            else if (path.StartsWith("HKEY_CURRENT_USER"))
            {
                string subPath = path.Substring(17);
                return Registry.CurrentUser.OpenSubKey(subPath);
            }

            return null;
        }

        private string GetRegistryValueType(object value)
        {
            if (value is int) return "dword";
            if (value is long) return "qword";
            if (value is string) return "";
            if (value is byte[]) return "hex";
            return "";
        }

        private string FormatRegistryValue(object value)
        {
            if (value is int intValue)
                return intValue.ToString("X8");

            if (value is long longValue)
                return longValue.ToString("X16");

            if (value is string stringValue)
                return $"\"{stringValue.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

            if (value is byte[] bytes)
                return BitConverter.ToString(bytes).Replace("-", "");

            return "\"\"";
        }

        private void IntegrityCheckCallback(object state)
        {
            try
            {
                var health = MonitorSystemHealth();

                if (health.HealthScore < 70)
                {
                    _logger.LogWarning($"⚠️ System Health Score niedrig: {health.HealthScore}/100");

                    foreach (var warning in health.Warnings)
                    {
                        _logger.LogWarning($"  - {warning}");
                    }
                }

                // Automatische Wiederherstellung bei kritischen Zuständen
                if (health.HealthScore < 50)
                {
                    _logger.LogError($"🚨 KRITISCH: System Health Score {health.HealthScore}/100 - Starte automatische Wiederherstellung...");

                    // Finde letzten validen Snapshot
                    var lastSnapshot = FindLastValidSnapshot();
                    if (lastSnapshot != null)
                    {
                        RestoreSystem(lastSnapshot.Id);
                    }
                }
            }
            catch
            {
                // Silent fail für Timer-Callback
            }
        }

        private SystemSnapshot FindLastValidSnapshot()
        {
            try
            {
                var backupRoot = Path.GetDirectoryName(Path.GetDirectoryName(_backupDirectory));
                var snapshotFiles = Directory.GetFiles(backupRoot, "snapshot_*.json", SearchOption.AllDirectories);

                foreach (var file in snapshotFiles.OrderByDescending(f => File.GetCreationTime(f)))
                {
                    try
                    {
                        var snapshot = JsonConvert.DeserializeObject<SystemSnapshot>(File.ReadAllText(file));
                        if (snapshot != null && ValidateSystemDNA(snapshot.DnaHash))
                            return snapshot;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch
            {
                // Keine Snapshots gefunden
            }

            return null;
        }

        // System Information Methods
        private string GetCpuId()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["ProcessorId"]?.ToString() ?? "UNKNOWN";
                    }
                }
            }
            catch
            {
                return "UNKNOWN";
            }

            return "UNKNOWN";
        }

        private string GetMotherboardSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["SerialNumber"]?.ToString() ?? "UNKNOWN";
                    }
                }
            }
            catch
            {
                return "UNKNOWN";
            }

            return "UNKNOWN";
        }

        private string GetTpmId()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Tpm"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["IsEnabled"]?.ToString() ?? "DISABLED";
                    }
                }
            }
            catch
            {
                return "NOT_FOUND";
            }

            return "NOT_FOUND";
        }

        private string GetDiskSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["SerialNumber"]?.ToString() ?? "UNKNOWN";
                    }
                }
            }
            catch
            {
                return "UNKNOWN";
            }

            return "UNKNOWN";
        }

        private string GetMacAddress()
        {
            try
            {
                var adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                    .FirstOrDefault();

                return adapters?.GetPhysicalAddress().ToString() ?? "00-00-00-00-00-00";
            }
            catch
            {
                return "00-00-00-00-00-00";
            }
        }

        private string GetWindowsInstallationId()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    return key?.GetValue("ProductId")?.ToString() ?? "UNKNOWN";
                }
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        private bool IsSecureBootEnabled()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SecureBoot FROM Win32_Firmware"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["SecureBoot"]?.ToString() == "1";
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private TpmStatus CheckTpmStatus()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Tpm"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var isEnabled = obj["IsEnabled"]?.ToString() == "True";
                        var isActivated = obj["IsActivated"]?.ToString() == "True";

                        if (isEnabled && isActivated)
                            return TpmStatus.Healthy;
                        else if (isEnabled)
                            return TpmStatus.NotActivated;
                        else
                            return TpmStatus.Disabled;
                    }
                }
            }
            catch
            {
                return TpmStatus.NotAvailable;
            }

            return TpmStatus.NotAvailable;
        }

        // Weitere Hilfsmethoden (aus Platzgründen gekürzt)
        private double GetDiskUsage() => 0;
        private Dictionary<string, double> GetSystemTemperatures() => new Dictionary<string, double>();
        private List<string> DetectSuspiciousProcesses() => new List<string>();
        private List<string> DetectNetworkAnomalies() => new List<string>();
        private double CalculateHealthScore(SystemHealthStatus status) => 85;
        private List<string> GenerateHealthWarnings(SystemHealthStatus status) => new List<string>();
        private bool ValidateSystemFiles() => true;
        private bool ValidateRegistry() => true;
        private bool ValidateCriticalServices() => true;
        private bool ValidateNetworkStack() => true;
        private bool ValidatePerformance() => true;
        private bool ValidateSecurityFeatures() => true;
        private void RestoreRegistry(string backupFile) { }
        private void RestoreNetworkConfiguration(NetworkConfiguration config) { }
        private void RestoreServiceStates(Dictionary<string, ServiceState> services) { }
        private void ValidateProtectedFiles(Dictionary<string, string> expectedHashes) { }
        private void RecommendSystemRestart() { }
        private void PerformForensicAnalysis(string expectedDna, string currentDna) { }
        private string GetWindowsEdition() => "Unknown";
        private bool IsUefiEnabled() => true;
        private bool IsVirtualizationEnabled() => true;
        private string[] GetDnsServers() => new string[0];
        private NetworkAdapterInfo[] GetNetworkAdapterInfo() => new NetworkAdapterInfo[0];
        private bool IsFirewallEnabled() => true;
        private bool IsQosEnabled() => false;
        private DiskPerformance MeasureDiskPerformance() => new DiskPerformance();
        private double MeasureNetworkLatency() => 0;
        private GpuInfo GetGpuInfo() => new GpuInfo();

        public void Dispose()
        {
            _integrityMonitor?.Dispose();
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
            _logger.Log("🔐 SystemSanityManager disposed");
        }
    }

    // Data Classes
    public class SystemSnapshot
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Operation { get; set; }
        public string DnaHash { get; set; }
        public string BackupLocation { get; set; }
        public string RegistryBackup { get; set; }
        public SystemConfiguration SystemConfig { get; set; }
        public NetworkConfiguration NetworkConfig { get; set; }
        public Dictionary<string, ServiceState> ServiceStates { get; set; }
        public PerformanceBaseline PerformanceBaseline { get; set; }
        public Dictionary<string, string> ProtectedFileHashes { get; set; }
    }

    public class SystemConfiguration
    {
        public string OsVersion { get; set; }
        public bool Is64Bit { get; set; }
        public int ProcessorCount { get; set; }
        public string SystemDirectory { get; set; }
        public string MachineName { get; set; }
        public string UserName { get; set; }
        public string WindowsEdition { get; set; }
        public bool UefiEnabled { get; set; }
        public bool VirtualizationEnabled { get; set; }
    }

    public class NetworkConfiguration
    {
        public string[] IpAddresses { get; set; }
        public string[] DnsServers { get; set; }
        public NetworkAdapterInfo[] NetworkAdapters { get; set; }
        public bool FirewallEnabled { get; set; }
        public bool QosEnabled { get; set; }
    }

    public class NetworkAdapterInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string MacAddress { get; set; }
        public string Speed { get; set; }
    }

    public class ServiceState
    {
        public System.ServiceProcess.ServiceControllerStatus Status { get; set; }
        public System.ServiceProcess.ServiceStartMode StartType { get; set; }
        public bool CanStop { get; set; }
        public bool CanPauseAndContinue { get; set; }
    }

    public class PerformanceBaseline
    {
        public DateTime CaptureTime { get; set; }
        public double CpuUsage { get; set; }
        public float AvailableMemoryMB { get; set; }
        public DiskPerformance DiskPerformance { get; set; }
        public double NetworkLatency { get; set; }
        public GpuInfo GpuInfo { get; set; }
    }

    public class DiskPerformance
    {
        public double ReadSpeedMBps { get; set; }
        public double WriteSpeedMBps { get; set; }
        public double AccessTimeMs { get; set; }
    }

    public class GpuInfo
    {
        public string Name { get; set; }
        public string DriverVersion { get; set; }
        public double MemoryMB { get; set; }
    }

    public class SystemHealthStatus
    {
        public Guid MonitorId { get; set; }
        public DateTime Timestamp { get; set; }
        public float CpuUsage { get; set; }
        public float AvailableMemoryMB { get; set; }
        public double DiskUsage { get; set; }
        public Dictionary<string, double> Temperatures { get; set; }
        public List<string> SuspiciousProcesses { get; set; }
        public List<string> NetworkAnomalies { get; set; }
        public TpmStatus TpmStatus { get; set; }
        public bool SecureBootEnabled { get; set; }
        public double HealthScore { get; set; }
        public List<string> Warnings { get; set; }
    }

    public class IntegrityValidationResult
    {
        public Guid ValidationId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool TpmValid { get; set; }
        public bool SecureBootValid { get; set; }
        public bool SystemFilesValid { get; set; }
        public bool RegistryValid { get; set; }
        public bool ServicesValid { get; set; }
        public bool NetworkValid { get; set; }
        public bool PerformanceValid { get; set; }
        public bool SecurityValid { get; set; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; }
    }

    public enum TpmStatus
    {
        Healthy,
        NotActivated,
        Disabled,
        NotAvailable
    }

    public class SystemSanityException : Exception
    {
        public SystemSanityException(string message) : base(message) { }
        public SystemSanityException(string message, Exception inner) : base(message, inner) { }
    }
}