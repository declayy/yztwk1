using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FiveMQuantumTweaker2026.Utils;

namespace FiveMQuantumTweaker2026.Services
{
    /// <summary>
    /// Automatischer Update-Service mit Hintergrund-Checks und sicheren Updates
    /// </summary>
    public class AutoUpdateService : IDisposable
    {
        private readonly Logger _logger;
        private readonly HttpClient _httpClient;
        private readonly System.Timers.Timer _updateCheckTimer;
        private readonly string _updateManifestUrl;
        private readonly string _downloadDirectory;
        private bool _isChecking;
        private bool _isUpdating;
        private UpdateManifest _cachedManifest;
        private DateTime _lastUpdateCheck;

        // Configuration
        private const int UpdateCheckIntervalHours = 6; // Alle 6 Stunden prüfen
        private const string UpdateBaseUrl = "https://updates.fivemquantum.com";
        private const string ManifestFileName = "update_manifest.json";

        public bool AutoUpdateEnabled { get; set; }
        public bool NotifyOnAvailable { get; set; }
        public bool DownloadInBackground { get; set; }
        public UpdateStatus CurrentStatus { get; set; }
        public Version CurrentVersion { get; private set; }
        public UpdateManifest LatestManifest { get; private set; }

        public event EventHandler<UpdateCheckEventArgs> OnUpdateCheck;
        public event EventHandler<UpdateAvailableEventArgs> OnUpdateAvailable;
        public event EventHandler<UpdateProgressEventArgs> OnUpdateProgress;
        public event EventHandler<UpdateCompletedEventArgs> OnUpdateCompleted;
        public event EventHandler<UpdateErrorEventArgs> OnUpdateError;

        public AutoUpdateService()
        {
            _logger = Logger.CreateLogger();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"FiveMQuantumTweaker/{GetCurrentVersion()}");

            _updateManifestUrl = $"{UpdateBaseUrl}/{ManifestFileName}";
            _downloadDirectory = Path.Combine(Path.GetTempPath(), "FiveMQuantumTweaker", "Updates");

            _updateCheckTimer = new System.Timers.Timer(UpdateCheckIntervalHours * 3600 * 1000);
            _updateCheckTimer.Elapsed += async (s, e) => await CheckForUpdatesAsync(false);

            // Default settings
            AutoUpdateEnabled = true;
            NotifyOnAvailable = true;
            DownloadInBackground = false;
            CurrentStatus = UpdateStatus.Idle;

            // Load settings from configuration
            LoadSettings();

            // Current version
            CurrentVersion = GetCurrentVersion();

            EnsureDownloadDirectory();

            _logger.LogSystemInfo("AutoUpdateService", $"Initialized. Version: {CurrentVersion}");
        }

        private void LoadSettings()
        {
            try
            {
                // Hier könnten Einstellungen aus einer Konfigurationsdatei geladen werden
                // AutoUpdateEnabled = Settings.Default.AutoUpdateEnabled;
                // NotifyOnAvailable = Settings.Default.NotifyOnAvailable;
                // DownloadInBackground = Settings.Default.DownloadInBackground;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to load update settings, using defaults", ex);
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Hier könnten Einstellungen in eine Konfigurationsdatei gespeichert werden
                // Settings.Default.AutoUpdateEnabled = AutoUpdateEnabled;
                // Settings.Default.NotifyOnAvailable = NotifyOnAvailable;
                // Settings.Default.DownloadInBackground = DownloadInBackground;
                // Settings.Default.Save();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to save update settings", ex);
            }
        }

