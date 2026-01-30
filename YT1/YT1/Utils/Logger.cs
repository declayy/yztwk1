using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace FiveMQuantumTweaker2026.Utils
{
    /// <summary>
    /// Advanced Logger mit Thread-Safety, Rotation und Performance-Monitoring
    /// </summary>
    public class Logger : IDisposable
    {
        private static Logger _instance;
        private static readonly object _lock = new object();

        private readonly string _logDirectory;
        private readonly string _logFile;
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB
        private readonly int _maxBackupFiles = 5;
        private readonly bool _enableConsoleOutput;

        private StreamWriter _writer;
        private readonly Queue<string> _logQueue;
        private readonly Thread _logWorker;
        private bool _isRunning;

        // Performance Monitoring
        private long _totalLogs;
        private long _errors;
        private long _warnings;
        private DateTime _startTime;

        // Log Levels
        public enum LogLevel
        {
            Debug,
            Info,
            Success,
            Warning,
            Error,
            Critical
        }

        // Singleton Instance
        public static Logger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new Logger();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Privater Konstruktor für Singleton
        /// </summary>
        private Logger(bool enableConsole = true)
        {
            _enableConsoleOutput = enableConsole;

            // Log-Verzeichnis erstellen
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FiveMQuantumTweaker",
                "Logs"
            );

            Directory.CreateDirectory(_logDirectory);

            // Log-Datei
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logFile = Path.Combine(_logDirectory, $"QuantumTweaker_{timestamp}.log");

            // Queue für Thread-Safety
            _logQueue = new Queue<string>();

            // Worker Thread
            _isRunning = true;
            _logWorker = new Thread(LogWorker)
            {
                Name = "LoggerWorker",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _logWorker.Start();

            // Performance Tracking
            _startTime = DateTime.Now;
            _totalLogs = 0;
            _errors = 0;
            _warnings = 0;

            // Initiale Log-Datei erstellen
            InitializeLogFile();

            LogInternal("Logger initialized successfully", LogLevel.Info, "Logger");
        }

        /// <summary>
        /// Öffentliche Factory-Methode
        /// </summary>
        public static Logger CreateLogger(bool enableConsole = true)
        {
            return new Logger(enableConsole);
        }

        /// <summary>
        /// Loggt eine Nachricht
        /// </summary>
        public void Log(string message,
                       [CallerMemberName] string caller = "",
                       [CallerFilePath] string filePath = "",
                       [CallerLineNumber] int lineNumber = 0)
        {
            LogInternal(message, LogLevel.Info, caller, filePath, lineNumber);
        }

        /// <summary>
        /// Loggt eine Erfolgsmeldung
        /// </summary>
        public void LogSuccess(string message,
                             [CallerMemberName] string caller = "",
                             [CallerFilePath] string filePath = "",
                             [CallerLineNumber] int lineNumber = 0)
        {
            LogInternal(message, LogLevel.Success, caller, filePath, lineNumber);
        }

        /// <summary>
        /// Loggt eine Warnung
        /// </summary>
        public void LogWarning(string message,
                             [CallerMemberName] string caller = "",
                             [CallerFilePath] string filePath = "",
                             [CallerLineNumber] int lineNumber = 0)
        {
            Interlocked.Increment(ref _warnings);
            LogInternal(message, LogLevel.Warning, caller, filePath, lineNumber);
        }

        /// <summary>
        /// Loggt einen Fehler
        /// </summary>
        public void LogError(string message,
                           [CallerMemberName] string caller = "",
                           [CallerFilePath] string filePath = "",
                           [CallerLineNumber] int lineNumber = 0)
        {
            LogError(message, null, caller, filePath, lineNumber);
        }

        /// <summary>
        /// Loggt einen Fehler mit Exception
        /// </summary>
        public void LogError(string message, Exception exception,
                           [CallerMemberName] string caller = "",
                           [CallerFilePath] string filePath = "",
                           [CallerLineNumber] int lineNumber = 0)
        {
            Interlocked.Increment(ref _errors);

            string fullMessage = message;
            if (exception != null)
            {
                fullMessage += $"\nException: {exception.Message}\nStackTrace: {exception.StackTrace}";

                // Innere Exception
                Exception inner = exception.InnerException;
                int depth = 0;
                while (inner != null && depth < 5)
                {
                    fullMessage += $"\nInner Exception [{depth}]: {inner.Message}";
                    inner = inner.InnerException;
                    depth++;
                }
            }

            LogInternal(fullMessage, LogLevel.Error, caller, filePath, lineNumber);
        }

        /// <summary>
        /// Loggt kritische Fehler
        /// </summary>
        public void LogCritical(string message, Exception exception = null,
                              [CallerMemberName] string caller = "",
                              [CallerFilePath] string filePath = "",
                              [CallerLineNumber] int lineNumber = 0)
        {
            Interlocked.Increment(ref _errors);

            string fullMessage = $"🚨 CRITICAL: {message}";
            if (exception != null)
            {
                fullMessage += $"\nCRITICAL EXCEPTION: {exception.GetType().Name}: {exception.Message}";
                fullMessage += $"\nStack Trace:\n{exception.StackTrace}";
            }

            LogInternal(fullMessage, LogLevel.Critical, caller, filePath, lineNumber);

            // Bei kritischen Fehlern sofort in Event Log schreiben
            WriteToEventLog(message, exception);
        }

        /// <summary>
        /// Loggt Debug-Informationen (nur im Debug-Modus)
        /// </summary>
        [Conditional("DEBUG")]
        public void LogDebug(string message,
                           [CallerMemberName] string caller = "",
                           [CallerFilePath] string filePath = "",
                           [CallerLineNumber] int lineNumber = 0)
        {
            LogInternal(message, LogLevel.Debug, caller, filePath, lineNumber);
        }

        /// <summary>
        /// Loggt Performance-Metriken
        /// </summary>
        public void LogPerformance(string operation, TimeSpan duration,
                                 [CallerMemberName] string caller = "",
                                 [CallerFilePath] string filePath = "",
                                 [CallerLineNumber] int lineNumber = 0)
        {
            string message = $"⏱️ Performance: {operation} took {duration.TotalMilliseconds:F2}ms";
            LogInternal(message, LogLevel.Info, caller, filePath, lineNumber);
        }

        /// <summary>
        /// Loggt System-Informationen
        /// </summary>
        public void LogSystemInfo(string component, string info,
                                [CallerMemberName] string caller = "",
                                [CallerFilePath] string filePath = "",
                                [CallerLineNumber] int lineNumber = 0)
        {
            string message = $"🔧 {component}: {info}";
            LogInternal(message, LogLevel.Info, caller, filePath, lineNumber);
        }

        /// <summary>
        /// Gibt Log-Statistiken zurück
        /// </summary>
        public LogStatistics GetStatistics()
        {
            return new LogStatistics
            {
                TotalLogs = _totalLogs,
                Errors = _errors,
                Warnings = _warnings,
                StartTime = _startTime,
                Uptime = DateTime.Now - _startTime,
                LogFile = _logFile,
                QueueSize = _logQueue.Count
            };
        }

        /// <summary>
        /// Interne Log-Methode
        /// </summary>
        private void LogInternal(string message, LogLevel level,
                               string caller, string filePath, int lineNumber)
        {
            try
            {
                Interlocked.Increment(ref _totalLogs);

                // Zeitstempel
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                // Dateiname extrahieren
                string fileName = Path.GetFileName(filePath);

                // Log-Level Symbol
                string levelSymbol = GetLevelSymbol(level);

                // Formatierte Nachricht
                string logMessage = $"[{timestamp}] [{levelSymbol}] [{caller}@{fileName}:{lineNumber}] {message}";

                // In Queue für asynchrone Verarbeitung
                lock (_logQueue)
                {
                    _logQueue.Enqueue(logMessage);
                }

                // Console Output (optional)
                if (_enableConsoleOutput)
                {
                    WriteToConsole(logMessage, level);
                }
            }
            catch (Exception ex)
            {
                // Fallback: Direkt in Event Log
                try
                {
                    EventLog.WriteEntry("FiveMQuantumTweaker",
                        $"Logger Error: {ex.Message}\nOriginal: {message}",
                        EventLogEntryType.Error);
                }
                catch
                {
                    // Ultimate fallback
                    Console.WriteLine($"LOGGER CRASH: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Log Worker Thread
        /// </summary>
        private void LogWorker()
        {
            while (_isRunning || _logQueue.Count > 0)
            {
                try
                {
                    string logMessage = null;

                    lock (_logQueue)
                    {
                        if (_logQueue.Count > 0)
                        {
                            logMessage = _logQueue.Dequeue();
                        }
                    }

                    if (logMessage != null)
                    {
                        WriteToFile(logMessage);
                    }
                    else
                    {
                        Thread.Sleep(10); // 10ms wenn keine Nachrichten
                    }

                    // Datei-Rotation prüfen
                    CheckFileRotation();
                }
                catch (Exception ex)
                {
                    // Logger darf nicht crashen
                    Thread.Sleep(100);

                    // Versuche in Event Log zu schreiben
                    try
                    {
                        EventLog.WriteEntry("FiveMQuantumTweaker",
                            $"Logger Worker Error: {ex.Message}",
                            EventLogEntryType.Error);
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }

            // Finalize
            FinalizeLogFile();
        }

        /// <summary>
        /// Schreibt in Log-Datei
        /// </summary>
        private void WriteToFile(string message)
        {
            try
            {
                if (_writer == null || _writer.BaseStream == null)
                {
                    InitializeLogFile();
                }

                _writer.WriteLine(message);
                _writer.Flush();
            }
            catch (Exception ex)
            {
                // Versuche Datei neu zu öffnen
                try
                {
                    InitializeLogFile();
                    _writer.WriteLine($"[LOGGER RECOVERY] Failed to write: {ex.Message}");
                    _writer.WriteLine(message);
                    _writer.Flush();
                }
                catch
                {
                    // Ultimate fallback
                }
            }
        }

        /// <summary>
        /// Schreibt in Console mit Farben
        /// </summary>
        private void WriteToConsole(string message, LogLevel level)
        {
            try
            {
                ConsoleColor originalColor = Console.ForegroundColor;

                switch (level)
                {
                    case LogLevel.Debug:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                    case LogLevel.Info:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Success:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogLevel.Critical:
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        break;
                }

                Console.WriteLine(message);

                // Reset colors
                Console.ForegroundColor = originalColor;
                Console.BackgroundColor = ConsoleColor.Black;
            }
            catch
            {
                // Console nicht verfügbar
            }
        }

        /// <summary>
        /// Schreibt in Windows Event Log
        /// </summary>
        private void WriteToEventLog(string message, Exception exception = null)
        {
            try
            {
                if (!EventLog.SourceExists("FiveMQuantumTweaker"))
                {
                    EventLog.CreateEventSource("FiveMQuantumTweaker", "Application");
                }

                string eventMessage = message;
                if (exception != null)
                {
                    eventMessage += $"\nException: {exception.Message}";
                }

                EventLog.WriteEntry("FiveMQuantumTweaker", eventMessage,
                    EventLogEntryType.Error, 1001);
            }
            catch
            {
                // Event Log nicht verfügbar
            }
        }

        /// <summary>
        /// Initialisiert Log-Datei
        /// </summary>
        private void InitializeLogFile()
        {
            try
            {
                if (_writer != null)
                {
                    try { _writer.Close(); } catch { }
                    try { _writer.Dispose(); } catch { }
                }

                _writer = new StreamWriter(_logFile, true, Encoding.UTF8)
                {
                    AutoFlush = false
                };

                // Header schreiben
                _writer.WriteLine("=".PadRight(80, '='));
                _writer.WriteLine($"FiveM Quantum Tweaker 2026 - Log File");
                _writer.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _writer.WriteLine($"Version: {GetApplicationVersion()}");
                _writer.WriteLine($"OS: {Environment.OSVersion.VersionString}");
                _writer.WriteLine($"User: {Environment.UserName}");
                _writer.WriteLine($"Machine: {Environment.MachineName}");
                _writer.WriteLine("=".PadRight(80, '='));
                _writer.WriteLine();
                _writer.Flush();
            }
            catch (Exception ex)
            {
                // Fallback zu Event Log
                WriteToEventLog($"Failed to initialize log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Prüft Datei-Rotation
        /// </summary>
        private void CheckFileRotation()
        {
            try
            {
                FileInfo fileInfo = new FileInfo(_logFile);
                if (fileInfo.Exists && fileInfo.Length > _maxFileSize)
                {
                    RotateLogFiles();
                }
            }
            catch
            {
                // Ignore rotation errors
            }
        }

        /// <summary>
        /// Rotiert Log-Dateien
        /// </summary>
        private void RotateLogFiles()
        {
            try
            {
                // Aktuelle Datei schließen
                if (_writer != null)
                {
                    _writer.Close();
                    _writer.Dispose();
                    _writer = null;
                }

                // Bestehende Backups verschieben
                for (int i = _maxBackupFiles - 1; i > 0; i--)
                {
                    string oldFile = Path.Combine(_logDirectory, $"QuantumTweaker_{i}.log");
                    string newFile = Path.Combine(_logDirectory, $"QuantumTweaker_{i + 1}.log");

                    if (File.Exists(oldFile))
                    {
                        if (File.Exists(newFile))
                            File.Delete(newFile);

                        File.Move(oldFile, newFile);
                    }
                }

                // Aktuelle Datei als Backup 1
                string backupFile = Path.Combine(_logDirectory, "QuantumTweaker_1.log");
                if (File.Exists(_logFile))
                {
                    if (File.Exists(backupFile))
                        File.Delete(backupFile);

                    File.Move(_logFile, backupFile);
                }

                // Neue Log-Datei erstellen
                InitializeLogFile();

                LogInternal("Log file rotated successfully", LogLevel.Info, "Logger");
            }
            catch (Exception ex)
            {
                WriteToEventLog($"Log rotation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Finalisiert Log-Datei
        /// </summary>
        private void FinalizeLogFile()
        {
            try
            {
                if (_writer != null)
                {
                    _writer.WriteLine();
                    _writer.WriteLine("=".PadRight(80, '='));
                    _writer.WriteLine($"Log session ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    _writer.WriteLine($"Total logs: {_totalLogs}");
                    _writer.WriteLine($"Errors: {_errors}");
                    _writer.WriteLine($"Warnings: {_warnings}");
                    _writer.WriteLine("=".PadRight(80, '='));

                    _writer.Close();
                    _writer.Dispose();
                    _writer = null;
                }
            }
            catch
            {
                // Ignore
            }
        }

        /// <summary>
        /// Gibt Level-Symbol zurück
        /// </summary>
        private string GetLevelSymbol(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => "🔍",
                LogLevel.Info => "ℹ️",
                LogLevel.Success => "✅",
                LogLevel.Warning => "⚠️",
                LogLevel.Error => "❌",
                LogLevel.Critical => "🚨",
                _ => "📝"
            };
        }

        /// <summary>
        /// Gibt Anwendungsversion zurück
        /// </summary>
        private string GetApplicationVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Stoppt Logger
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _logWorker?.Join(3000); // 3 Sekunden warten

            // Verbleibende Nachrichten schreiben
            while (_logQueue.Count > 0)
            {
                Thread.Sleep(10);
            }
        }

        public void Dispose()
        {
            Stop();
            FinalizeLogFile();

            GC.SuppressFinalize(this);
        }

        ~Logger()
        {
            Dispose();
        }
    }

    /// <summary>
    /// Log-Statistiken
    /// </summary>
    public class LogStatistics
    {
        public long TotalLogs { get; set; }
        public long Errors { get; set; }
        public long Warnings { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Uptime { get; set; }
        public string LogFile { get; set; }
        public int QueueSize { get; set; }

        public override string ToString()
        {
            return $"Logs: {TotalLogs}, Errors: {Errors}, Warnings: {Warnings}, Uptime: {Uptime:hh\\:mm\\:ss}";
        }
    }
}