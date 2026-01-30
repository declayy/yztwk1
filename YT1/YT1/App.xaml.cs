using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using FiveMQuantumTweaker2026.Core;
using FiveMQuantumTweaker2026.Security;
using FiveMQuantumTweaker2026.Services;
using FiveMQuantumTweaker2026.Utils;
using FiveMQuantumTweaker2026.Models;

namespace FiveMQuantumTweaker2026
{
    /// <summary>
    /// Haupt-Anwendungsklasse - Quantum Tweaker 2026
    /// </summary>
    public partial class App : Application
    {
        #region Private Felder

        private static readonly Logger _logger = new Logger();
        private QuantumTPMValidator _tpmValidator;
        private SystemIntegrityGuard _integrityGuard;
        private TelemetryService _telemetryService;
        private AutoUpdateService _updateService;
        private PerformanceMonitor _performanceMonitor;
        private SystemSanityManager _sanityManager;

        private Mutex _singleInstanceMutex;
        private const string APP_GUID = "{8F6F0AC4-B9A2-4FDB-9B1B-3E3A7C5D8E9F}";

        private bool _isShuttingDown = false;
        private DateTime _startupTime;
        private MainWindow _mainWindow;

        #endregion

        #region Public Properties

        /// <summary>
        /// Aktuell geladenes Tweak-Profil
        /// </summary>
        public static OptimizationProfile CurrentProfile { get; private set; }

        /// <summary>
        /// Aktueller Systemzustand
        /// </summary>
        public static SystemSnapshot CurrentSystemState { get; private set; }

        /// <summary>
        /// Gibt an, ob die App im Gaming-Modus ist
        /// </summary>
        public static bool IsGamingModeActive { get; private set; }

        /// <summary>
        /// Gibt an, ob Quanten-Tweaks aktiv sind
        /// </summary>
        public static bool AreQuantumTweaksActive { get; private set; }

        /// <summary>
        /// App-Laufzeit
        /// </summary>
        public static TimeSpan Uptime => DateTime.Now - _startupTime;

        #endregion

        #region Application Lifecycle

        /// <summary>
        /// Wird beim Start der Anwendung aufgerufen
        /// </summary>
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                _startupTime = DateTime.Now;
                _logger.Info($"=== FiveM Quantum Tweaker 2026 Startup ===");
                _logger.Info($"Startzeit: {_startupTime:yyyy-MM-dd HH:mm:ss}");
                _logger.Info($"Command Line Args: {string.Join(" ", e.Args)}");

                // 1. Single-Instance Check
                if (!CheckSingleInstance())
                {
                    _logger.Warn("Eine andere Instanz läuft bereits");
                    ShutdownWithMessage("FiveM Quantum Tweaker läuft bereits!\n\nBitte schließen Sie die andere Instanz zuerst.");
                    return;
                }

                // 2. Kritische Systemvalidierung
                if (!await ValidateSystemPrerequisites())
                {
                    _logger.Error("Systemvoraussetzungen nicht erfüllt");
                    return;
                }

                // 3. Security Layer initialisieren
                InitializeSecurityLayer();

                // 4. Services starten
                InitializeServices();

                // 5. System-Baseline erfassen
                await CaptureSystemBaseline();

                // 6. MainWindow erstellen und anzeigen
                CreateAndShowMainWindow(e);

                // 7. Hintergrund-Tasks starten
                StartBackgroundTasks();

