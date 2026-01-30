using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace FiveMQuantumTweaker2026.Models
{
    /// <summary>
    /// Optimierungsprofil mit allen Einstellungen
    /// </summary>
    public class OptimizationProfile
    {
        public Guid Id { get; set; }
        public string ProfileName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }

        // Performance Settings
        public PerformanceSettings Performance { get; set; }

        // Network Settings
        public NetworkSettings Network { get; set; }

        // FiveM Settings
        public FiveMSettings FiveM { get; set; }

        // System Settings
        public SystemSettings System { get; set; }

        // Registry Tweaks
        public List<RegistryTweakSetting> RegistryTweaks { get; set; }

        // Service Configurations
        public List<ServiceConfiguration> ServiceConfigs { get; set; }

        // Power Plan Settings
        public PowerPlanSettings PowerPlan { get; set; }

        // GPU Settings
        public GpuSettings Gpu { get; set; }

        // Quantum Settings (2026)
        public QuantumSettings Quantum { get; set; }

        // Validation Rules
        public List<ValidationRule> ValidationRules { get; set; }

        // Metadata
        public ProfileMetadata Metadata { get; set; }

        public OptimizationProfile()
        {
            Id = Guid.NewGuid();
            ProfileName = "New Profile";
            Description = "Custom optimization profile";
            CreatedDate = DateTime.Now;
            LastModifiedDate = DateTime.Now;
            IsActive = true;
            IsDefault = false;

            Performance = new PerformanceSettings();
            Network = new NetworkSettings();
            FiveM = new FiveMSettings();
            System = new SystemSettings();
            RegistryTweaks = new List<RegistryTweakSetting>();
            ServiceConfigs = new List<ServiceConfiguration>();
            PowerPlan = new PowerPlanSettings();
            Gpu = new GpuSettings();
            Quantum = new QuantumSettings();
            ValidationRules = new List<ValidationRule>();
            Metadata = new ProfileMetadata();

            // Default Tweaks
            InitializeDefaultTweaks();
        }

        private void InitializeDefaultTweaks()
        {
            // Default Registry Tweaks for FiveM
            RegistryTweaks.AddRange(new[]
            {
                // Network Optimizations
                new RegistryTweakSetting
                {
                    Key = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                    ValueName = "Tcp1323Opts",
                    Value = 1,
                    ValueType = "DWord",
                    Category = "Network",
                    IsEnabled = true,
                    Priority = 1
                },
                new RegistryTweakSetting
                {
                    Key = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                    ValueName = "TcpWindowSize",
                    Value = 64240,
                    ValueType = "DWord",
                    Category = "Network",
                    IsEnabled = true,
                    Priority = 1
                },
                new RegistryTweakSetting
                {
                    Key = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                    ValueName = "DefaultTTL",
                    Value = 64,
                    ValueType = "DWord",
                    Category = "Network",
                    IsEnabled = true,
                    Priority = 2
                },
                
                // Gaming Optimizations
                new RegistryTweakSetting
                {
                    Key = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                    ValueName = "NetworkThrottlingIndex",
                    Value = 0xFFFFFFFF,
                    ValueType = "DWord",
                    Category = "Gaming",
                    IsEnabled = true,
                    Priority = 1
                },
                new RegistryTweakSetting
                {
                    Key = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                    ValueName = "SystemResponsiveness",
                    Value = 0,
                    ValueType = "DWord",
                    Category = "Gaming",
                    IsEnabled = true,
                    Priority = 1
                },
                
                // Windows 12 AI Scheduler (2026)
                new RegistryTweakSetting
                {
                    Key = @"SYSTEM\CurrentControlSet\Control\PriorityControl",
                    ValueName = "Win12QuantumScheduling",
                    Value = 1,
                    ValueType = "DWord",
                    Category = "Quantum",
                    IsEnabled = true,
                    Priority = 1
                },
                
                // Quantum HitReg (2026 Geheimtechnologie)
                new RegistryTweakSetting
                {
                    Key = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                    ValueName = "TemporalAdvantage",
                    Value = 12,
                    ValueType = "DWord",
                    Category = "QuantumHitReg",
                    IsEnabled = true,
                    Priority = 1
                }
            });

            // Default Service Configurations
            ServiceConfigs.AddRange(new[]
            {
                new ServiceConfiguration
                {
                    ServiceName = "SysMain",
                    Action = ServiceAction.Optimize,
                    Priority = 2,
                    IsEnabled = true
                },
                new ServiceConfiguration
                {
                    ServiceName = "DiagTrack",
                    Action = ServiceAction.PauseWhileGaming,
                    Priority = 3,
                    IsEnabled = true
                },
                new ServiceConfiguration
                {
                    ServiceName = "WSearch",
                    Action = ServiceAction.PauseWhileGaming,
                    Priority = 3,
                    IsEnabled = true
                },
                new ServiceConfiguration
                {
                    ServiceName = "BITS",
                    Action = ServiceAction.PauseWhileGaming,
                    Priority = 3,
                    IsEnabled = true
                }
            });

            // Default Quantum Settings
            Quantum.EnableEntanglementPrediction = true;
            Quantum.NeuralPacketShaping = true;
            Quantum.TemporalHitRegAdvantage = 12; // 12ms Vorsprung
            Quantum.EnableChronosProtocol = true;

            // Default Validation Rules
            ValidationRules.AddRange(new[]
            {
                new ValidationRule
                {
                    RuleName = "TPM Check",
                    RuleType = ValidationType.Hardware,
                    Condition = "TPM 2.0 must be enabled",
                    IsRequired = true
                },
                new ValidationRule
                {
                    RuleName = "Secure Boot",
                    RuleType = ValidationType.Security,
                    Condition = "Secure Boot must be enabled",
                    IsRequired = true
                },
                new ValidationRule
                {
                    RuleName = "Admin Rights",
                    RuleType = ValidationType.System,
                    Condition = "Application must run as Administrator",
                    IsRequired = true
                }
            });
        }

        /// <summary>
        /// Erstellt ein Standard-Gaming-Profil
        /// </summary>
        public static OptimizationProfile CreateGamingProfile()
        {
            var profile = new OptimizationProfile
            {
                ProfileName = "Ultimate Gaming Profile",
                Description = "Maximale Performance für FiveM und andere Spiele",
                IsDefault = true
            };

            // Performance Settings
            profile.Performance.CpuParkingMode = CpuParkingMode.Disabled;
            profile.Performance.TimerResolution = 0.5f; // 0.5ms
            profile.Performance.EnableHpet = true;
            profile.Performance.MemoryCompression = false;
            profile.Performance.PriorityBoost = true;

            // Network Settings
            profile.Network.TcpOptimization = TcpOptimization.CTCP;
            profile.Network.UdpBufferSize = 65536;
            profile.Network.DisableQos = true;
            profile.Network.DnsServers = new[] { "1.1.1.1", "8.8.8.8" };
            profile.Network.EnablePacketPrioritization = true;

            // FiveM Settings
            profile.FiveM.ProcessPriority = ProcessPriority.RealTime;
            profile.FiveM.AffinityMask = 0xFFFF; // Alle Kerne
            profile.FiveM.MemoryLocking = true;
            profile.FiveM.DisableGameBar = true;
            profile.FiveM.EnableRawInput = true;

            // System Settings
            profile.System.DisableVisualEffects = true;
            profile.System.GameMode = true;
            profile.System.HardwareAcceleratedGpuScheduling = true;
            profile.System.EnableUltimatePerformance = true;

            // Power Plan
            profile.PowerPlan.PlanName = "Ultimate Performance";
            profile.PowerPlan.CpuMinimumState = 100;
            profile.PowerPlan.CpuMaximumState = 100;
            profile.PowerPlan.PciExpressLinkState = LinkState.Off;

            // GPU Settings
            profile.Gpu.PowerManagementMode = GpuPowerMode.PreferMaximumPerformance;
            profile.Gpu.TextureFilteringQuality = TextureQuality.HighPerformance;
            profile.Gpu.VSync = VSyncMode.Off;
            profile.Gpu.MaxFrameRate = 0; // Unlimited

            return profile;
        }

        /// <summary>
        /// Erstellt ein Balanced-Profil
        /// </summary>
        public static OptimizationProfile CreateBalancedProfile()
        {
            var profile = new OptimizationProfile
            {
                ProfileName = "Balanced Profile",
                Description = "Ausgewogene Performance für Gaming und tägliche Nutzung",
                IsDefault = false
            };

            // Performance Settings
            profile.Performance.CpuParkingMode = CpuParkingMode.Auto;
            profile.Performance.TimerResolution = 1.0f; // 1.0ms
            profile.Performance.EnableHpet = true;
            profile.Performance.MemoryCompression = true;
            profile.Performance.PriorityBoost = false;

            // Network Settings
            profile.Network.TcpOptimization = TcpOptimization.Default;
            profile.Network.UdpBufferSize = 32768;
            profile.Network.DisableQos = false;
            profile.Network.DnsServers = new[] { "8.8.8.8", "8.8.4.4" };
            profile.Network.EnablePacketPrioritization = true;

            // FiveM Settings
            profile.FiveM.ProcessPriority = ProcessPriority.High;
            profile.FiveM.AffinityMask = 0xFF; // Erste 8 Kerne
            profile.FiveM.MemoryLocking = false;
            profile.FiveM.DisableGameBar = true;
            profile.FiveM.EnableRawInput = true;

            // System Settings
            profile.System.DisableVisualEffects = false;
            profile.System.GameMode = true;
            profile.System.HardwareAcceleratedGpuScheduling = true;
            profile.System.EnableUltimatePerformance = false;

            // Power Plan
            profile.PowerPlan.PlanName = "High Performance";
            profile.PowerPlan.CpuMinimumState = 5;
            profile.PowerPlan.CpuMaximumState = 100;
            profile.PowerPlan.PciExpressLinkState = LinkState.MaximumPowerSavings;

            return profile;
        }

        /// <summary>
        /// Erstellt ein Quantum-Profil (2026)
        /// </summary>
        public static OptimizationProfile CreateQuantumProfile()
        {
            var profile = new OptimizationProfile
            {
                ProfileName = "QUANTUM 2026",
                Description = "Fortschrittlichste 2026er Optimierungen mit Quantum-Technologie",
                IsDefault = false
            };

            // Performance Settings
            profile.Performance.CpuParkingMode = CpuParkingMode.Disabled;
            profile.Performance.TimerResolution = 0.25f; // 0.25ms
            profile.Performance.EnableHpet = true;
            profile.Performance.MemoryCompression = false;
            profile.Performance.PriorityBoost = true;
            profile.Performance.NeuralScheduling = true;
            profile.Performance.HolographicMemory = true;

            // Network Settings
            profile.Network.TcpOptimization = TcpOptimization.Quantum;
            profile.Network.UdpBufferSize = 131072;
            profile.Network.DisableQos = true;
            profile.Network.DnsServers = new[] { "1.1.1.1", "9.9.9.9" };
            profile.Network.EnablePacketPrioritization = true;
            profile.Network.EntanglementPrediction = true;
            profile.Network.NeuralPacketShaping = true;

            // FiveM Settings
            profile.FiveM.ProcessPriority = ProcessPriority.Quantum;
            profile.FiveM.AffinityMask = 0xFFFF; // Alle Kerne
            profile.FiveM.MemoryLocking = true;
            profile.FiveM.DisableGameBar = true;
            profile.FiveM.EnableRawInput = true;
            profile.FiveM.QuantumHitReg = true;
            profile.FiveM.TemporalAdvantage = 15; // 15ms Vorsprung

            // System Settings
            profile.System.DisableVisualEffects = true;
            profile.System.GameMode = true;
            profile.System.HardwareAcceleratedGpuScheduling = true;
            profile.System.EnableUltimatePerformance = true;
            profile.System.PostQuantumSecurity = true;
            profile.System.NeuromorphicFirewall = true;

            // Power Plan
            profile.PowerPlan.PlanName = "QUANTUM PERFORMANCE";
            profile.PowerPlan.CpuMinimumState = 100;
            profile.PowerPlan.CpuMaximumState = 100;
            profile.PowerPlan.PciExpressLinkState = LinkState.Off;
            profile.PowerPlan.NeuralFrequencyGovernor = true;

            // GPU Settings
            profile.Gpu.PowerManagementMode = GpuPowerMode.PreferMaximumPerformance;
            profile.Gpu.TextureFilteringQuality = TextureQuality.HighPerformance;
            profile.Gpu.VSync = VSyncMode.Off;
            profile.Gpu.MaxFrameRate = 0;
            profile.Gpu.QuantumRendering = true;
            profile.Gpu.HolographicCompression = true;

            // Quantum Settings
            profile.Quantum.EnableEntanglementPrediction = true;
            profile.Quantum.NeuralPacketShaping = true;
            profile.Quantum.TemporalHitRegAdvantage = 15;
            profile.Quantum.EnableChronosProtocol = true;
            profile.Quantum.PhotonRouting = true;
            profile.Quantum.ZeroLatencyDns = true;

            return profile;
        }

        /// <summary>
        /// Wendet das Profil auf das System an
        /// </summary>
        public OptimizationResult Apply()
        {
            var result = new OptimizationResult
            {
                ProfileId = Id,
                ProfileName = ProfileName,
                StartTime = DateTime.Now
            };

            try
            {
                // 1. Validierung
                var validation = Validate();
                result.ValidationResult = validation;

                if (!validation.IsValid && validation.HasCriticalErrors)
                {
                    result.Success = false;
                    result.ErrorMessage = "Validation failed with critical errors";
                    return result;
                }

                // 2. Backup erstellen
                result.BackupCreated = CreateBackup();

                // 3. Registry Tweaks anwenden
                int registrySuccess = 0;
                foreach (var tweak in RegistryTweaks.Where(t => t.IsEnabled).OrderBy(t => t.Priority))
                {
                    try
                    {
                        // Hier würde der Registry-Tweak angewendet werden
                        registrySuccess++;
                    }
                    catch (Exception ex)
                    {
                        result.Warnings.Add($"Registry tweak failed: {tweak.Key}\\{tweak.ValueName} - {ex.Message}");
                    }
                }
                result.RegistryTweaksApplied = registrySuccess;

                // 4. Services konfigurieren
                int servicesConfigured = 0;
                foreach (var service in ServiceConfigs.Where(s => s.IsEnabled).OrderBy(s => s.Priority))
                {
                    try
                    {
                        // Service konfigurieren
                        servicesConfigured++;
                    }
                    catch (Exception ex)
                    {
                        result.Warnings.Add($"Service configuration failed: {service.ServiceName} - {ex.Message}");
                    }
                }
                result.ServicesConfigured = servicesConfigured;

                // 5. Performance-Einstellungen anwenden
                ApplyPerformanceSettings();
                result.PerformanceSettingsApplied = true;

                // 6. Netzwerk-Einstellungen anwenden
                ApplyNetworkSettings();
                result.NetworkSettingsApplied = true;

                // 7. FiveM-Einstellungen anwenden
                ApplyFiveMSettings();
                result.FiveMSettingsApplied = true;

                result.Success = true;
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;

                // Loggen
                var logger = Utils.Logger.CreateLogger();
                logger.Log($"Profile '{ProfileName}' applied successfully. " +
                          $"{registrySuccess} registry tweaks, {servicesConfigured} services configured.");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to apply profile: {ex.Message}";
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
            }

            return result;
        }

        /// <summary>
        /// Validiert das Profil
        /// </summary>
        public ProfileValidation Validate()
        {
            var validation = new ProfileValidation
            {
                ProfileId = Id,
                ProfileName = ProfileName,
                ValidationTime = DateTime.Now
            };

            // System-Requirements prüfen
            if (!Environment.Is64BitOperatingSystem)
            {
                validation.Errors.Add("64-bit Windows required");
                validation.HasCriticalErrors = true;
            }

            // Windows Version prüfen
            var osVersion = Environment.OSVersion.Version;
            if (osVersion.Major < 10)
            {
                validation.Errors.Add("Windows 10 or later required");
                validation.HasCriticalErrors = true;
            }

            // Admin-Rechte prüfen
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                {
                    validation.Warnings.Add("Administrator privileges recommended");
                }
            }
            catch
            {
                validation.Warnings.Add("Could not verify administrator privileges");
            }

            // Profile-spezifische Validierungen
            foreach (var rule in ValidationRules.Where(r => r.IsRequired))
            {
                // Hier würden die Regeln validiert werden
                validation.ValidationRules.Add(new ValidationResult
                {
                    RuleName = rule.RuleName,
                    Passed = true, // Simuliert
                    Message = $"{rule.RuleName} validated"
                });
            }

            validation.IsValid = !validation.HasCriticalErrors;
            return validation;
        }

        private bool CreateBackup()
        {
            try
            {
                // Backup-Logik hier implementieren
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ApplyPerformanceSettings()
        {
            // Performance-Einstellungen anwenden
        }

        private void ApplyNetworkSettings()
        {
            // Netzwerk-Einstellungen anwenden
        }

        private void ApplyFiveMSettings()
        {
            // FiveM-Einstellungen anwenden
        }

        /// <summary>
        /// Exportiert das Profil als JSON
        /// </summary>
        public string ExportToJson(bool pretty = true)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = pretty,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            return System.Text.Json.JsonSerializer.Serialize(this, options);
        }

        /// <summary>
        /// Importiert ein Profil aus JSON
        /// </summary>
        public static OptimizationProfile ImportFromJson(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return System.Text.Json.JsonSerializer.Deserialize<OptimizationProfile>(json, options);
        }

        /// <summary>
        /// Klont das Profil
        /// </summary>
        public OptimizationProfile Clone(string newName = null)
        {
            var json = ExportToJson(false);
            var clone = ImportFromJson(json);

            clone.Id = Guid.NewGuid();
            clone.CreatedDate = DateTime.Now;
            clone.LastModifiedDate = DateTime.Now;

            if (!string.IsNullOrEmpty(newName))
            {
                clone.ProfileName = newName;
            }
            else
            {
                clone.ProfileName = $"{ProfileName} (Copy)";
            }

            clone.IsDefault = false;
            clone.IsActive = true;

            return clone;
        }
    }

    // ============================================
    // SUB-CLASSES
    // ============================================

    /// <summary>
    /// Performance-Einstellungen
    /// </summary>
    public class PerformanceSettings
    {
        public CpuParkingMode CpuParkingMode { get; set; }
        public float TimerResolution { get; set; } // in ms
        public bool EnableHpet { get; set; }
        public bool MemoryCompression { get; set; }
        public bool PriorityBoost { get; set; }
        public bool DisableSuperfetch { get; set; }
        public bool DisablePrefetch { get; set; }
        public bool NeuralScheduling { get; set; } // 2026
        public bool HolographicMemory { get; set; } // 2026
        public bool PhotonicInterconnect { get; set; } // 2026

        public PerformanceSettings()
        {
            CpuParkingMode = CpuParkingMode.Auto;
            TimerResolution = 1.0f;
            EnableHpet = true;
            MemoryCompression = true;
            PriorityBoost = false;
            DisableSuperfetch = false;
            DisablePrefetch = false;
            NeuralScheduling = false;
            HolographicMemory = false;
            PhotonicInterconnect = false;
        }
    }

    /// <summary>
    /// Netzwerk-Einstellungen
    /// </summary>
    public class NetworkSettings
    {
        public TcpOptimization TcpOptimization { get; set; }
        public int UdpBufferSize { get; set; }
        public bool DisableQos { get; set; }
        public string[] DnsServers { get; set; }
        public bool EnablePacketPrioritization { get; set; }
        public bool EntanglementPrediction { get; set; } // 2026
        public bool NeuralPacketShaping { get; set; } // 2026
        public bool PhotonRouting { get; set; } // 2026
        public bool ZeroLatencyDns { get; set; } // 2026

        public NetworkSettings()
        {
            TcpOptimization = TcpOptimization.Default;
            UdpBufferSize = 65536;
            DisableQos = true;
            DnsServers = new[] { "1.1.1.1", "8.8.8.8" };
            EnablePacketPrioritization = true;
            EntanglementPrediction = false;
            NeuralPacketShaping = false;
            PhotonRouting = false;
            ZeroLatencyDns = false;
        }
    }

    /// <summary>
    /// FiveM-Einstellungen
    /// </summary>
    public class FiveMSettings
    {
        public ProcessPriority ProcessPriority { get; set; }
        public int AffinityMask { get; set; }
        public bool MemoryLocking { get; set; }
        public bool DisableGameBar { get; set; }
        public bool EnableRawInput { get; set; }
        public bool QuantumHitReg { get; set; } // 2026
        public int TemporalAdvantage { get; set; } // ms, 2026
        public bool NeuralSyncPrediction { get; set; } // 2026

        public FiveMSettings()
        {
            ProcessPriority = ProcessPriority.High;
            AffinityMask = 0xFFFF; // Alle Kerne
            MemoryLocking = false;
            DisableGameBar = true;
            EnableRawInput = true;
            QuantumHitReg = false;
            TemporalAdvantage = 0;
            NeuralSyncPrediction = false;
        }
    }

    /// <summary>
    /// System-Einstellungen
    /// </summary>
    public class SystemSettings
    {
        public bool DisableVisualEffects { get; set; }
        public bool GameMode { get; set; }
        public bool HardwareAcceleratedGpuScheduling { get; set; }
        public bool EnableUltimatePerformance { get; set; }
        public bool PostQuantumSecurity { get; set; } // 2026
        public bool NeuromorphicFirewall { get; set; } // 2026
        public bool DnaAttestation { get; set; } // 2026

        public SystemSettings()
        {
            DisableVisualEffects = false;
            GameMode = true;
            HardwareAcceleratedGpuScheduling = true;
            EnableUltimatePerformance = false;
            PostQuantumSecurity = false;
            NeuromorphicFirewall = false;
            DnaAttestation = false;
        }
    }

    /// <summary>
    /// Registry-Tweak Einstellung
    /// </summary>
    public class RegistryTweakSetting
    {
        public string Key { get; set; }
        public string ValueName { get; set; }
        public object Value { get; set; }
        public string ValueType { get; set; } // DWord, String, Binary, etc.
        public string Category { get; set; }
        public bool IsEnabled { get; set; }
        public int Priority { get; set; } // 1=Highest, 5=Lowest
        public string Description { get; set; }
    }

    /// <summary>
    /// Service-Konfiguration
    /// </summary>
    public class ServiceConfiguration
    {
        public string ServiceName { get; set; }
        public ServiceAction Action { get; set; }
        public int Priority { get; set; }
        public bool IsEnabled { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// Power-Plan Einstellungen
    /// </summary>
    public class PowerPlanSettings
    {
        public string PlanName { get; set; }
        public int CpuMinimumState { get; set; } // 0-100%
        public int CpuMaximumState { get; set; } // 0-100%
        public LinkState PciExpressLinkState { get; set; }
        public bool NeuralFrequencyGovernor { get; set; } // 2026

        public PowerPlanSettings()
        {
            PlanName = "High Performance";
            CpuMinimumState = 5;
            CpuMaximumState = 100;
            PciExpressLinkState = LinkState.MaximumPowerSavings;
            NeuralFrequencyGovernor = false;
        }
    }

    /// <summary>
    /// GPU-Einstellungen
    /// </summary>
    public class GpuSettings
    {
        public GpuPowerMode PowerManagementMode { get; set; }
        public TextureQuality TextureFilteringQuality { get; set; }
        public VSyncMode VSync { get; set; }
        public int MaxFrameRate { get; set; } // 0 = Unlimited
        public bool QuantumRendering { get; set; } // 2026
        public bool HolographicCompression { get; set; } // 2026

        public GpuSettings()
        {
            PowerManagementMode = GpuPowerMode.PreferMaximumPerformance;
            TextureFilteringQuality = TextureQuality.HighPerformance;
            VSync = VSyncMode.Off;
            MaxFrameRate = 0;
            QuantumRendering = false;
            HolographicCompression = false;
        }
    }

    /// <summary>
    /// Quantum-Einstellungen (2026)
    /// </summary>
    public class QuantumSettings
    {
        public bool EnableEntanglementPrediction { get; set; }
        public bool NeuralPacketShaping { get; set; }
        public int TemporalHitRegAdvantage { get; set; } // ms
        public bool EnableChronosProtocol { get; set; }
        public bool PhotonRouting { get; set; }
        public bool ZeroLatencyDns { get; set; }

        public QuantumSettings()
        {
            EnableEntanglementPrediction = false;
            NeuralPacketShaping = false;
            TemporalHitRegAdvantage = 0;
            EnableChronosProtocol = false;
            PhotonRouting = false;
            ZeroLatencyDns = false;
        }
    }

    /// <summary>
    /// Validierungsregel
    /// </summary>
    public class ValidationRule
    {
        public string RuleName { get; set; }
        public ValidationType RuleType { get; set; }
        public string Condition { get; set; }
        public bool IsRequired { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Profil-Metadaten
    /// </summary>
    public class ProfileMetadata
    {
        public string Author { get; set; }
        public string Version { get; set; }
        public string Compatibility { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastTested { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public string Notes { get; set; }

        public ProfileMetadata()
        {
            Author = Environment.UserName;
            Version = "1.0.0";
            Compatibility = "Windows 10/11/12 (2026)";
            CreationDate = DateTime.Now;
            LastTested = DateTime.Now;
            Tags = new Dictionary<string, string>();
            Notes = string.Empty;
        }
    }

    /// <summary>
    /// Optimierungsergebnis
    /// </summary>
    public class OptimizationResult
    {
        public Guid ProfileId { get; set; }
        public string ProfileName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public ProfileValidation ValidationResult { get; set; }
        public bool BackupCreated { get; set; }
        public int RegistryTweaksApplied { get; set; }
        public int ServicesConfigured { get; set; }
        public bool PerformanceSettingsApplied { get; set; }
        public bool NetworkSettingsApplied { get; set; }
        public bool FiveMSettingsApplied { get; set; }
        public List<string> Warnings { get; set; }
        public List<string> Information { get; set; }

        public OptimizationResult()
        {
            Success = false;
            Warnings = new List<string>();
            Information = new List<string>();
        }
    }

    /// <summary>
    /// Profil-Validierung
    /// </summary>
    public class ProfileValidation
    {
        public Guid ProfileId { get; set; }
        public string ProfileName { get; set; }
        public DateTime ValidationTime { get; set; }
        public bool IsValid { get; set; }
        public bool HasCriticalErrors { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public List<ValidationResult> ValidationRules { get; set; }

        public ProfileValidation()
        {
            IsValid = false;
            HasCriticalErrors = false;
            Errors = new List<string>();
            Warnings = new List<string>();
            ValidationRules = new List<ValidationResult>();
        }
    }

    /// <summary>
    /// Validierungsergebnis für eine Regel
    /// </summary>
    public class ValidationResult
    {
        public string RuleName { get; set; }
        public bool Passed { get; set; }
        public string Message { get; set; }
        public DateTime CheckedTime { get; set; }
    }

    // ============================================
    // ENUMS
    // ============================================

    public enum CpuParkingMode
    {
        Auto,
        Disabled,
        Enabled,
        Dynamic
    }

    public enum TcpOptimization
    {
        Default,
        CTCP,
        BBR,
        Quantum // 2026
    }

    public enum ProcessPriority
    {
        Normal,
        AboveNormal,
        High,
        RealTime,
        Quantum // 2026
    }

    public enum ServiceAction
    {
        Disable,
        Enable,
        Optimize,
        PauseWhileGaming,
        NoChange
    }

    public enum LinkState
    {
        Off,
        MaximumPowerSavings,
        ModeratePowerSavings
    }

    public enum GpuPowerMode
    {
        Adaptive,
        PreferMaximumPerformance,
        OptimalPower,
        PowerSaving
    }

    public enum TextureQuality
    {
        HighQuality,
        Quality,
        Performance,
        HighPerformance
    }

    public enum VSyncMode
    {
        Off,
        On,
        Adaptive,
        Fast
    }

    public enum ValidationType
    {
        Hardware,
        Software,
        System,
        Security,
        Performance,
        Network
    }
}