        private void EnsureDownloadDirectory()
        {
            try
            {
                if (!Directory.Exists(_downloadDirectory))
                {
                    Directory.CreateDirectory(_downloadDirectory);
                    _logger.LogDebug($"Created download directory: {_downloadDirectory}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create download directory: {_downloadDirectory}", ex);
            }
        }

        /// <summary>
        /// Startet den automatischen Update-Service
        /// </summary>
        public void Start()
        {
            try
            {
                if (AutoUpdateEnabled)
                {
                    _updateCheckTimer.Start();

                    // Sofortiger Check beim Start (mit Verzögerung)
                    Task.Run(async () =>
                    {
                        await Task.Delay(5000); // 5 Sekunden Verzögerung
                        await CheckForUpdatesAsync(true);
                    });

                    _logger.Log("Auto-update service started");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to start auto-update service", ex);
            }
        }

        /// <summary>
        /// Stoppt den automatischen Update-Service
        /// </summary>
        public void Stop()
        {
            try
            {
                _updateCheckTimer.Stop();
                _logger.Log("Auto-update service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to stop auto-update service", ex);
            }
        }

        /// <summary>
        /// Prüft auf verfügbare Updates
        /// </summary>
        public async Task<UpdateCheckResult> CheckForUpdatesAsync(bool forceCheck = false)
        {
            if (_isChecking && !forceCheck)
            {
                _logger.LogDebug("Update check already in progress");
                return new UpdateCheckResult
                {
                    Success = false,
                    ErrorMessage = "Check already in progress"
                };
            }

            _isChecking = true;
            CurrentStatus = UpdateStatus.Checking;

            var result = new UpdateCheckResult
            {
                CheckTime = DateTime.Now,
                CurrentVersion = CurrentVersion
            };

            try
            {
                _lastUpdateCheck = DateTime.Now;

                OnUpdateCheck?.Invoke(this, new UpdateCheckEventArgs
                {
                    CheckTime = result.CheckTime,
                    IsForced = forceCheck
                });

                _logger.LogDebug($"Checking for updates (force: {forceCheck})");

                // Manifest herunterladen
                string manifestJson;
                try
                {
                    manifestJson = await _httpClient.GetStringAsync(_updateManifestUrl);
                    result.ManifestDownloaded = true;
                }
                catch (HttpRequestException httpEx)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Network error: {httpEx.Message}";
                    result.IsNetworkError = true;

                    OnUpdateError?.Invoke(this, new UpdateErrorEventArgs
                    {
                        ErrorType = UpdateErrorType.Network,
                        ErrorMessage = httpEx.Message,
                        CheckTime = result.CheckTime
                    });

                    _logger.LogWarning($"Network error during update check: {httpEx.Message}");
                    return result;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Failed to download manifest: {ex.Message}";

                    OnUpdateError?.Invoke(this, new UpdateErrorEventArgs
                    {
                        ErrorType = UpdateErrorType.ManifestDownload,
                        ErrorMessage = ex.Message,
                        CheckTime = result.CheckTime
                    });

                    _logger.LogError("Failed to download update manifest", ex);
                    return result;
                }

                // Manifest parsen
                UpdateManifest manifest;
                try
                {
                    manifest = ParseManifest(manifestJson);
                    _cachedManifest = manifest;
                    LatestManifest = manifest;
                    result.Manifest = manifest;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Failed to parse manifest: {ex.Message}";

                    OnUpdateError?.Invoke(this, new UpdateErrorEventArgs
                    {
                        ErrorType = UpdateErrorType.ManifestParse,
                        ErrorMessage = ex.Message,
                        CheckTime = result.CheckTime
                    });

                    _logger.LogError("Failed to parse update manifest", ex);
                    return result;
                }

                // Version vergleichen
                var latestVersion = new Version(manifest.LatestVersion);
                result.LatestVersion = latestVersion;
                result.UpdateAvailable = latestVersion > CurrentVersion;

                if (result.UpdateAvailable)
                {
                    result.IsCriticalUpdate = manifest.IsCriticalUpdate;
                    result.ReleaseNotes = manifest.ReleaseNotes;
                    result.UpdateSizeBytes = manifest.UpdateSize;
                    result.EstimatedDownloadTime = CalculateDownloadTime(manifest.UpdateSize);

                    _logger.Log($"Update available: {CurrentVersion} -> {latestVersion} " +
                               $"(Critical: {manifest.IsCriticalUpdate})");

                    // Event für verfügbares Update
                    OnUpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs
                    {
                        CurrentVersion = CurrentVersion,
                        LatestVersion = latestVersion,
                        IsCritical = manifest.IsCriticalUpdate,
                        ReleaseNotes = manifest.ReleaseNotes,
                        UpdateSize = manifest.UpdateSize,
                        CheckTime = result.CheckTime
                    });

                    // Automatischen Download starten wenn konfiguriert
                    if (DownloadInBackground && AutoUpdateEnabled)
                    {
                        _ = Task.Run(() => DownloadAndApplyUpdateAsync(manifest));
                    }
                }
                else
                {
                    _logger.LogDebug($"No update available. Current: {CurrentVersion}, Latest: {latestVersion}");
                }

                result.Success = true;
                result.CheckCompleted = DateTime.Now;
                result.CheckDuration = result.CheckCompleted - result.CheckTime;

                CurrentStatus = UpdateStatus.Idle;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
                CurrentStatus = UpdateStatus.Error;

                OnUpdateError?.Invoke(this, new UpdateErrorEventArgs
                {
                    ErrorType = UpdateErrorType.Unknown,
                    ErrorMessage = ex.Message,
                    CheckTime = result.CheckTime
                });

                _logger.LogError("Unexpected error during update check", ex);
            }
            finally
            {
                _isChecking = false;
            }

            return result;
        }