                _logger.Info("=== Application Startup Completed ===");
            }
            catch (SecurityException secEx)
            {
                _logger.Error($"Security Exception: {secEx}");
                ShowCriticalError("Sicherheitsverletzung",
                    "Die Anwendung konnte nicht auf erforderliche Systemressourcen zugreifen.\n\n" +
                    $"Fehler: {secEx.Message}\n\n" +
                    "Bitte starten Sie das Programm als Administrator.");
                Shutdown(1001);
            }
            catch (Exception ex)
            {
                _logger.Error($"Kritischer Startup-Fehler: {ex}");
                HandleUnhandledException(ex, "Startup");
                Shutdown(1002);
            }
        }

        /// <summary>
        /// Erstellt und zeigt das Hauptfenster
        /// </summary>
        private void CreateAndShowMainWindow(StartupEventArgs e)
        {
            try
            {
                _mainWindow = new MainWindow();

                // Startup-Parameter verarbeiten
                ProcessStartupArguments(e.Args);

                // Event-Handler registrieren
                _mainWindow.Closing += MainWindow_Closing;

                // Fenster anzeigen basierend auf Parametern
                if (e.Args.Contains("--minimized") || e.Args.Contains("--tray"))
                {
                    _mainWindow.WindowState = WindowState.Minimized;
                    _mainWindow.ShowInTaskbar = false;
                    _mainWindow.Hide();
                }
                else if (e.Args.Contains("--maximized"))
                {
                    _mainWindow.WindowState = WindowState.Maximized;
                    _mainWindow.Show();
                }
                else
                {
                    _mainWindow.Show();
                }

                // Autostart-Modus
                if (e.Args.Contains("--autostart"))
                {
                    _logger.Info("Autostart-Modus aktiviert");
                    Task.Delay(2000).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _mainWindow.StartQuantumOptimization();
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Erstellen des MainWindow: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Verarbeitet Startup-Argumente
        /// </summary>
        private void ProcessStartupArguments(string[] args)
        {
            try
            {
                _logger.Info($"Verarbeite {args.Length} Startup-Argumente");

                // Profil laden
                string profileArg = args.FirstOrDefault(a => a.StartsWith("--profile="));
                if (!string.IsNullOrEmpty(profileArg))
                {
                    string profileName = profileArg.Split('=')[1];
                    _logger.Info($"Lade Profil: {profileName}");
                    // Profil-Logik hier
                }

                // FiveM-Pfad setzen
                string pathArg = args.FirstOrDefault(a => a.StartsWith("--fivem-path="));
                if (!string.IsNullOrEmpty(pathArg))
                {
                    string fivemPath = pathArg.Split('=')[1];
                    if (Directory.Exists(fivemPath))
                    {
                        Properties["FiveMPath"] = fivemPath;
                        _logger.Info($"FiveM-Pfad gesetzt: {fivemPath}");
                    }
                }

                // Debug-Modus
                if (args.Contains("--debug") || args.Contains("--verbose"))
                {
                    _logger.EnableDebugLogging();
                    _logger.Info("Debug-Logging aktiviert");
                }

                // Keine UI
                if (args.Contains("--no-ui") || args.Contains("--console"))
                {
                    _logger.Info("No-UI Mode aktiviert");
                    // Headless-Modus Logik
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Fehler beim Verarbeiten von Startup-Arguments: {ex}");
            }
        }

        /// <summary>
        /// Wird beim Beenden der Anwendung aufgerufen
        /// </summary>
        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                _isShuttingDown = true;
                _logger.Info("=== Application Exit Started ===");

                // 1. Gaming-Modus deaktivieren
                if (IsGamingModeActive)
                {
                    _logger.Info("Deaktiviere Gaming-Modus...");
                    await DeactivateGamingMode();
                }

                // 2. Quantum-Tweaks rückgängig machen
                if (AreQuantumTweaksActive)
                {
                    _logger.Info("Mache Quantum-Tweaks rückgängig...");
                    await RevertQuantumTweaks();
                }

                // 3. Services stoppen
                await ShutdownServices();

                // 4. Systemintegrität prüfen
                await VerifySystemIntegrity();

                // 5. Finalen System-Snapshot erstellen
                await CreateFinalSystemSnapshot();

                // 6. Resourcen bereinigen
                CleanupResources();

                _logger.Info($"Uptime: {Uptime:hh\\:mm\\:ss}");
                _logger.Info("=== Application Exit Completed ===");

                // Logfile schließen
                _logger?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler während Application Exit: {ex}");
                // Im Exit versuchen wir einfach alles zu schließen
            }
            finally
            {
                // Mutex freigeben
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn Windows sich beendet
        /// </summary>
        private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            try
            {
                _logger.Warn($"Windows Session Ending: {e.ReasonSessionEnding}");

                // Bei Shutdown/Logoff: Schnelles Cleanup
                if (e.ReasonSessionEnding == ReasonSessionEnding.Shutdown ||
                    e.ReasonSessionEnding == ReasonSessionEnding.Logoff)
                {
                    _logger.Info("System-Shutdown erkannt - beschleunigtes Cleanup");

                    // Wichtigste Tweaks sofort rückgängig machen
                    Task.Run(async () =>
                    {
                        await QuickRevertCriticalTweaks();
                    }).Wait(5000); // Maximal 5 Sekunden warten
                }

                // Standard-Cancel verhindern
                e.Cancel = false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in SessionEnding: {ex}");
            }
        }

        /// <summary>
        /// Fängt nicht behandelte Exceptions ab
        /// </summary>
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                _logger.Error($"UNHANDLED EXCEPTION: {e.Exception}");

                // Zeige benutzerfreundliche Fehlermeldung
                string errorMessage = $"Ein unerwarteter Fehler ist aufgetreten:\n\n" +
                                    $"{e.Exception.GetType().Name}: {e.Exception.Message}\n\n" +
                                    "Die Anwendung wird versuchen, weiter zu laufen.\n" +
                                    "Details wurden geloggt.";

                MessageBox.Show(errorMessage,
                    "Unerwarteter Fehler - FiveM Quantum Tweaker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // Verhindere, dass die App crasht
                e.Handled = true;

                // Versuche Recovery
                AttemptApplicationRecovery();
            }
            catch (Exception fatalEx)
            {
                // Falls auch das fehlschlägt, sicher beenden
                _logger.Fatal($"FATAL in UnhandledException handler: {fatalEx}");
                Environment.Exit(999);
            }
        }

        #endregion

        #region Initialisierungsmethoden

        /// <summary>
        /// Überprüft, ob bereits eine Instanz läuft
        /// </summary>
        private bool CheckSingleInstance()
        {
            try
            {
                _singleInstanceMutex = new Mutex(true, $"Global\\{APP_GUID}", out bool createdNew);

                if (!createdNew)
                {
                    // Versuche, die bestehende Instanz zu aktivieren
                    ActivateExistingInstance();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"SingleInstance Check fehlgeschlagen: {ex}");
                return true; // Im Fehlerfall weiterlaufen
            }
        }

        /// <summary>
        /// Aktiviert eine bereits laufende Instanz
        /// </summary>
        private void ActivateExistingInstance()
        {
            try
            {
                // Hier könnte man Named Pipes oder andere IPC verwenden
                // Für jetzt einfach eine Meldung anzeigen

                MessageBox.Show(
                    "FiveM Quantum Tweaker 2026 läuft bereits!\n\n" +
                    "Es kann nur eine Instanz gleichzeitig ausgeführt werden.",
                    "Instanz bereits aktiv",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Fehler beim Aktivieren bestehender Instanz: {ex}");
            }
        }

        /// <summary>
        /// Validiert Systemvoraussetzungen
        /// </summary>
        private async Task<bool> ValidateSystemPrerequisites()
        {
            try
            {
                _logger.Info("Validiere Systemvoraussetzungen...");

                var validator = new SystemValidator();

                // 1. Windows Version
                if (!validator.IsWindowsVersionSupported())
                {
                    ShowCriticalError("Nicht unterstützte Windows-Version",
                        "FiveM Quantum Tweaker 2026 benötigt:\n" +
                        "• Windows 11 24H2 oder neuer\n" +
                        "• Windows 12 (alle Versionen)\n" +
                        "• 64-Bit Architektur");
                    return false;
                }

                // 2. Administrator-Rechte
                if (!validator.IsRunningAsAdministrator())
                {
                    ShowCriticalError("Administrator-Rechte erforderlich",
                        "Das Programm benötigt Administrator-Rechte für:\n" +
                        "• Systemoptimierungen\n" +
                        "• Registry-Anpassungen\n" +
                        "• Dienst-Verwaltung\n\n" +
                        "Bitte starten Sie das Programm als Administrator neu.");
                    return false;
                }

                // 3. TPM 2.0/3.0 (optional, aber empfohlen)
                if (!await validator.CheckTpmAvailability())
                {
                    var result = MessageBox.Show(
                        "TPM 2.0/3.0 nicht erkannt oder deaktiviert.\n\n" +
                        "Einige Sicherheitsfunktionen sind nicht verfügbar.\n" +
                        "Möchten Sie trotzdem fortfahren?",
                        "TPM Warnung",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                        return false;
                }

                // 4. Secure Boot (optional)
                if (!validator.IsSecureBootEnabled())
                {
                    _logger.Warn("Secure Boot ist nicht aktiviert");
                    // Nur Warnung, nicht blockierend
                }

                // 5. RAM Check
                if (!validator.HasMinimumRam(8)) // 8GB Minimum
                {
                    var result = MessageBox.Show(
                        "Warnung: Weniger als 8GB RAM erkannt.\n\n" +
                        "Für optimale FiveM-Performance werden 16GB+ empfohlen.\n" +
                        "Möchten Sie trotzdem fortfahren?",
                        "RAM Warnung",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                        return false;
                }

                // 6. FiveM Installation prüfen
                if (!validator.IsFiveMInstalled())
                {
                    _logger.Warn("FiveM Installation nicht gefunden");
                    // Nicht blockierend, kann später installiert werden
                }

                _logger.Info("Systemvalidierung erfolgreich");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler bei Systemvalidierung: {ex}");
                ShowCriticalError("Validierungsfehler",
                    $"Die Systemprüfung ist fehlgeschlagen:\n{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initialisiert die Security Layer
        /// </summary>
        private void InitializeSecurityLayer()
        {
            try
            {
                _logger.Info("Initialisiere Security Layer...");

                // 1. TPM Validator
                _tpmValidator = new QuantumTPMValidator();
                _tpmValidator.Initialize();

                // 2. System Integrity Guard
                _integrityGuard = new SystemIntegrityGuard();
                _integrityGuard.StartMonitoring();

                // 3. Security Policies setzen
                SetSecurityPolicies();

                _logger.Info("Security Layer initialisiert");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler bei Security Initialisierung: {ex}");
                // Nicht fatal, aber Warnung
                MessageBox.Show(
                    "Sicherheitssystem konnte nicht vollständig initialisiert werden.\n" +
                    "Einige Sicherheitsfunktionen sind möglicherweise eingeschränkt.",
                    "Sicherheitswarnung",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Setzt Security Policies
        /// </summary>
        private void SetSecurityPolicies()
        {
            try
            {
                // Deaktiviere bestimmte .NET Features für mehr Sicherheit
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

                // Setze SecurityProtocol auf moderne Standards
                System.Net.ServicePointManager.SecurityProtocol =
                    System.Net.SecurityProtocolType.Tls13 |
                    System.Net.SecurityProtocolType.Tls12;

                // Deaktiviere RC4 und schwache Cipher Suites
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine
                    .OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL", true);

                if (key != null)
                {
                    // ... Sicherheits-relevante Registry-Einstellungen ...
                    key.Close();
                }

                _logger.Info("Security Policies gesetzt");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Fehler beim Setzen von Security Policies: {ex}");
            }
        }

        /// <summary>
        /// Initialisiert die Hintergrund-Services
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                _logger.Info("Initialisiere Services...");

                // 1. Performance Monitor
                _performanceMonitor = new PerformanceMonitor();
                _performanceMonitor.Start();

                // 2. Telemetry Service (anonym)
                _telemetryService = new TelemetryService();
                _telemetryService.Initialize();

                // 3. Auto Update Service
                _updateService = new AutoUpdateService();
                _updateService.CheckForUpdatesOnStartup();

                // 4. System Sanity Manager
                _sanityManager = new SystemSanityManager();

                _logger.Info("Services initialisiert");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler bei Service-Initialisierung: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Erfasst System-Baseline
        /// </summary>
        private async Task CaptureSystemBaseline()
        {
            try
            {
                _logger.Info("Erfasse System-Baseline...");

                CurrentSystemState = await SystemSnapshot.CreateCurrentSnapshotAsync();

                // Snapshot für Rollback speichern
                string snapshotId = _sanityManager.CreateSystemSnapshot("INITIAL_BASELINE");
                CurrentSystemState.SnapshotId = snapshotId;

                _logger.Info($"System-Baseline erfasst: {snapshotId}");
                _logger.Info($"CPU: {CurrentSystemState.CpuUsage}%, RAM: {CurrentSystemState.MemoryUsage}%, GPU: {CurrentSystemState.GpuUsage}%");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Erfassen der Baseline: {ex}");
                // Nicht fatal, aber wichtig für spätere Vergleiche
            }
        }

        /// <summary>
        /// Startet Hintergrund-Tasks
        /// </summary>
        private void StartBackgroundTasks()
        {
            try
            {
                _logger.Info("Starte Hintergrund-Tasks...");

                // 1. Periodische Systemüberwachung
                var monitorTimer = new DispatcherTimer();
                monitorTimer.Interval = TimeSpan.FromSeconds(5);
                monitorTimer.Tick += async (s, e) => await UpdateSystemMetrics();
                monitorTimer.Start();

                // 2. Auto-Save alle 60 Sekunden
                var saveTimer = new DispatcherTimer();
                saveTimer.Interval = TimeSpan.FromMinutes(1);
                saveTimer.Tick += (s, e) => SaveApplicationState();
                saveTimer.Start();

                // 3. Periodische Integritätsprüfung
                var integrityTimer = new DispatcherTimer();
                integrityTimer.Interval = TimeSpan.FromMinutes(5);
                integrityTimer.Tick += async (s, e) => await CheckSystemIntegrity();
                integrityTimer.Start();

                _logger.Info("Hintergrund-Tasks gestartet");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Starten von Hintergrund-Tasks: {ex}");
            }
        }

        #endregion

        #region Service Management

        /// <summary>
        /// Stoppt alle Services
        /// </summary>
        private async Task ShutdownServices()
        {
            try
            {
                _logger.Info("Stoppe Services...");

                var tasks = new List<Task>();

                // Performance Monitor stoppen
                if (_performanceMonitor != null)
                {
                    tasks.Add(Task.Run(() => _performanceMonitor.Stop()));
                }

                // Telemetry Service stoppen
                if (_telemetryService != null)
                {
                    tasks.Add(Task.Run(() => _telemetryService.Shutdown()));
                }

                // Integrity Guard stoppen
                if (_integrityGuard != null)
                {
                    tasks.Add(Task.Run(() => _integrityGuard.StopMonitoring()));
                }

                // Auf alle Tasks warten
                await Task.WhenAll(tasks);

                _logger.Info("Services gestoppt");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Stoppen von Services: {ex}");
            }
        }

        /// <summary>
        /// Aktualisiert System-Metriken
        /// </summary>
        private async Task UpdateSystemMetrics()
        {
            try
            {
                if (_performanceMonitor == null || _isShuttingDown)
                    return;

                var metrics = await _performanceMonitor.GetCurrentMetricsAsync();

                // Update CurrentSystemState
                CurrentSystemState.UpdateFromMetrics(metrics);

                // Event auslösen für UI Updates
                OnSystemMetricsUpdated?.Invoke(this, new SystemMetricsEventArgs(metrics));
            }
            catch (Exception ex)
            {
                _logger.Warn($"Fehler beim Aktualisieren von System-Metriken: {ex}");
            }
        }

        /// <summary>
        /// Prüft Systemintegrität
        /// </summary>
        private async Task CheckSystemIntegrity()
        {
            try
            {
                if (_integrityGuard == null || _isShuttingDown)
                    return;

                var integrityReport = await _integrityGuard.CheckIntegrityAsync();

                if (!integrityReport.IsHealthy)
                {
                    _logger.Warn($"Systemintegritätsprobleme: {string.Join(", ", integrityReport.Issues)}");

                    // Automatische Reparatur versuchen
                    if (integrityReport.CanAutoRepair)
                    {
                        await _integrityGuard.RepairIntegrityAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler bei Integritätsprüfung: {ex}");
            }
        }

        /// <summary>
        /// Speichert Anwendungszustand
        /// </summary>
        private void SaveApplicationState()
        {
            try
            {
                if (_isShuttingDown)
                    return;

                // Speichere aktuelle Einstellungen
                var state = new ApplicationState
                {
                    CurrentProfile = CurrentProfile,
                    LastSaveTime = DateTime.Now,
                    Uptime = Uptime,
                    IsGamingModeActive = IsGamingModeActive,
                    AreQuantumTweaksActive = AreQuantumTweaksActive
                };

                string statePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FiveMQuantumTweaker2026",
                    "state.json");

                System.IO.File.WriteAllText(statePath,
                    Newtonsoft.Json.JsonConvert.SerializeObject(state, Newtonsoft.Json.Formatting.Indented));
            }
            catch (Exception ex)
            {
                _logger.Warn($"Fehler beim Speichern des App-Zustands: {ex}");
            }
        }

        #endregion

        #region Tweak Management

        /// <summary>
        /// Aktiviert Gaming-Modus
        /// </summary>
        public static async Task ActivateGamingMode()
        {
            try
            {
                _logger.Info("Aktiviere Gaming-Modus...");

                var optimizer = new QuantumOptimizer();
                await optimizer.ActivateGamingModeAsync();

                IsGamingModeActive = true;

                _logger.Info("Gaming-Modus aktiviert");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Aktivieren des Gaming-Modus: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Deaktiviert Gaming-Modus
        /// </summary>
        public static async Task DeactivateGamingMode()
        {
            try
            {
                _logger.Info("Deaktiviere Gaming-Modus...");

                var optimizer = new QuantumOptimizer();
                await optimizer.DeactivateGamingModeAsync();

                IsGamingModeActive = false;

                _logger.Info("Gaming-Modus deaktiviert");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Deaktivieren des Gaming-Modus: {ex}");
            }
        }

        /// <summary>
        /// Wendet Quantum-Tweaks an
        /// </summary>
        public static async Task ApplyQuantumTweaks(OptimizationProfile profile)
        {
            try
            {
                _logger.Info($"Wende Quantum-Tweaks an: {profile.ProfileName}");

                CurrentProfile = profile;

                var optimizer = new QuantumOptimizer();
                await optimizer.ApplyQuantumOptimizationsAsync(profile);

                AreQuantumTweaksActive = true;

                _logger.Info("Quantum-Tweaks angewendet");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Anwenden von Quantum-Tweaks: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Macht Quantum-Tweaks rückgängig
        /// </summary>
        public static async Task RevertQuantumTweaks()
        {
            try
            {
                _logger.Info("Mache Quantum-Tweaks rückgängig...");

                var optimizer = new QuantumOptimizer();
                await optimizer.RevertAllOptimizationsAsync();

                AreQuantumTweaksActive = false;
                CurrentProfile = null;

                _logger.Info("Quantum-Tweaks rückgängig gemacht");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Rückgängigmachen von Quantum-Tweaks: {ex}");
            }
        }

        /// <summary>
        /// Macht kritische Tweaks schnell rückgängig (für Shutdown)
        /// </summary>
        private async Task QuickRevertCriticalTweaks()
        {
            try
            {
                _logger.Info("Mache kritische Tweaks schnell rückgängig...");

                var optimizer = new QuantumOptimizer();
                await optimizer.QuickRevertCriticalTweaksAsync();

                _logger.Info("Kritische Tweaks rückgängig gemacht");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler bei Quick-Revert: {ex}");
            }
        }

        #endregion

        #region Error Handling und Recovery

        /// <summary>
        /// Versucht Application Recovery
        /// </summary>
        private void AttemptApplicationRecovery()
        {
            try
            {
                _logger.Info("Versuche Application Recovery...");

                // 1. Services neustarten
                Task.Run(async () =>
                {
                    await ShutdownServices();
                    await Task.Delay(1000);
                    InitializeServices();
                });

                // 2. UI zurücksetzen
                Dispatcher.Invoke(() =>
                {
                    if (_mainWindow != null && _mainWindow.IsLoaded)
                    {
                        _mainWindow.ResetUIState();
                    }
                });

                // 3. Systemintegrität prüfen
                Task.Run(async () =>
                {
                    await CheckSystemIntegrity();
                });

                _logger.Info("Application Recovery versucht");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler bei Recovery: {ex}");
            }
        }

        /// <summary>
        /// Behandelt nicht behandelte Exceptions
        /// </summary>
        private void HandleUnhandledException(Exception ex, string context)
        {
            try
            {
                string errorDetails = $"{context} Error: {ex.GetType().Name}\n" +
                                    $"Message: {ex.Message}\n" +
                                    $"StackTrace: {ex.StackTrace}";

                // In Logdatei schreiben
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FiveMQuantumTweaker2026",
                    "CrashLogs",
                    $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");

                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                System.IO.File.WriteAllText(logPath, errorDetails);

                // Benutzerfreundliche Meldung
                string userMessage = $"Ein schwerwiegender Fehler ist aufgetreten ({context}).\n\n" +
                                   "Die Anwendung muss beendet werden.\n" +
                                   "Fehlerdetails wurden gespeichert:\n" +
                                   logPath;

                MessageBox.Show(userMessage,
                    "Kritischer Fehler - FiveM Quantum Tweaker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception fatalEx)
            {
                // Letzter Versuch
                MessageBox.Show($"FATAL ERROR: {fatalEx.Message}",
                    "Fataler Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);
            }
        }

        /// <summary>
        /// Zeigt kritische Fehlermeldung
        /// </summary>
        private void ShowCriticalError(string title, string message)
        {
            try
            {
                MessageBox.Show(message, title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // Fallback
                Console.WriteLine($"CRITICAL: {title} - {message}");
            }
        }

        /// <summary>
        /// Beendet mit Meldung
        /// </summary>
        private void ShutdownWithMessage(string message)
        {
            try
            {
                MessageBox.Show(message,
                    "FiveM Quantum Tweaker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            finally
            {
                Shutdown(0);
            }
        }

        #endregion

        #region System Integrity

        /// <summary>
        /// Verifiziert Systemintegrität beim Beenden
        /// </summary>
        private async Task VerifySystemIntegrity()
        {
            try
            {
                _logger.Info("Verifiziere Systemintegrität...");

                if (_integrityGuard != null)
                {
                    var report = await _integrityGuard.VerifyFinalIntegrityAsync();

                    if (!report.IsHealthy)
                    {
                        _logger.Warn($"Systemintegritätsprobleme beim Beenden: {string.Join(", ", report.Issues)}");

                        // Automatische Reparatur
                        if (report.CanAutoRepair)
                        {
                            await _integrityGuard.RepairIntegrityAsync();
                        }
                    }
                }

                _logger.Info("Systemintegrität verifiziert");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler bei Integritätsverifikation: {ex}");
            }
        }

        /// <summary>
        /// Erstellt finalen System-Snapshot
        /// </summary>
        private async Task CreateFinalSystemSnapshot()
        {
            try
            {
                _logger.Info("Erstelle finalen System-Snapshot...");

                if (_sanityManager != null)
                {
                    string snapshotId = _sanityManager.CreateSystemSnapshot("FINAL_SHUTDOWN");
                    _logger.Info($"Finaler Snapshot erstellt: {snapshotId}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Erstellen des finalen Snapshots: {ex}");
            }
        }

        #endregion

        #region Event Handler

        /// <summary>
        /// Wird aufgerufen, wenn das MainWindow geschlossen wird
        /// </summary>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_isShuttingDown)
                    return;

                // Frage den Benutzer, ob er wirklich beenden möchte
                if (AreQuantumTweaksActive && !_isShuttingDown)
                {
                    var result = MessageBox.Show(
                        "⚠️ Quantum-Tweaks sind noch aktiv!\n\n" +
                        "Möchten Sie die Tweaks vor dem Beenden rückgängig machen?\n\n" +
                        "• JA: Tweaks werden sicher rückgängig gemacht\n" +
                        "• NEIN: Tweaks bleiben aktiv (nicht empfohlen)\n" +
                        "• ABBRECHEN: App bleibt geöffnet",
                        "Aktive Tweaks erkannt",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Tweaks rückgängig machen und beenden
                        Task.Run(async () =>
                        {
                            await RevertQuantumTweaks();
                            Dispatcher.Invoke(() => Shutdown(0));
                        });

                        e.Cancel = true; // Verhindere sofortiges Schließen
                        return;
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        // Warnung anzeigen
                        MessageBox.Show(
                            "⚠️ WARNUNG: Tweaks bleiben aktiv!\n\n" +
                            "Das System läuft möglicherweise nicht optimal.\n" +
                            "Starten Sie den Tweaker später neu, um die Tweaks rückgängig zu machen.",
                            "Warnung",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true; // Beenden abbrechen
                        return;
                    }
                }

                // Minimieren statt schließen, wenn im Tray-Modus
                if (_mainWindow != null && _mainWindow.IsMinimizedToTray)
                {
                    e.Cancel = true;
                    _mainWindow.Hide();
                    return;
                }

                // Normales Beenden
                _logger.Info("MainWindow Closing - Starte Shutdown");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in MainWindow_Closing: {ex}");
            }
        }

        /// <summary>
        /// Bereinigt Ressourcen
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                _logger.Info("Bereinige Ressourcen...");

                // Timers stoppen
                var field = typeof(DispatcherTimer).GetField("_instance",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                if (field != null)
                {
                    var timer = field.GetValue(null) as DispatcherTimer;
                    timer?.Stop();
                }

                // Event-Handler entfernen
                if (_mainWindow != null)
                {
                    _mainWindow.Closing -= MainWindow_Closing;
                }

                // Services disposen
                _performanceMonitor?.Dispose();
                _telemetryService?.Dispose();
                _updateService?.Dispose();
                _sanityManager?.Dispose();
                _integrityGuard?.Dispose();
                _tpmValidator?.Dispose();

                // GC erzwingen
                GC.Collect();
                GC.WaitForPendingFinalizers();

                _logger.Info("Ressourcen bereinigt");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler bei Resource Cleanup: {ex}");
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Wird ausgelöst, wenn System-Metriken aktualisiert wurden
        /// </summary>
        public static event EventHandler<SystemMetricsEventArgs> OnSystemMetricsUpdated;

        /// <summary>
        /// Wird ausgelöst, wenn Gaming-Modus sich ändert
        /// </summary>
        public static event EventHandler<bool> OnGamingModeChanged;

        /// <summary>
        /// Wird ausgelöst, wenn Quantum-Tweaks sich ändern
        /// </summary>
        public static event EventHandler<bool> OnQuantumTweaksChanged;

        #endregion

        #region Public Methoden

        /// <summary>
        /// Startet FiveM mit optimierten Einstellungen
        /// </summary>
        public static async Task LaunchFiveMOptimized(string fiveMPath = null)
        {
            try
            {
                _logger.Info("Starte FiveM optimiert...");

                var launcher = new FiveMLaunchManager();
                await launcher.LaunchWithOptimizationsAsync(fiveMPath);

                _logger.Info("FiveM gestartet");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Starten von FiveM: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Führt Systembereinigung durch
        /// </summary>
        public static async Task PerformSystemCleanup()
        {
            try
            {
                _logger.Info("Führe Systembereinigung durch...");

                var cleaner = new SystemCleaner();
                await cleaner.PerformIntelligentCleanupAsync();

                _logger.Info("Systembereinigung abgeschlossen");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler bei Systembereinigung: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Stellt das System auf Standard zurück
        /// </summary>
        public static async Task RevertAllTweaks()
        {
            try
            {
                _logger.Info("Stelle alle Tweaks zurück...");

                var reverter = new SystemReverter();
                await reverter.RevertAllChangesAsync();

                AreQuantumTweaksActive = false;
                IsGamingModeActive = false;
                CurrentProfile = null;

                _logger.Info("Alle Tweaks zurückgesetzt");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler beim Zurücksetzen: {ex}");
                throw;
            }
        }

        #endregion
    }

    #region Event Argument Klassen

    /// <summary>
    /// Event Argumente für System-Metriken Updates
    /// </summary>
    public class SystemMetricsEventArgs : EventArgs
    {
        public PerformanceMetrics Metrics { get; }
        public DateTime Timestamp { get; }

        public SystemMetricsEventArgs(PerformanceMetrics metrics)
        {
            Metrics = metrics;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Application State für Persistenz
    /// </summary>
    public class ApplicationState
    {
        public OptimizationProfile CurrentProfile { get; set; }
        public DateTime LastSaveTime { get; set; }
        public TimeSpan Uptime { get; set; }
        public bool IsGamingModeActive { get; set; }
        public bool AreQuantumTweaksActive { get; set; }
    }

    #endregion
}