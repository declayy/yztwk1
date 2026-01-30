using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FiveMQuantumTweaker2026.Models;
using FiveMQuantumTweaker2026.Utils;

namespace FiveMQuantumTweaker2026.Services
{
    /// <summary>
    /// Anonyme Telemetrie-Service für Performance-Daten (Opt-In)
    /// </summary>
    public class TelemetryService : IDisposable
    {
        private readonly Logger _logger;
        private readonly HttpClient _httpClient;
        private readonly System.Timers.Timer _uploadTimer;
        private readonly List<TelemetryData> _telemetryQueue;
        private readonly object _queueLock = new object();
        private bool _isEnabled;
        private bool _isInitialized;
        private string _sessionId;
        private DateTime _sessionStartTime;

        // Configuration
        private const string TelemetryEndpoint = "https://telemetry.fivemquantum.com/api/v1/collect";
        private const int UploadIntervalMinutes = 15; // Alle 15 Minuten
        private const int MaxQueueSize = 1000;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnTelemetryStateChanged?.Invoke(this, new TelemetryStateChangedEventArgs
                    {
                        IsEnabled = value,
                        ChangedTime = DateTime.Now
                    });

                    if (value)
                        StartTelemetry();
                    else
                        StopTelemetry();
                }
            }
        }

        public string UserId { get; private set; }
        public string DeviceId { get; private set; }
        public TelemetryStatistics Statistics { get; private set; }

        public event EventHandler<TelemetryStateChangedEventArgs> OnTelemetryStateChanged;
        public event EventHandler<TelemetryUploadEventArgs> OnTelemetryUpload;

        public TelemetryService()
        {
            _logger = Logger.CreateLogger();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FiveMQuantumTweaker/2026");

            _telemetryQueue = new List<TelemetryData>();
            _uploadTimer = new System.Timers.Timer(UploadIntervalMinutes * 60 * 1000);
            _uploadTimer.Elapsed += OnUploadTimerElapsed;

            Statistics = new TelemetryStatistics();

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                // Session ID generieren
                _sessionId = Guid.NewGuid().ToString();
                _sessionStartTime = DateTime.Now;

                // Device ID generieren/bestimmen
                DeviceId = GetOrCreateDeviceId();

                // User ID (anonym)
                UserId = GenerateAnonymousUserId();

                // Opt-In Status aus Einstellungen laden
                LoadOptInStatus();

                _isInitialized = true;

                _logger.LogSystemInfo("TelemetryService", $"Initialized. Device: {DeviceId}, Session: {_sessionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to initialize telemetry service", ex);
                _isInitialized = false;
            }
        }

        private string GetOrCreateDeviceId()
        {
            try
            {
                // Versuche eine stabile Device ID zu erstellen
                var machineName = Environment.MachineName;
                var userName = Environment.UserName;
                var processorId = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown";

                // Kombiniere zu einem Hash
                var combined = $"{machineName}_{userName}_{processorId}";
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16).ToLower();
                }
            }
            catch
            {
                // Fallback: Zufällige ID
                return $"device_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            }
        }

        private string GenerateAnonymousUserId()
        {
            // Generiere eine anonyme User ID basierend auf Device ID und Zeitstempel
            var timestamp = DateTime.Now.ToString("yyyyMMdd");
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{DeviceId}_{timestamp}"));
                return $"user_{BitConverter.ToString(hash).Replace("-", "").Substring(0, 12).ToLower()}";
            }
        }

        private void LoadOptInStatus()
        {
            try
            {
                // Standardmäßig deaktiviert (Opt-In)
                IsEnabled = false;

                // Hier könnte man eine Konfigurationsdatei lesen
                // IsEnabled = Settings.Default.EnableTelemetry;
            }
            catch
            {
                IsEnabled = false;
            }
        }

        private void StartTelemetry()
        {
            if (!_isInitialized) return;

            try
            {
                _uploadTimer.Start();

                // Session-Start event senden
                var sessionStartData = new TelemetryData
                {
                    EventType = "session_start",
                    Timestamp = DateTime.Now,
                    Data = new Dictionary<string, object>
                    {
                        ["session_id"] = _sessionId,
                        ["app_version"] = GetApplicationVersion(),
                        ["os_version"] = Environment.OSVersion.VersionString,
                        ["is_64bit"] = Environment.Is64BitOperatingSystem,
                        ["processor_count"] = Environment.ProcessorCount
                    }
                };

                EnqueueData(sessionStartData);

                _logger.Log("Telemetry service started (Opt-In)");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to start telemetry", ex);
            }
        }

        private void StopTelemetry()
        {
            try
            {
                _uploadTimer.Stop();

                // Session-End event senden
                var sessionEndData = new TelemetryData
                {
                    EventType = "session_end",
                    Timestamp = DateTime.Now,
                    Data = new Dictionary<string, object>
                    {
                        ["session_id"] = _sessionId,
                        ["session_duration"] = (DateTime.Now - _sessionStartTime).TotalSeconds,
                        ["events_sent"] = Statistics.EventsSent,
                        ["events_failed"] = Statistics.EventsFailed
                    }
                };

                EnqueueData(sessionEndData);

                // Wartezeit für letztes Upload
                Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    await UploadTelemetryData(true); // Force upload
                });

                _logger.Log("Telemetry service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to stop telemetry", ex);
            }
        }

        private async void OnUploadTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            await UploadTelemetryData(false);
        }

        /// <summary>
        /// Sendet Performance-Daten
        /// </summary>
        public void SendPerformanceData(PerformanceMetrics metrics)
        {
            if (!IsEnabled || !_isInitialized) return;

            try
            {
                var performanceData = new TelemetryData
                {
                    EventType = "performance_metrics",
                    Timestamp = DateTime.Now,
                    Data = new Dictionary<string, object>
                    {
                        ["cpu_usage"] = metrics.CpuUsage,
                        ["memory_usage_gb"] = metrics.MemoryUsageGB,
                        ["gpu_usage"] = metrics.GpuUsage,
                        ["fps"] = metrics.Fps,
                        ["ping"] = metrics.Ping,
                        ["disk_usage_percent"] = metrics.DiskUsagePercent,
                        ["network_usage_mbps"] = metrics.NetworkUsageMbps,
                        ["performance_score"] = metrics.PerformanceScore
                    }
                };

                EnqueueData(performanceData);
                Statistics.PerformanceEvents++;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send performance data", ex);
            }
        }

        /// <summary>
        /// Sendet Optimierungs-Ereignis
        /// </summary>
        public void SendOptimizationEvent(string optimizationType, bool success,
            Dictionary<string, object> additionalData = null)
        {
            if (!IsEnabled || !_isInitialized) return;

            try
            {
                var data = new Dictionary<string, object>
                {
                    ["optimization_type"] = optimizationType,
                    ["success"] = success,
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                if (additionalData != null)
                {
                    foreach (var kvp in additionalData)
                    {
                        data[kvp.Key] = kvp.Value;
                    }
                }

                var optimizationData = new TelemetryData
                {
                    EventType = "optimization_applied",
                    Timestamp = DateTime.Now,
                    Data = data
                };

                EnqueueData(optimizationData);
                Statistics.OptimizationEvents++;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send optimization event", ex);
            }
        }

        /// <summary>
        /// Sendet Error-Report (anonymisiert)
        /// </summary>
        public void SendErrorReport(string errorType, string errorMessage,
            string stackTrace = null, Dictionary<string, object> context = null)
        {
            if (!IsEnabled || !_isInitialized) return;

            try
            {
                var data = new Dictionary<string, object>
                {
                    ["error_type"] = errorType,
                    ["error_message"] = AnonymizeErrorMessage(errorMessage),
                    ["app_version"] = GetApplicationVersion(),
                    ["os_version"] = Environment.OSVersion.VersionString
                };

                if (!string.IsNullOrEmpty(stackTrace))
                {
                    data["stack_trace"] = AnonymizeStackTrace(stackTrace);
                }

                if (context != null)
                {
                    foreach (var kvp in context)
                    {
                        data[$"context_{kvp.Key}"] = kvp.Value;
                    }
                }

                var errorData = new TelemetryData
                {
                    EventType = "error_report",
                    Timestamp = DateTime.Now,
                    Data = data
                };

                EnqueueData(errorData);
                Statistics.ErrorEvents++;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send error report", ex);
            }
        }

        /// <summary>
        /// Sendet Usage-Statistiken
        /// </summary>
        public void SendUsageStatistics(string featureName, Dictionary<string, object> usageData = null)
        {
            if (!IsEnabled || !_isInitialized) return;

            try
            {
                var data = new Dictionary<string, object>
                {
                    ["feature"] = featureName,
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                if (usageData != null)
                {
                    foreach (var kvp in usageData)
                    {
                        data[kvp.Key] = kvp.Value;
                    }
                }

                var usageEvent = new TelemetryData
                {
                    EventType = "feature_usage",
                    Timestamp = DateTime.Now,
                    Data = data
                };

                EnqueueData(usageEvent);
                Statistics.UsageEvents++;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send usage statistics", ex);
            }
        }

        /// <summary>
        /// Sendet Hardware-Informationen (einmalig pro Session)
        /// </summary>
        public void SendHardwareInfo(HardwareInfo hardwareInfo)
        {
            if (!IsEnabled || !_isInitialized) return;

            try
            {
                // Nur einmal pro Session senden
                if (Statistics.HardwareInfoSent) return;

                var hardwareData = new TelemetryData
                {
                    EventType = "hardware_info",
                    Timestamp = DateTime.Now,
                    Data = new Dictionary<string, object>
                    {
                        ["cpu_name"] = hardwareInfo.CpuName,
                        ["cpu_cores"] = hardwareInfo.CpuCores,
                        ["cpu_threads"] = hardwareInfo.CpuThreads,
                        ["total_memory_gb"] = hardwareInfo.TotalMemoryGB,
                        ["gpu_name"] = hardwareInfo.GpuName,
                        ["gpu_memory_gb"] = hardwareInfo.GpuMemoryGB,
                        ["is_ssd"] = hardwareInfo.IsSsd,
                        ["disk_size_gb"] = hardwareInfo.DiskSizeGB
                    }
                };

                EnqueueData(hardwareData);
                Statistics.HardwareInfoSent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send hardware info", ex);
            }
        }

        private void EnqueueData(TelemetryData data)
        {
            lock (_queueLock)
            {
                // Basic Validation
                if (data == null || data.Data == null) return;

                // Required fields
                data.SessionId = _sessionId;
                data.UserId = UserId;
                data.DeviceId = DeviceId;
                data.AppVersion = GetApplicationVersion();

                _telemetryQueue.Add(data);
                Statistics.EventsQueued++;

                // Queue Size limit
                if (_telemetryQueue.Count > MaxQueueSize)
                {
                    _telemetryQueue.RemoveRange(0, _telemetryQueue.Count - MaxQueueSize);
                    _logger.LogWarning($"Telemetry queue exceeded max size, old events removed");
                }

                // Trigger upload if queue is large
                if (_telemetryQueue.Count >= 50)
                {
                    Task.Run(async () => await UploadTelemetryData(false));
                }
            }
        }

        private async Task UploadTelemetryData(bool forceUpload)
        {
            if (!IsEnabled || !_isInitialized) return;

            List<TelemetryData> dataToUpload;

            lock (_queueLock)
            {
                if (_telemetryQueue.Count == 0) return;

                // Bei forceUpload alles senden, sonst maximal 100 Events
                dataToUpload = forceUpload
                    ? new List<TelemetryData>(_telemetryQueue)
                    : _telemetryQueue.Take(100).ToList();
            }

            if (dataToUpload.Count == 0) return;

            try
            {
                var uploadStartTime = DateTime.Now;

                // Payload erstellen
                var payload = new TelemetryPayload
                {
                    BatchId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now,
                    Events = dataToUpload
                };

                // Zu JSON serialisieren
                var json = SerializeToJson(payload);

                // HTTP Request
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(TelemetryEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    // Erfolg - Daten aus Queue entfernen
                    lock (_queueLock)
                    {
                        _telemetryQueue.RemoveRange(0, dataToUpload.Count);
                    }

                    Statistics.EventsSent += dataToUpload.Count;
                    Statistics.LastUploadTime = DateTime.Now;
                    Statistics.LastUploadSize = dataToUpload.Count;

                    // Event auslösen
                    OnTelemetryUpload?.Invoke(this, new TelemetryUploadEventArgs
                    {
                        Success = true,
                        EventsCount = dataToUpload.Count,
                        UploadTime = DateTime.Now - uploadStartTime
                    });

                    _logger.LogDebug($"Telemetry upload successful: {dataToUpload.Count} events");
                }
                else
                {
                    Statistics.EventsFailed += dataToUpload.Count;

                    OnTelemetryUpload?.Invoke(this, new TelemetryUploadEventArgs
                    {
                        Success = false,
                        EventsCount = dataToUpload.Count,
                        ErrorMessage = $"HTTP {response.StatusCode}",
                        UploadTime = DateTime.Now - uploadStartTime
                    });

                    _logger.LogWarning($"Telemetry upload failed: {response.StatusCode}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Statistics.EventsFailed += dataToUpload.Count;
                Statistics.NetworkErrors++;

                OnTelemetryUpload?.Invoke(this, new TelemetryUploadEventArgs
                {
                    Success = false,
                    EventsCount = dataToUpload.Count,
                    ErrorMessage = $"Network error: {httpEx.Message}",
                    UploadTime = DateTime.Now
                });

                _logger.LogWarning($"Telemetry network error: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                Statistics.EventsFailed += dataToUpload.Count;

                OnTelemetryUpload?.Invoke(this, new TelemetryUploadEventArgs
                {
                    Success = false,
                    EventsCount = dataToUpload.Count,
                    ErrorMessage = ex.Message,
                    UploadTime = DateTime.Now
                });

                _logger.LogError("Telemetry upload failed", ex);
            }
        }

        private string SerializeToJson(TelemetryPayload payload)
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                return System.Text.Json.JsonSerializer.Serialize(payload, options);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to serialize telemetry data", ex);
                return "{}";
            }
        }

        private string GetApplicationVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        private string AnonymizeErrorMessage(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage)) return errorMessage;

            // Entferne persönliche Informationen (Pfade, Benutzernamen, etc.)
            var anonymized = errorMessage
                .Replace(Environment.UserName, "[USER]")
                .Replace(Environment.MachineName, "[MACHINE]")
                .Replace(@"C:\Users\", @"C:\Users\[USER]\")
                .Replace(@"D:\Users\", @"D:\Users\[USER]\");

            return anonymized;
        }

        private string AnonymizeStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return stackTrace;

            // Entferne persönliche Pfade
            var lines = stackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var anonymizedLines = new List<string>();

            foreach (var line in lines)
            {
                var anonymizedLine = line
                    .Replace(Environment.UserName, "[USER]")
                    .Replace(@"C:\Users\", @"C:\Users\[USER]\");

                anonymizedLines.Add(anonymizedLine);
            }

            return string.Join(Environment.NewLine, anonymizedLines);
        }

        /// <summary>
        /// Löscht alle gespeicherten Telemetrie-Daten
        /// </summary>
        public void ClearAllData()
        {
            lock (_queueLock)
            {
                _telemetryQueue.Clear();
                Statistics.Reset();

                _logger.Log("All telemetry data cleared");
            }
        }

        /// <summary>
        /// Gibt Telemetrie-Statistiken zurück
        /// </summary>
        public TelemetryStatistics GetStatistics()
        {
            lock (_queueLock)
            {
                Statistics.QueueSize = _telemetryQueue.Count;
                Statistics.SessionDuration = DateTime.Now - _sessionStartTime;
                return Statistics.Clone();
            }
        }

        /// <summary>
        /// Setzt Opt-In Status und speichert Einstellung
        /// </summary>
        public void SetOptIn(bool optIn, bool saveSetting = true)
        {
            IsEnabled = optIn;

            if (saveSetting)
            {
                SaveOptInStatus(optIn);
            }
        }

        private void SaveOptInStatus(bool optIn)
        {
            try
            {
                // Hier könnte man die Einstellung in eine Konfigurationsdatei speichern
                // Settings.Default.EnableTelemetry = optIn;
                // Settings.Default.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save telemetry opt-in status", ex);
            }
        }

        public void Dispose()
        {
            try
            {
                StopTelemetry();
                _uploadTimer?.Dispose();
                _httpClient?.Dispose();

                _logger.Log("TelemetryService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error disposing TelemetryService", ex);
            }
        }

        // ============================================
        // DATA CLASSES
        // ============================================

        public class TelemetryData
        {
            public string EventType { get; set; }
            public DateTime Timestamp { get; set; }
            public Dictionary<string, object> Data { get; set; }

            // Metadata
            public string SessionId { get; set; }
            public string UserId { get; set; }
            public string DeviceId { get; set; }
            public string AppVersion { get; set; }
        }

        public class TelemetryPayload
        {
            public string BatchId { get; set; }
            public DateTime Timestamp { get; set; }
            public List<TelemetryData> Events { get; set; }
        }

        public class PerformanceMetrics
        {
            public float CpuUsage { get; set; }
            public float MemoryUsageGB { get; set; }
            public float GpuUsage { get; set; }
            public float Fps { get; set; }
            public int Ping { get; set; }
            public float DiskUsagePercent { get; set; }
            public float NetworkUsageMbps { get; set; }
            public int PerformanceScore { get; set; }
        }

        public class TelemetryStatistics
        {
            public int EventsQueued { get; set; }
            public int EventsSent { get; set; }
            public int EventsFailed { get; set; }
            public int QueueSize { get; set; }
            public int PerformanceEvents { get; set; }
            public int OptimizationEvents { get; set; }
            public int ErrorEvents { get; set; }
            public int UsageEvents { get; set; }
            public int NetworkErrors { get; set; }
            public bool HardwareInfoSent { get; set; }
            public DateTime LastUploadTime { get; set; }
            public int LastUploadSize { get; set; }
            public TimeSpan SessionDuration { get; set; }

            public void Reset()
            {
                EventsQueued = 0;
                EventsSent = 0;
                EventsFailed = 0;
                QueueSize = 0;
                PerformanceEvents = 0;
                OptimizationEvents = 0;
                ErrorEvents = 0;
                UsageEvents = 0;
                NetworkErrors = 0;
                HardwareInfoSent = false;
                LastUploadTime = DateTime.MinValue;
                LastUploadSize = 0;
                SessionDuration = TimeSpan.Zero;
            }

            public TelemetryStatistics Clone()
            {
                return new TelemetryStatistics
                {
                    EventsQueued = EventsQueued,
                    EventsSent = EventsSent,
                    EventsFailed = EventsFailed,
                    QueueSize = QueueSize,
                    PerformanceEvents = PerformanceEvents,
                    OptimizationEvents = OptimizationEvents,
                    ErrorEvents = ErrorEvents,
                    UsageEvents = UsageEvents,
                    NetworkErrors = NetworkErrors,
                    HardwareInfoSent = HardwareInfoSent,
                    LastUploadTime = LastUploadTime,
                    LastUploadSize = LastUploadSize,
                    SessionDuration = SessionDuration
                };
            }
        }

        public class TelemetryStateChangedEventArgs : EventArgs
        {
            public bool IsEnabled { get; set; }
            public DateTime ChangedTime { get; set; }
        }

        public class TelemetryUploadEventArgs : EventArgs
        {
            public bool Success { get; set; }
            public int EventsCount { get; set; }
            public string ErrorMessage { get; set; }
            public TimeSpan UploadTime { get; set; }
        }
    }
}