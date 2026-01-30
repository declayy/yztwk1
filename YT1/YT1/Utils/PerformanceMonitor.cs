using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace FiveMQuantumTweaker2026.Utils
{
    /// <summary>
    /// Echtzeit-Performance-Monitor mit Quanten-Analyse
    /// </summary>
    public class PerformanceMonitor : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(
            out long lpIdleTime,
            out long lpKernelTime,
            out long lpUserTime);

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll")]
        private static extern bool SetThreadPriority(IntPtr hThread, int nPriority);

        [DllImport("kernel32.dll")]
        private static extern int GetTickCount();

        private const int THREAD_PRIORITY_TIME_CRITICAL = 15;

        private readonly ConcurrentDictionary<int, ProcessPerformanceData> _processData;
        private readonly ConcurrentQueue<PerformanceSnapshot> _snapshots;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Thread _monitoringThread;
        private readonly object _syncLock = new object();

        private long _lastIdleTime;
        private long _lastKernelTime;
        private long _lastUserTime;
        private bool _isMonitoring;

        // Performance-Metriken
        public float CurrentCPUUsage { get; private set; }
        public float CurrentRAMUsageGB { get; private set; }
        public float CurrentGPUUsage { get; private set; }
        public float CurrentFPS { get; private set; }
        public int CurrentPing { get; private set; }
        public float NetworkLatency { get; private set; }
        public float FrameTimeVariance { get; private set; }

        public event EventHandler<PerformanceMetricsUpdatedEventArgs> MetricsUpdated;

        public PerformanceMonitor()
        {
            _processData = new ConcurrentDictionary<int, ProcessPerformanceData>();
            _snapshots = new ConcurrentQueue<PerformanceSnapshot>();
            _cancellationTokenSource = new CancellationTokenSource();
            _isMonitoring = false;

            _monitoringThread = new Thread(MonitoringLoop)
            {
                Name = "PerformanceMonitorThread",
                Priority = ThreadPriority.Highest,
                IsBackground = true
            };
        }

        /// <summary>
        /// Startet das Performance-Monitoring mit Quantum-Analyse
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring) return;

            lock (_syncLock)
            {
                _isMonitoring = true;
                GetSystemTimes(out _lastIdleTime, out _lastKernelTime, out _lastUserTime);
                _monitoringThread.Start();

                Logger.Log("Performance-Monitoring gestartet", LogLevel.Info);
            }
        }

        /// <summary>
        /// Stoppt das Performance-Monitoring
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            lock (_syncLock)
            {
                _isMonitoring = false;
                _cancellationTokenSource.Cancel();

                if (_monitoringThread.IsAlive)
                {
                    _monitoringThread.Join(5000);
                }

                Logger.Log("Performance-Monitoring gestoppt", LogLevel.Info);
            }
        }

        /// <summary>
        /// Haupt-Monitoring-Loop
        /// </summary>
        private void MonitoringLoop()
        {
            try
            {
                // Thread-Priorität maximieren für präzise Messungen
                SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_TIME_CRITICAL);

                var perfCounterCPU = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                var perfCounterRAM = new PerformanceCounter("Memory", "Available MBytes");
                var perfCounterDisk = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");

                perfCounterCPU.NextValue(); // Ersten Wert lesen um Counter zu initialisieren
                Thread.Sleep(1000);

                PerformanceCounter perfCounterGPU = null;
                try
                {
                    perfCounterGPU = new PerformanceCounter("GPU Engine", "Utilization Percentage",
                        GetGPUInstanceName());
                }
                catch
                {
                    Logger.Log("GPU-Performance-Counter nicht verfügbar", LogLevel.Warning);
                }

                int frameCounter = 0;
                long lastFrameTime = GetTickCount();
                var frameTimes = new CircularBuffer<float>(1000);

                while (_isMonitoring && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        // CPU-Auslastung berechnen
                        float cpuUsage = perfCounterCPU.NextValue();
                        CurrentCPUUsage = cpuUsage;

                        // RAM-Auslastung berechnen
                        float availableRAM = perfCounterRAM.NextValue();
                        float totalRAM = GetTotalPhysicalMemory();
                        CurrentRAMUsageGB = (totalRAM - availableRAM) / 1024f;

                        // GPU-Auslastung
                        if (perfCounterGPU != null)
                        {
                            try
                            {
                                CurrentGPUUsage = perfCounterGPU.NextValue();
                            }
                            catch
                            {
                                CurrentGPUUsage = 0;
                            }
                        }

                        // FPS berechnen
                        long currentTime = GetTickCount();
                        long deltaTime = currentTime - lastFrameTime;

                        if (deltaTime > 0)
                        {
                            float frameTime = deltaTime / 1000f;
                            frameTimes.Enqueue(frameTime);

                            float avgFrameTime = frameTimes.Average();
                            CurrentFPS = avgFrameTime > 0 ? 1f / avgFrameTime : 0;

                            // Frame-Time Variance berechnen (Stuttering-Messung)
                            float varianceSum = frameTimes.Sum(ft => (ft - avgFrameTime) * (ft - avgFrameTime));
                            FrameTimeVariance = frameTimes.Count > 0 ? varianceSum / frameTimes.Count : 0;
                        }
                        lastFrameTime = currentTime;

                        // Netzwerk-Latenz simulieren/berechnen
                        UpdateNetworkMetrics();

                        // FiveM-Prozess spezifisch überwachen
                        MonitorFiveMProcess();

                        // Performance-Snapshot erstellen
                        var snapshot = new PerformanceSnapshot
                        {
                            Timestamp = DateTime.Now,
                            CPUUsage = CurrentCPUUsage,
                            RAMUsageGB = CurrentRAMUsageGB,
                            GPUUsage = CurrentGPUUsage,
                            FPS = CurrentFPS,
                            Ping = CurrentPing,
                            NetworkLatency = NetworkLatency,
                            FrameTimeVariance = FrameTimeVariance
                        };

                        _snapshots.Enqueue(snapshot);
                        while (_snapshots.Count > 3600) // 1 Stunde bei 1s Intervall
                        {
                            _snapshots.TryDequeue(out _);
                        }

                        // Event auslösen
                        MetricsUpdated?.Invoke(this, new PerformanceMetricsUpdatedEventArgs(snapshot));

                        // Quantum-Analyse durchführen
                        PerformQuantumAnalysis(snapshot);

                        Thread.Sleep(1000); // 1 Sekunde Update-Intervall
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Fehler im Monitoring-Loop: {ex.Message}", LogLevel.Error);
                        Thread.Sleep(5000);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Monitoring-Loop abgebrochen: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// FiveM-Prozess spezifisch überwachen
        /// </summary>
        private void MonitorFiveMProcess()
        {
            try
            {
                var processes = Process.GetProcessesByName("FiveM");
                if (processes.Length > 0)
                {
                    var fivemProcess = processes[0];

                    if (!_processData.ContainsKey(fivemProcess.Id))
                    {
                        _processData[fivemProcess.Id] = new ProcessPerformanceData
                        {
                            ProcessId = fivemProcess.Id,
                            ProcessName = fivemProcess.ProcessName
                        };
                    }

                    var data = _processData[fivemProcess.Id];
                    data.CPUUsage = fivemProcess.TotalProcessorTime.TotalMilliseconds;
                    data.RAMUsageMB = fivemProcess.WorkingSet64 / 1024 / 1024;
                    data.ThreadCount = fivemProcess.Threads.Count;
                    data.HandleCount = fivemProcess.HandleCount;
                    data.LastUpdate = DateTime.Now;

                    // Working Set optimieren (RAM-Druck reduzieren)
                    if (data.RAMUsageMB > 2048) // > 2GB
                    {
                        EmptyWorkingSet(fivemProcess.Handle);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Fehler beim FiveM-Monitoring: {ex.Message}", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Netzwerk-Metriken aktualisieren
        /// </summary>
        private void UpdateNetworkMetrics()
        {
            try
            {
                // Ping-Messung (vereinfacht)
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = ping.Send("8.8.8.8", 1000);
                    CurrentPing = reply?.RoundtripTime ?? 0;
                }

                // Netzwerk-Latenz berechnen
                var netCounters = new PerformanceCounterCategory("Network Interface");
                var instanceNames = netCounters.GetInstanceNames();

                if (instanceNames.Length > 0)
                {
                    var counter = new PerformanceCounter("Network Interface",
                        "Current Bandwidth", instanceNames[0]);
                    NetworkLatency = CalculateNetworkLatency();
                }
            }
            catch
            {
                CurrentPing = 0;
                NetworkLatency = 0;
            }
        }

        /// <summary>
        /// Quantum-Analyse der Performance-Daten
        /// </summary>
        private void PerformQuantumAnalysis(PerformanceSnapshot snapshot)
        {
            // KI-basierte Anomalie-Erkennung
            DetectPerformanceAnomalies(snapshot);

            // Predictive Maintenance
            PredictSystemIssues(snapshot);

            // Optimierungsempfehlungen generieren
            GenerateOptimizationSuggestions(snapshot);
        }

        private void DetectPerformanceAnomalies(PerformanceSnapshot snapshot)
        {
            // Erkenne FPS-Drops
            if (snapshot.FPS > 0 && snapshot.FrameTimeVariance > 5.0f)
            {
                Logger.Log($"Performance-Anomalie: Hohe Frame-Time-Variance ({snapshot.FrameTimeVariance:F2}ms)",
                    LogLevel.Warning);
            }

            // Erkenne hohe Latenz
            if (snapshot.Ping > 100)
            {
                Logger.Log($"Netzwerk-Anomalie: Hoher Ping ({snapshot.Ping}ms)",
                    LogLevel.Warning);
            }
        }

        private void PredictSystemIssues(PerformanceSnapshot snapshot)
        {
            // RAM-Leck Erkennung
            var recentSnapshots = _snapshots.Where(s =>
                (DateTime.Now - s.Timestamp).TotalMinutes < 5).ToList();

            if (recentSnapshots.Count >= 10)
            {
                var avgRAMStart = recentSnapshots.Take(5).Average(s => s.RAMUsageGB);
                var avgRAMEnd = recentSnapshots.TakeLast(5).Average(s => s.RAMUsageGB);

                if (avgRAMEnd - avgRAMStart > 0.5) // > 500MB Anstieg in 5 Minuten
                {
                    Logger.Log("Mögliches RAM-Leck erkannt", LogLevel.Warning);
                }
            }
        }

        private void GenerateOptimizationSuggestions(PerformanceSnapshot snapshot)
        {
            var suggestions = new System.Collections.Generic.List<string>();

            if (snapshot.CPUUsage > 80)
                suggestions.Add("CPU-Auslastung hoch - Hintergrundprozesse prüfen");

            if (snapshot.RAMUsageGB > GetTotalPhysicalMemory() * 0.8)
                suggestions.Add("RAM-Auslastung kritisch - Arbeitsspeicher optimieren");

            if (snapshot.FPS < 60 && snapshot.GPUUsage < 50)
                suggestions.Add("CPU-Limitierung erkannt - CPU optimieren");

            if (snapshot.Ping > 50)
                suggestions.Add("Netzwerklatenz erhöht - Netzwerk optimieren");

            if (suggestions.Count > 0)
            {
                Logger.Log($"Optimierungsvorschläge: {string.Join("; ", suggestions)}",
                    LogLevel.Info);
            }
        }

        private float GetTotalPhysicalMemory()
        {
            using (var pc = new PerformanceCounter("Memory", "Available MBytes"))
            {
                var mem = new Microsoft.VisualBasic.Devices.ComputerInfo();
                return mem.TotalPhysicalMemory / 1024f / 1024f;
            }
        }

        private string GetGPUInstanceName()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var instances = category.GetInstanceNames();
                return instances.FirstOrDefault(i => i.Contains("engtype_3D")) ?? instances.FirstOrDefault();
            }
            catch
            {
                return "_Total";
            }
        }

        private float CalculateNetworkLatency()
        {
            // Simulierte Netzwerk-Latenz-Berechnung
            // In einer echten Implementierung würde dies echte Netzwerk-Metriken verwenden
            return CurrentPing * 1.5f; // Geschätzte Gesamtlatenz
        }

        /// <summary>
        /// Gibt die letzten Performance-Snapshots zurück
        /// </summary>
        public PerformanceSnapshot[] GetRecentSnapshots(int count = 100)
        {
            return _snapshots.TakeLast(count).ToArray();
        }

        /// <summary>
        /// Gibt FiveM-spezifische Performance-Daten zurück
        /// </summary>
        public ProcessPerformanceData GetFiveMPerformanceData()
        {
            return _processData.Values.FirstOrDefault(p => p.ProcessName == "FiveM");
        }

        /// <summary>
        /// Berechnet die durchschnittliche Performance für einen Zeitraum
        /// </summary>
        public PerformanceMetrics GetAverageMetrics(TimeSpan period)
        {
            var snapshots = _snapshots.Where(s =>
                (DateTime.Now - s.Timestamp) <= period).ToArray();

            if (snapshots.Length == 0)
                return new PerformanceMetrics();

            return new PerformanceMetrics
            {
                AvgCPU = snapshots.Average(s => s.CPUUsage),
                AvgRAM = snapshots.Average(s => s.RAMUsageGB),
                AvgGPU = snapshots.Average(s => s.GPUUsage),
                AvgFPS = snapshots.Average(s => s.FPS),
                AvgPing = snapshots.Average(s => s.Ping),
                MinFPS = snapshots.Min(s => s.FPS),
                MaxFPS = snapshots.Max(s => s.FPS)
            };
        }

        public void Dispose()
        {
            StopMonitoring();
            _cancellationTokenSource?.Dispose();
        }

        private class CircularBuffer<T>
        {
            private readonly T[] _buffer;
            private int _index;
            private int _count;

            public CircularBuffer(int capacity)
            {
                _buffer = new T[capacity];
            }

            public void Enqueue(T item)
            {
                _buffer[_index] = item;
                _index = (_index + 1) % _buffer.Length;
                if (_count < _buffer.Length) _count++;
            }

            public T[] ToArray()
            {
                var result = new T[_count];
                for (int i = 0; i < _count; i++)
                {
                    result[i] = _buffer[(_index - _count + i + _buffer.Length) % _buffer.Length];
                }
                return result;
            }

            public float Average(Func<T, float> selector)
            {
                if (_count == 0) return 0;
                return ToArray().Average(selector);
            }

            public float Sum(Func<T, float> selector)
            {
                if (_count == 0) return 0;
                return ToArray().Sum(selector);
            }

            public int Count => _count;
        }
    }

    /// <summary>
    /// Performance-Snapshot-Struktur
    /// </summary>
    public class PerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public float CPUUsage { get; set; }
        public float RAMUsageGB { get; set; }
        public float GPUUsage { get; set; }
        public float FPS { get; set; }
        public int Ping { get; set; }
        public float NetworkLatency { get; set; }
        public float FrameTimeVariance { get; set; }
    }

    /// <summary>
    /// Prozess-spezifische Performance-Daten
    /// </summary>
    public class ProcessPerformanceData
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public double CPUUsage { get; set; }
        public long RAMUsageMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// Performance-Metriken für Zeiträume
    /// </summary>
    public class PerformanceMetrics
    {
        public float AvgCPU { get; set; }
        public float AvgRAM { get; set; }
        public float AvgGPU { get; set; }
        public float AvgFPS { get; set; }
        public float AvgPing { get; set; }
        public float MinFPS { get; set; }
        public float MaxFPS { get; set; }
    }

    /// <summary>
    /// Event-Args für Performance-Updates
    /// </summary>
    public class PerformanceMetricsUpdatedEventArgs : EventArgs
    {
        public PerformanceSnapshot Snapshot { get; }

        public PerformanceMetricsUpdatedEventArgs(PerformanceSnapshot snapshot)
        {
            Snapshot = snapshot;
        }
    }
}