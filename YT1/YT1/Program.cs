using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using FiveMQuantumTweaker2026.Core;
using FiveMQuantumTweaker2026.Security;
using FiveMQuantumTweaker2026.Utils;

namespace FiveMQuantumTweaker2026
{
    /// <summary>
    /// Haupt-Einstiegspunkt der Anwendung
    /// 2026er Quantum-Optimierungstechnologie
    /// </summary>
    internal static class Program
    {
        #region WinAPI Imports für erweiterte Systemprüfungen

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(
            IntPtr processHandle,
            uint desiredAccess,
            out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(
            IntPtr tokenHandle,
            TOKEN_INFORMATION_CLASS tokenInformationClass,
            IntPtr tokenInformation,
            uint tokenInformationLength,
            out uint returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsProcessorFeaturePresent(
            ProcessorFeature processorFeature);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

        private enum TOKEN_INFORMATION_CLASS
        {
            TokenElevation = 20
        }

        private enum ProcessorFeature
        {
            PF_ARM_V8_INSTRUCTIONS_AVAILABLE = 29,
            PF_SSSE3_INSTRUCTIONS_AVAILABLE = 36,
            PF_SSE4_1_INSTRUCTIONS_AVAILABLE = 37,
            PF_SSE4_2_INSTRUCTIONS_AVAILABLE = 38,
            PF_AVX_INSTRUCTIONS_AVAILABLE = 39,
            PF_AVX2_INSTRUCTIONS_AVAILABLE = 40,
            PF_AVX512F_INSTRUCTIONS_AVAILABLE = 41
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_ELEVATION
        {
            public uint TokenIsElevated;
        }

        #endregion

        #region Konstanten und Felder

        private static Mutex _globalMutex;
        private const string MUTEX_NAME = "Global\\FiveMQuantumTweaker2026_8D3F1A9C";
        private static Logger _logger;
        private static bool _isInitialized = false;

        #endregion

        /// <summary>
        /// Haupt-Einstiegspunkt der Anwendung
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // 1. SINGLE-INSTANCE CHECK (Globaler Mutex)
                InitializeSingleInstance();

                // 2. FRÜHZEITIGE SYSTEMVALIDIERUNG
                if (!PerformEarlySystemValidation())
                {
                    ShutdownWithError("Systemanforderungen nicht erfüllt");
                    return;
                }

                // 3. LOGGER INITIALISIERUNG
                InitializeLogger();

                // 4. ADMINISTRATOR-RECHTE PRÜFEN
                if (!IsRunningAsAdministrator())
                {
                    RequestAdministratorElevation();
                    return;
                }

                // 5. QUANTUM SECURITY BOOTSTRAP
                if (!InitializeQuantumSecurity())
                {
                    _logger.Error("Quantum Security Initialisierung fehlgeschlagen");
                    ShutdownWithError("Sicherheitssystem konnte nicht initialisiert werden");
                    return;
                }

                // 6. SYSTEM SNAPSHOT FÜR ROLLBACK
                CreateInitialSystemSnapshot();

                // 7. WPF APPLICATION STARTEN
                StartWpfApplication(args);
            }
            catch (Exception ex)
            {
                HandleCriticalError(ex);
            }
            finally
            {
                CleanupResources();
            }
        }

        #region Initialisierungsmethoden

        /// <summary>
        /// Verhindert mehrere gleichzeitige Instanzen
        /// </summary>
        private static void InitializeSingleInstance()
        {
            try
            {
                _globalMutex = new Mutex(true, MUTEX_NAME, out bool createdNew);

                if (!createdNew)
                {
                    // Applikation läuft bereits
                    MessageBox.Show(
                        "🚫 FiveM Quantum Tweaker 2026 läuft bereits!\n\n" +
                        "Bitte schließen Sie die andere Instanz, bevor Sie eine neue starten.\n" +
                        "Mehrere Instanzen können zu Systemkonflikten führen.",
                        "Instanz-Konflikt - FiveM Quantum Tweaker",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    Environment.Exit(1001);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"🔧 Mutex-Initialisierungsfehler:\n{ex.Message}\n\n" +
                    "Mögliche Lösungen:\n" +
                    "1. Andere Instanzen manuell beenden\n" +
                    "2. Computer neu starten\n" +
                    "3. Antivirus temporär deaktivieren",
                    "System-Konflikt",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Environment.Exit(1002);
            }
        }

        /// <summary>
        /// Initialisiert das Logging-System
        /// </summary>
        private static void InitializeLogger()
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FiveMQuantumTweaker2026",
                    "Logs");

                Directory.CreateDirectory(logPath);

                _logger = new Logger(Path.Combine(logPath, $"tweaker_{DateTime.Now:yyyyMMdd_HHmmss}.log"));
                _logger.Info("=== FiveM Quantum Tweaker 2026 - Start ===");
                _logger.Info($"Version: {Assembly.GetExecutingAssembly().GetName().Version}");
                _logger.Info($"OS: {Environment.OSVersion.VersionString}");
                _logger.Info($"64-Bit: {Environment.Is64BitProcess}");
            }
            catch (Exception ex)
            {
                // Fallback zu EventLog
                try
                {
                    using (EventLog eventLog = new EventLog("Application"))
                    {
                        eventLog.Source = "FiveMQuantumTweaker2026";
                        eventLog.WriteEntry(
                            $"Logger-Init fehlgeschlagen: {ex.Message}",
                            EventLogEntryType.Error,
                            1000);
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Führt frühe Systemvalidierungen durch
        /// </summary>
        private static bool PerformEarlySystemValidation()
        {
            try
            {
                // 1. Windows Version Check (Windows 11/12 2026)
                if (!IsSupportedWindowsVersion())
                {
                    MessageBox.Show(
                        "⚠️ UNTERSTÜTZTE WINDOWS-VERSION ERFORDERLICH\n\n" +
                        "FiveM Quantum Tweaker 2026 benötigt:\n" +
                        "• Windows 11 24H2 oder neuer\n" +
                        "• Windows 12 (alle Versionen)\n" +
                        "• 64-Bit Architektur\n\n" +
                        "Aktuell erkannt: " + Environment.OSVersion.VersionString,
                        "Systemanforderungen nicht erfüllt",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                // 2. CPU-Feature Check
                if (!CheckRequiredCpuFeatures())
                {
                    MessageBox.Show(
                        "⚠️ MODERNE CPU ERFORDERLICH\n\n" +
                        "Die Quantum-Optimierung benötigt:\n" +
                        "• AVX2 Instruktionen (Intel Haswell / AMD Excavator+)\n" +
                        "• SSE4.2 Unterstützung\n" +
                        "• Mindestens 4 physische Kerne\n\n" +
                        "Ihre CPU unterstützt nicht alle erforderlichen Features.",
                        "CPU-Anforderungen nicht erfüllt",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                // 3. RAM Check
                if (!CheckMinimumRam())
                {
                    MessageBox.Show(
                        "⚠️ ZU WENIG ARBEITSSPEICHER\n\n" +
                        "Für optimale Performance benötigen Sie:\n" +
                        "• Mindestens 8GB RAM (16GB empfohlen)\n" +
                        "• DDR4 oder schneller\n\n" +
                        "Bitte erweitern Sie Ihren Arbeitsspeicher.",
                        "Speicheranforderungen nicht erfüllt",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    // Warning only, nicht fatal
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Systemvalidation fehlgeschlagen: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Prüft, ob die App als Administrator läuft
        /// </summary>
        private static bool IsRunningAsAdministrator()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Fordert Administrator-Rechte an
        /// </summary>
        private static void RequestAdministratorElevation()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1))
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"🔐 ADMINISTRATORBERECHTIGUNGEN FEHLGESCHLAGEN\n\n" +
                    $"Fehler: {ex.Message}\n\n" +
                    "Bitte starten Sie das Programm manuell als Administrator:\n" +
                    "1. Rechtsklick auf FiveMQuantumTweaker2026.exe\n" +
                    "2. 'Als Administrator ausführen' wählen",
                    "Berechtigungsfehler",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Environment.Exit(1003);
            }
        }

        /// <summary>
        /// Initialisiert die Quantum-Security-Schicht
        /// </summary>
        private static bool InitializeQuantumSecurity()
        {
            try
            {
                _logger?.Info("Initialisiere Quantum Security Layer...");

                // 1. TPM 3.0 Validierung
                var tpmValidator = new QuantumTPMValidator();
                if (!tpmValidator.ValidateTPM3())
                {
                    _logger?.Warn("TPM 3.0 nicht verfügbar - eingeschränkter Modus");
                    // Nicht fatal, aber Warnung
                }

                // 2. Secure Boot Check
                if (!tpmValidator.IsSecureBootEnabled())
                {
                    _logger?.Warn("Secure Boot nicht aktiviert - Sicherheitseinschränkung");
                    // Nicht fatal
                }

                // 3. System Integrity Guard starten
                var integrityGuard = new SystemIntegrityGuard();
                integrityGuard.StartMonitoring();

                _logger?.Info("Quantum Security Layer erfolgreich initialisiert");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Quantum Security Init fehlgeschlagen: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Erstellt initialen System-Snapshot für Rollback
        /// </summary>
        private static void CreateInitialSystemSnapshot()
        {
            try
            {
                _logger?.Info("Erstelle initialen System-Snapshot...");

                var sanityManager = new SystemSanityManager();
                string snapshotId = sanityManager.CreateSystemSnapshot("PRE_INITIALIZATION");

                _logger?.Info($"System-Snapshot erstellt: {snapshotId}");

                // Snapshot-ID für spätere Verwendung speichern
                AppDomain.CurrentDomain.SetData("InitialSnapshotId", snapshotId);
            }
            catch (Exception ex)
            {
                _logger?.Error($"Snapshot-Erstellung fehlgeschlagen: {ex}");
                // Nicht fatal, aber wichtig für Rollback
            }
        }

        /// <summary>
        /// Startet die WPF-Anwendung
        /// </summary>
        private static void StartWpfApplication(string[] args)
        {
            try
            {
                _logger?.Info("Starte WPF Application Framework...");

                var app = new App();
                app.InitializeComponent();

                // Command Line Arguments verarbeiten
                if (args.Contains("--autostart"))
                {
                    _logger?.Info("Autostart-Modus erkannt");
                    Application.Current.Properties["AutostartMode"] = true;
                }

                if (args.Contains("--minimized"))
                {
                    _logger?.Info("Minimiert-Modus erkannt");
                    Application.Current.Properties["StartMinimized"] = true;
                }

                _isInitialized = true;
                app.Run();
            }
            catch (Exception ex)
            {
                _logger?.Error($"WPF Application Start fehlgeschlagen: {ex}");
                throw;
            }
        }

        #endregion

        #region System-Validierungs-Helper

        private static bool IsSupportedWindowsVersion()
        {
            try
            {
                var version = Environment.OSVersion.Version;

                // Windows 11 (Build 22000+) oder Windows 12 (Build 26000+)
                return (version.Major == 10 && version.Build >= 22000) || // Windows 11
                       (version.Major == 12); // Windows 12
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckRequiredCpuFeatures()
        {
            try
            {
                // AVX2 für Quantum-Berechnungen
                bool hasAvx2 = IsProcessorFeaturePresent(ProcessorFeature.PF_AVX2_INSTRUCTIONS_AVAILABLE);

                // SSE4.2 für moderne Optimierungen
                bool hasSse42 = IsProcessorFeaturePresent(ProcessorFeature.PF_SSE4_2_INSTRUCTIONS_AVAILABLE);

                return hasAvx2 && hasSse42;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckMinimumRam()
        {
            try
            {
                using (var pc = new System.Diagnostics.PerformanceCounter("Memory", "Available Bytes"))
                {
                    float availableRamGB = pc.NextValue() / (1024 * 1024 * 1024);
                    return availableRamGB >= 4.0f; // Mindestens 4GB verfügbar
                }
            }
            catch
            {
                return true; // Bei Fehler annehmen, dass es passt
            }
        }

        #endregion

        #region Fehlerbehandlung und Cleanup

        /// <summary>
        /// Behandelt kritische Fehler
        /// </summary>
        private static void HandleCriticalError(Exception ex)
        {
            try
            {
                string errorMsg = $"🔴 KRITISCHER FEHLER - FiveM Quantum Tweaker 2026\n\n" +
                                $"Fehler: {ex.GetType().Name}\n" +
                                $"Message: {ex.Message}\n\n" +
                                $"StackTrace:\n{ex.StackTrace}\n\n" +
                                $"Bitte melden Sie diesen Fehler mit den Logs im AppData-Ordner.";

                _logger?.Error($"CRITICAL ERROR: {ex}");

                MessageBox.Show(errorMsg,
                    "Kritischer Systemfehler",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // Letzter Versuch
                MessageBox.Show("Ein schwerwiegender Fehler ist aufgetreten. Das Programm wird beendet.",
                    "Fataler Fehler",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Stop);
            }
            finally
            {
                Environment.Exit(9999);
            }
        }

        /// <summary>
        /// Beendet mit einer Fehlermeldung
        /// </summary>
        private static void ShutdownWithError(string message)
        {
            MessageBox.Show(message,
                "Startfehler - FiveM Quantum Tweaker 2026",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            Environment.Exit(1000);
        }

        /// <summary>
        /// Bereinigt Ressourcen
        /// </summary>
        private static void CleanupResources()
        {
            try
            {
                _logger?.Info("Bereinige Ressourcen...");

                // Mutex freigeben
                if (_globalMutex != null)
                {
                    _globalMutex.ReleaseMutex();
                    _globalMutex.Dispose();
                }

                // Logger schließen
                _logger?.Info("=== FiveM Quantum Tweaker 2026 - Ende ===");
                _logger?.Dispose();

                // Rollback durchführen, wenn Initialisierung fehlgeschlagen
                if (!_isInitialized)
                {
                    var sanityManager = new SystemSanityManager();
                    string snapshotId = AppDomain.CurrentDomain.GetData("InitialSnapshotId") as string;

                    if (!string.IsNullOrEmpty(snapshotId))
                    {
                        _logger?.Info($"Führe Rollback durch für Snapshot: {snapshotId}");
                        sanityManager.RestoreSystemSnapshot(snapshotId);
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent cleanup failure
                Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
        }

        #endregion
    }
}