        /// <summary>
        /// Lädt und installiert ein Update
        /// </summary>
        public async Task<UpdateResult> DownloadAndApplyUpdateAsync(UpdateManifest manifest = null)
        {
            if (_isUpdating)
            {
                return new UpdateResult
                {
                    Success = false,
                    ErrorMessage = "Update already in progress"
                };
            }

            _isUpdating = true;
            CurrentStatus = UpdateStatus.Downloading;

            var result = new UpdateResult
            {
                StartTime = DateTime.Now,
                CurrentVersion = CurrentVersion
            };

            try
            {
                // Falls kein Manifest angegeben, das gecachte verwenden
                if (manifest == null)
                {
                    if (_cachedManifest == null)
                    {
                        var checkResult = await CheckForUpdatesAsync(true);
                        if (!checkResult.UpdateAvailable)
                        {
                            result.Success = false;
                            result.ErrorMessage = "No update available";
                            return result;
                        }
                        manifest = checkResult.Manifest;
                    }
                    else
                    {
                        manifest = _cachedManifest;
                    }
                }

                result.LatestVersion = new Version(manifest.LatestVersion);
                result.UpdateSizeBytes = manifest.UpdateSize;

                // Download-URL
                var downloadUrl = manifest.DownloadUrl;
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    downloadUrl = $"{UpdateBaseUrl}/FiveMQuantumTweaker_{manifest.LatestVersion}.zip";
                }

                // Dateiname
                var fileName = Path.GetFileName(downloadUrl);
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"update_{manifest.LatestVersion}.zip";
                }

                var downloadPath = Path.Combine(_downloadDirectory, fileName);
                result.DownloadPath = downloadPath;

                _logger.Log($"Starting update download: {manifest.LatestVersion}");
                _logger.Log($"Download URL: {downloadUrl}");
                _logger.Log($"Download path: {downloadPath}");

                // Alte Update-Dateien löschen
                CleanupOldUpdateFiles();

                // Download starten
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? manifest.UpdateSize;
                    result.TotalBytes = totalBytes;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[81920];
                        var totalRead = 0L;
                        var bytesRead = 0;

                        var lastProgressTime = DateTime.Now;
                        var lastProgressBytes = 0L;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;

                            // Progress Event (max. alle 500ms)
                            var now = DateTime.Now;
                            if ((now - lastProgressTime).TotalMilliseconds >= 500 || totalRead == totalBytes)
                            {
                                var progressPercent = totalBytes > 0 ? (double)totalRead / totalBytes * 100 : 0;
                                var bytesPerSecond = totalBytes > 0 ?
                                    (totalRead - lastProgressBytes) / (now - lastProgressTime).TotalSeconds : 0;

                                OnUpdateProgress?.Invoke(this, new UpdateProgressEventArgs
                                {
                                    BytesDownloaded = totalRead,
                                    TotalBytes = totalBytes,
                                    ProgressPercentage = progressPercent,
                                    DownloadSpeedBps = bytesPerSecond,
                                    EstimatedTimeRemaining = totalBytes > 0 ?
                                        TimeSpan.FromSeconds((totalBytes - totalRead) / bytesPerSecond) :
                                        TimeSpan.Zero
                                });

                                lastProgressTime = now;
                                lastProgressBytes = totalRead;
                            }

                            // Cancellation support
                            if (CurrentStatus == UpdateStatus.Cancelled)
                            {
                                result.Success = false;
                                result.ErrorMessage = "Update cancelled by user";
                                result.WasCancelled = true;
                                return result;
                            }
                        }
                    }
                }

                result.DownloadCompleted = DateTime.Now;
                result.DownloadDuration = result.DownloadCompleted - result.StartTime;
                result.DownloadSpeedBps = result.TotalBytes > 0 ?
                    result.TotalBytes / result.DownloadDuration.TotalSeconds : 0;

                // Datei-Integrität prüfen
                if (manifest.FileHash != null && !string.IsNullOrEmpty(manifest.HashAlgorithm))
                {
                    CurrentStatus = UpdateStatus.Verifying;

                    var isValid = await VerifyFileIntegrityAsync(downloadPath, manifest.FileHash, manifest.HashAlgorithm);
                    if (!isValid)
                    {
                        result.Success = false;
                        result.ErrorMessage = "File integrity check failed";
                        result.IntegrityCheckFailed = true;

                        OnUpdateError?.Invoke(this, new UpdateErrorEventArgs
                        {
                            ErrorType = UpdateErrorType.IntegrityCheck,
                            ErrorMessage = "Downloaded file failed integrity check",
                            CheckTime = DateTime.Now
                        });

                        _logger.LogError("Downloaded update file failed integrity check");
                        return result;
                    }

                    result.IntegrityCheckPassed = true;
                }

                // Update anwenden
                CurrentStatus = UpdateStatus.Installing;
                result.InstallationStartTime = DateTime.Now;

                _logger.Log("Starting update installation...");

                // Hier würde die eigentliche Installation stattfinden
                // Für dieses Beispiel simulieren wir eine Installation
                await SimulateInstallationAsync();

                result.InstallationCompleted = DateTime.Now;
                result.InstallationDuration = result.InstallationCompleted - result.InstallationStartTime;

                // Erfolgreich abgeschlossen
                result.Success = true;
                CurrentStatus = UpdateStatus.Completed;

                _logger.Log($"Update successfully installed: {manifest.LatestVersion}");

                // Event für abgeschlossenes Update
                OnUpdateCompleted?.Invoke(this, new UpdateCompletedEventArgs
                {
                    PreviousVersion = CurrentVersion,
                    NewVersion = result.LatestVersion,
                    TotalDuration = DateTime.Now - result.StartTime,
                    UpdateSize = result.TotalBytes,
                    InstallationTime = DateTime.Now
                });

                // Anwendung neu starten (würde normalerweise hier passieren)
                if (manifest.RequiresRestart)
                {
                    _logger.Log("Update requires application restart");
                    result.RestartRequired = true;

                    // Hier würde die Anwendung neu gestartet werden
                    // ScheduleRestart();
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Update failed: {ex.Message}";
                CurrentStatus = UpdateStatus.Error;

                OnUpdateError?.Invoke(this, new UpdateErrorEventArgs
                {
                    ErrorType = UpdateErrorType.Installation,
                    ErrorMessage = ex.Message,
                    CheckTime = DateTime.Now
                });

                _logger.LogError("Update failed", ex);
            }
            finally
            {
                _isUpdating = false;
                if (CurrentStatus != UpdateStatus.Completed && CurrentStatus != UpdateStatus.Error)
                {
                    CurrentStatus = UpdateStatus.Idle;
                }
            }

            return result;
        }

        /// <summary>
        /// Bricht das aktuelle Update ab
        /// </summary>
        public void CancelUpdate()
        {
            if (_isUpdating)
            {
                CurrentStatus = UpdateStatus.Cancelled;
                _logger.Log("Update cancelled by user");
            }
        }

        private UpdateManifest ParseManifest(string json)
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var manifest = System.Text.Json.JsonSerializer.Deserialize<UpdateManifest>(json, options);

                // Validierung
                if (string.IsNullOrEmpty(manifest.LatestVersion))
                    throw new InvalidDataException("Manifest missing LatestVersion");

                if (manifest.UpdateSize <= 0)
                    throw new InvalidDataException("Invalid UpdateSize in manifest");

                // Version validieren
                if (!Version.TryParse(manifest.LatestVersion, out _))
                    throw new InvalidDataException($"Invalid version format: {manifest.LatestVersion}");

                return manifest;
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                throw new InvalidDataException($"Invalid JSON in manifest: {jsonEx.Message}", jsonEx);
            }
        }

        private async Task<bool> VerifyFileIntegrityAsync(string filePath, string expectedHash, string hashAlgorithm)
        {
            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes;

                    switch (hashAlgorithm.ToUpper())
                    {
                        case "SHA256":
                            using (var sha256 = System.Security.Cryptography.SHA256.Create())
                            {
                                hashBytes = sha256.ComputeHash(stream);
                            }
                            break;
                        case "SHA1":
                            using (var sha1 = System.Security.Cryptography.SHA1.Create())
                            {
                                hashBytes = sha1.ComputeHash(stream);
                            }
                            break;
                        case "MD5":
                            using (var md5 = System.Security.Cryptography.MD5.Create())
                            {
                                hashBytes = md5.ComputeHash(stream);
                            }
                            break;
                        default:
                            _logger.LogWarning($"Unsupported hash algorithm: {hashAlgorithm}");
                            return true; // Skip verification if algorithm not supported
                    }

                    var actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to verify file integrity: {ex.Message}");
                return false;
            }
        }

        private async Task SimulateInstallationAsync()
        {
            // Simuliere eine Installation (5 Sekunden mit Progress)
            for (int i = 0; i <= 100; i += 10)
            {
                if (CurrentStatus == UpdateStatus.Cancelled)
                    break;

                await Task.Delay(500);

                OnUpdateProgress?.Invoke(this, new UpdateProgressEventArgs
                {
                    ProgressPercentage = i,
                    InstallationPhase = $"Installing... ({i}%)"
                });
            }
        }

        private TimeSpan CalculateDownloadTime(long fileSize)
        {
            // Schätzt Download-Zeit basierend auf durchschnittlicher Geschwindigkeit
            const double averageSpeedMbps = 50; // 50 Mbps
            const double bytesPerMegabit = 125000; // 1 Mbps = 125,000 Bytes/s

            var seconds = fileSize / (averageSpeedMbps * bytesPerMegabit);
            return TimeSpan.FromSeconds(seconds);
        }

        private void CleanupOldUpdateFiles()
        {
            try
            {
                if (!Directory.Exists(_downloadDirectory))
                    return;

                var files = Directory.GetFiles(_downloadDirectory, "update_*.*");
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogDebug($"Deleted old update file: {file}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to delete old update file: {file}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to cleanup old update files", ex);
            }
        }

        private Version GetCurrentVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version ?? new Version(1, 0, 0, 0);
            }
            catch
            {
                return new Version(1, 0, 0, 0);
            }
        }

        /// <summary>
        /// Setzt Update-Einstellungen
        /// </summary>
        public void UpdateSettings(bool autoUpdateEnabled, bool notifyOnAvailable, bool downloadInBackground)
        {
            AutoUpdateEnabled = autoUpdateEnabled;
            NotifyOnAvailable = notifyOnAvailable;
            DownloadInBackground = downloadInBackground;

            SaveSettings();

            if (AutoUpdateEnabled)
                Start();
            else
                Stop();

            _logger.Log($"Update settings updated: AutoUpdate={autoUpdateEnabled}, " +
                       $"Notify={notifyOnAvailable}, BackgroundDL={downloadInBackground}");
        }

        /// <summary>
        /// Gibt Update-Statistiken zurück
        /// </summary>
        public UpdateStatistics GetStatistics()
        {
            return new UpdateStatistics
            {
                CurrentVersion = CurrentVersion,
                LastUpdateCheck = _lastUpdateCheck,
                UpdateCheckCount = 0, // Könnte gezählt werden
                UpdateSuccessCount = 0, // Könnte gezählt werden
                UpdateErrorCount = 0, // Könnte gezählt werden
                AutoUpdateEnabled = AutoUpdateEnabled,
                Status = CurrentStatus
            };
        }

        public void Dispose()
        {
            try
            {
                Stop();
                _updateCheckTimer?.Dispose();
                _httpClient?.Dispose();

                _logger.Log("AutoUpdateService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error disposing AutoUpdateService", ex);
            }
        }

        // ============================================
        // DATA CLASSES
        // ============================================

        public class UpdateManifest
        {
            public string LatestVersion { get; set; }
            public long UpdateSize { get; set; }
            public string DownloadUrl { get; set; }
            public string ReleaseNotes { get; set; }
            public bool IsCriticalUpdate { get; set; }
            public bool RequiresRestart { get; set; }
            public string FileHash { get; set; }
            public string HashAlgorithm { get; set; }
            public DateTime ReleaseDate { get; set; }
            public string[] SupportedOS { get; set; }
            public string[] Changes { get; set; }
            public string MinimumWindowsVersion { get; set; }
        }

        public class UpdateCheckResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public DateTime CheckTime { get; set; }
            public DateTime CheckCompleted { get; set; }
            public TimeSpan CheckDuration { get; set; }
            public Version CurrentVersion { get; set; }
            public Version LatestVersion { get; set; }
            public bool UpdateAvailable { get; set; }
            public bool IsCriticalUpdate { get; set; }
            public UpdateManifest Manifest { get; set; }
            public bool ManifestDownloaded { get; set; }
            public bool IsNetworkError { get; set; }
            public string ReleaseNotes { get; set; }
            public long UpdateSizeBytes { get; set; }
            public TimeSpan EstimatedDownloadTime { get; set; }
        }

        public class UpdateResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public DateTime StartTime { get; set; }
            public Version CurrentVersion { get; set; }
            public Version LatestVersion { get; set; }
            public long TotalBytes { get; set; }
            public long UpdateSizeBytes { get; set; }
            public string DownloadPath { get; set; }
            public DateTime DownloadCompleted { get; set; }
            public TimeSpan DownloadDuration { get; set; }
            public double DownloadSpeedBps { get; set; }
            public bool IntegrityCheckPassed { get; set; }
            public bool IntegrityCheckFailed { get; set; }
            public DateTime InstallationStartTime { get; set; }
            public DateTime InstallationCompleted { get; set; }
            public TimeSpan InstallationDuration { get; set; }
            public bool RestartRequired { get; set; }
            public bool WasCancelled { get; set; }
        }

        public class UpdateStatistics
        {
            public Version CurrentVersion { get; set; }
            public DateTime LastUpdateCheck { get; set; }
            public int UpdateCheckCount { get; set; }
            public int UpdateSuccessCount { get; set; }
            public int UpdateErrorCount { get; set; }
            public bool AutoUpdateEnabled { get; set; }
            public UpdateStatus Status { get; set; }
        }

        // ============================================
        // EVENT ARGS CLASSES
        // ============================================

        public class UpdateCheckEventArgs : EventArgs
        {
            public DateTime CheckTime { get; set; }
            public bool IsForced { get; set; }
        }

        public class UpdateAvailableEventArgs : EventArgs
        {
            public Version CurrentVersion { get; set; }
            public Version LatestVersion { get; set; }
            public bool IsCritical { get; set; }
            public string ReleaseNotes { get; set; }
            public long UpdateSize { get; set; }
            public DateTime CheckTime { get; set; }
        }

        public class UpdateProgressEventArgs : EventArgs
        {
            public long BytesDownloaded { get; set; }
            public long TotalBytes { get; set; }
            public double ProgressPercentage { get; set; }
            public double DownloadSpeedBps { get; set; }
            public TimeSpan EstimatedTimeRemaining { get; set; }
            public string InstallationPhase { get; set; }
        }

        public class UpdateCompletedEventArgs : EventArgs
        {
            public Version PreviousVersion { get; set; }
            public Version NewVersion { get; set; }
            public TimeSpan TotalDuration { get; set; }
            public long UpdateSize { get; set; }
            public DateTime InstallationTime { get; set; }
        }

        public class UpdateErrorEventArgs : EventArgs
        {
            public UpdateErrorType ErrorType { get; set; }
            public string ErrorMessage { get; set; }
            public DateTime CheckTime { get; set; }
        }

        // ============================================
        // ENUMS
        // ============================================

        public enum UpdateStatus
        {
            Idle,
            Checking,
            Downloading,
            Verifying,
            Installing,
            Completed,
            Error,
            Cancelled
        }

        public enum UpdateErrorType
        {
            Network,
            ManifestDownload,
            ManifestParse,
            Download,
            IntegrityCheck,
            Installation,
            Unknown
        }
    }
}