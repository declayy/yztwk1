using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using FiveMQuantumTweaker2026.Utils;

namespace FiveMQuantumTweaker2026.UI
{
    public partial class QuantumVisualizer : UserControl
    {
        private readonly DispatcherTimer _updateTimer;
        private readonly PerformanceMonitor _performanceMonitor;
        private readonly Random _random;
        private readonly List<Particle> _particles;
        private readonly List<PerformanceDataPoint> _graphData;
        private DateTime _startTime;
        private double _lastGraphUpdate;

        // Performance History
        private readonly Queue<float> _cpuHistory;
        private readonly Queue<float> _fpsHistory;
        private readonly Queue<float> _pingHistory;
        private readonly Queue<float> _hitRegHistory;

        // HitReg Animation
        private double _hitRegAngle;
        private bool _hitRegSynced;

        public QuantumVisualizer()
        {
            InitializeComponent();

            _random = new Random();
            _particles = new List<Particle>();
            _graphData = new List<PerformanceDataPoint>();

            _cpuHistory = new Queue<float>(60);
            _fpsHistory = new Queue<float>(60);
            _pingHistory = new Queue<float>(60);
            _hitRegHistory = new Queue<float>(60);

            _startTime = DateTime.Now;
            _lastGraphUpdate = 0;

            // Timer für UI-Updates
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100),
                IsEnabled = true
            };
            _updateTimer.Tick += OnUpdateTimerTick;

            // Performance Monitor initialisieren
            _performanceMonitor = new PerformanceMonitor();
            _performanceMonitor.DataUpdated += OnPerformanceDataUpdated;
            _performanceMonitor.StartMonitoring(500);

            // Particle System initialisieren
            InitializeParticles();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Animationen starten
            StartAnimations();

            // Status auf aktiv setzen
            StatusText.Text = "SYSTEM: AKTIV";
            StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));

            // Graph-Type ComboBox initialisieren
            GraphTypeCombo.SelectedIndex = 0;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _updateTimer.Stop();
            _performanceMonitor.StopMonitoring();
            _performanceMonitor.DataUpdated -= OnPerformanceDataUpdated;
            _performanceMonitor.Dispose();
        }

        private void InitializeParticles()
        {
            // Quanten-Partikel erstellen
            for (int i = 0; i < 150; i++)
            {
                var particle = new Particle
                {
                    X = _random.NextDouble() * 800, // Standard-Breite
                    Y = _random.NextDouble() * 450, // Standard-Höhe
                    Size = _random.Next(1, 4),
                    SpeedX = (_random.NextDouble() - 0.5) * 0.5,
                    SpeedY = (_random.NextDouble() - 0.5) * 0.5,
                    Color = GetRandomQuantumColor(),
                    Opacity = _random.NextDouble() * 0.3 + 0.1,
                    Life = _random.NextDouble() * 100 + 50
                };

                _particles.Add(particle);
            }
        }

        private Color GetRandomQuantumColor()
        {
            // Lila/Blau/Pink Farbpalette für Quanteneffekt
            int colorChoice = _random.Next(3);
            return colorChoice switch
            {
                0 => Color.FromRgb(106, 0, 255),    // Dunkellila
                1 => Color.FromRgb(157, 0, 255),    // Lila
                2 => Color.FromRgb(0, 212, 255),    // Cyan/Blau
                _ => Color.FromRgb(106, 0, 255)
            };
        }

        private void StartAnimations()
        {
            // HitReg-Kreis-Animation
            var hitRegAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };

            var rotateTransform = new RotateTransform();
            HitRegCircle.RenderTransform = rotateTransform;
            HitRegCircle.RenderTransformOrigin = new Point(0.5, 0.5);

            Storyboard.SetTarget(hitRegAnimation, rotateTransform);
            Storyboard.SetTargetProperty(hitRegAnimation, new PropertyPath(RotateTransform.AngleProperty));

            var hitRegStoryboard = new Storyboard();
            hitRegStoryboard.Children.Add(hitRegAnimation);
            hitRegStoryboard.Begin();

            // Quanten-Partikel-Animation
            CompositionTarget.Rendering += OnCompositionTargetRendering;

            // Status-Indicator Puls-Animation
            StartStatusPulseAnimation();
        }

        private void StartStatusPulseAnimation()
        {
            var pulseAnimation = new DoubleAnimation
            {
                From = 0.3,
                To = 0.8,
                Duration = TimeSpan.FromSeconds(1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            StatusIndicator.BeginAnimation(Ellipse.OpacityProperty, pulseAnimation);
        }

        private void OnPerformanceDataUpdated(object sender, PerformanceMonitor.PerformanceDataUpdatedEventArgs e)
        {
            // UI-Update im UI-Thread ausführen
            Dispatcher.Invoke(() =>
            {
                UpdatePerformanceUI(e);
                UpdateGraphData(e);
                UpdateHitRegVisualization(e);
            });
        }

        private void UpdatePerformanceUI(PerformanceMonitor.PerformanceDataUpdatedEventArgs data)
        {
            // CPU
            CpuUsageText.Text = $"{data.CpuUsage:F1}%";
            CpuUsageBar.Value = data.CpuUsage;
            CpuFrequencyText.Text = $"{Environment.ProcessorCount / 1000f:F1} GHz";
            CpuTempText.Text = "45°C"; // Simuliert

            // GPU
            GpuUsageText.Text = $"{data.GpuUsage:F1}%";
            GpuUsageBar.Value = data.GpuUsage;
            GpuVramText.Text = "8/12 GB"; // Simuliert
            GpuTempText.Text = "65°C"; // Simuliert

            // RAM
            float totalMemoryGB = 16f; // Simuliert
            float ramUsagePercent = (data.MemoryUsageGB / totalMemoryGB) * 100;

            RamUsageText.Text = $"{data.MemoryUsageGB:F1} GB";
            RamUsageBar.Value = ramUsagePercent;
            RamSpeedText.Text = "3200 MHz"; // Simuliert
            RamLatencyText.Text = "CL16"; // Simuliert

            // FPS
            FpsText.Text = $"{data.Fps:F0}";
            UpdateFpsIndicator(data.Fps);

            // FPS-Stabilität berechnen
            _fpsHistory.Enqueue(data.Fps);
            if (_fpsHistory.Count > 60) _fpsHistory.Dequeue();

            float fpsStability = CalculateFpsStability();
            FpsStabilityBar.Value = fpsStability;

            if (_fpsHistory.Count >= 2)
            {
                FpsMinText.Text = $"{_fpsHistory.Min():F0}";
                FpsMaxText.Text = $"{_fpsHistory.Max():F0}";
            }

            // Netzwerk
            PingText.Text = $"{data.Ping} ms";
            UpdatePingQualityBar(data.Ping);

            // Upload/Download (simuliert)
            float uploadSpeed = 50f + (float)(_random.NextDouble() * 20f);
            float downloadSpeed = 100f + (float)(_random.NextDouble() * 50f);

            UploadSpeedText.Text = $"{uploadSpeed:F1} Mbps";
            DownloadSpeedText.Text = $"{downloadSpeed:F1} Mbps";

            // HitReg
            UpdateHitRegDisplay(data);

            // System
            float systemLoad = (data.CpuUsage + data.GpuUsage) / 2;
            SystemLoadText.Text = $"LOAD: {systemLoad:F1}%";
            SystemLoadBar.Value = systemLoad;

            // Threads & Handles
            ThreadCountText.Text = $"{Environment.ProcessorCount * 2}";
            HandleCountText.Text = "15000"; // Simuliert

            // Uptime
            var uptime = DateTime.Now - _startTime;
            UptimeText.Text = $"UPTIME: {uptime.Hours:D2}:{uptime.Minutes:D2}";

            // Performance Score
            int score = _performanceMonitor.CalculatePerformanceScore();
            UpdatePerformanceScore(score);
        }

        private void UpdateFpsIndicator(float fps)
        {
            // Farbe basierend auf FPS
            Color fpsColor = fps switch
            {
                > 100 => Colors.LimeGreen,
                > 60 => Colors.GreenYellow,
                > 30 => Colors.Yellow,
                _ => Colors.Red
            };

            FpsIndicator.Fill = new SolidColorBrush(fpsColor);

            // Glow-Effekt aktualisieren
            var dropShadow = (DropShadowEffect)FpsIndicator.Effect;
            if (dropShadow != null)
            {
                dropShadow.Color = fpsColor;
            }
        }

        private float CalculateFpsStability()
        {
            if (_fpsHistory.Count < 2) return 100;

            float avgFps = _fpsHistory.Average();
            float variance = _fpsHistory.Sum(f => Math.Abs(f - avgFps)) / _fpsHistory.Count;

            // Stabilität als Prozentsatz (100% = perfekt stabil)
            float stability = Math.Max(0, 100 - variance * 2);
            return Math.Min(100, stability);
        }

        private void UpdatePingQualityBar(int ping)
        {
            // Ping-Qualität berechnen
            float quality = ping switch
            {
                < 20 => 100,
                < 50 => 90,
                < 100 => 75,
                < 150 => 50,
                < 200 => 25,
                _ => 10
            };

            PingQualityBar.Value = quality;

            // Farbe anpassen
            Color pingColor = ping switch
            {
                < 50 => Colors.LimeGreen,
                < 100 => Colors.GreenYellow,
                < 150 => Colors.Yellow,
                < 200 => Colors.Orange,
                _ => Colors.Red
            };

            // Gradient-Farbe aktualisieren
            var gradient = (LinearGradientBrush)PingQualityBar.Foreground;
            if (gradient != null && gradient.GradientStops.Count >= 2)
            {
                gradient.GradientStops[0].Color = pingColor;
                gradient.GradientStops[1].Color = pingColor;
            }
        }

        private void UpdateHitRegDisplay(PerformanceMonitor.PerformanceDataUpdatedEventArgs data)
        {
            // HitReg-Vorteil berechnen (simuliert)
            float hitRegAdvantage = CalculateHitRegAdvantage(data);

            HitRegAdvantageText.Text = $"+{hitRegAdvantage:F1} ms";
            HitRegQualityBar.Value = Math.Min(100, hitRegAdvantage * 10);

            // Server/Client Tick
            float serverTick = 64f;
            float clientTick = serverTick * (1 + hitRegAdvantage / 1000f);

            ServerTickText.Text = $"Tick: {serverTick:F0} Hz";
            ClientTickText.Text = $"Client: {clientTick:F0} Hz";

            // HitReg-Status
            _hitRegSynced = hitRegAdvantage > 5;
            HitRegStatusText.Text = _hitRegSynced ? "SYNCED ✓" : "DESYNCED ✗";
            HitRegStatusText.Foreground = _hitRegSynced ?
                new SolidColorBrush(Colors.LimeGreen) :
                new SolidColorBrush(Colors.Red);

            // HitReg-Animation basierend auf Sync-Status
            UpdateHitRegAnimation(_hitRegSynced);
        }

        private float CalculateHitRegAdvantage(PerformanceMonitor.PerformanceDataUpdatedEventArgs data)
        {
            // HitReg-Vorteil basierend auf Performance berechnen
            float baseAdvantage = 8f; // Basis-Vorteil

            // CPU-Effekt
            float cpuEffect = (100 - data.CpuUsage) * 0.05f;

            // Ping-Effekt (niedriger Ping = besserer Vorteil)
            float pingEffect = Math.Max(0, 50 - data.Ping) * 0.1f;

            // FPS-Effekt
            float fpsEffect = Math.Min(10, data.Fps / 30f);

            // Zufällige Variation
            float randomEffect = (float)(_random.NextDouble() * 2f - 1f);

            return Math.Max(0, baseAdvantage + cpuEffect + pingEffect + fpsEffect + randomEffect);
        }

        private void UpdateHitRegVisualization(PerformanceMonitor.PerformanceDataUpdatedEventArgs data)
        {
            // HitReg-Kreis-Animation basierend auf Sync-Status
            if (_hitRegSynced)
            {
                HitRegCore.Fill.Opacity = 0.8 + 0.2 * Math.Sin(DateTime.Now.Ticks / 10000000.0);
            }
            else
            {
                HitRegCore.Fill.Opacity = 0.3 + 0.1 * Math.Sin(DateTime.Now.Ticks / 5000000.0);
            }
        }

        private void UpdateHitRegAnimation(bool synced)
        {
            if (synced)
            {
                // Smooth Animation für synced state
                var syncedAnimation = new DoubleAnimation
                {
                    To = 0.8,
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                HitRegCore.BeginAnimation(Ellipse.OpacityProperty, syncedAnimation);
            }
            else
            {
                // Pulsierende Animation für desynced state
                var desyncedAnimation = new DoubleAnimation
                {
                    From = 0.3,
                    To = 0.6,
                    Duration = TimeSpan.FromSeconds(0.5),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                HitRegCore.BeginAnimation(Ellipse.OpacityProperty, desyncedAnimation);
            }
        }

        private void UpdatePerformanceScore(int score)
        {
            // Score-Anzeige aktualisieren (kann in UI hinzugefügt werden)
            Color scoreColor = score switch
            {
                > 80 => Colors.LimeGreen,
                > 60 => Colors.GreenYellow,
                > 40 => Colors.Yellow,
                > 20 => Colors.Orange,
                _ => Colors.Red
            };

            // Optional: Score in Status-Text einbinden
            StatusText.Text = $"SYSTEM: AKTIV | SCORE: {score}/100";
        }

        private void UpdateGraphData(PerformanceMonitor.PerformanceDataUpdatedEventArgs data)
        {
            // Neue Datenpunkte hinzufügen
            var dataPoint = new PerformanceDataPoint
            {
                Timestamp = DateTime.Now,
                CpuUsage = data.CpuUsage,
                Fps = data.Fps,
                Ping = data.Ping,
                HitRegAdvantage = CalculateHitRegAdvantage(data)
            };

            _graphData.Add(dataPoint);

            // Alte Daten entfernen (letzte 300 Sekunden behalten)
            var cutoffTime = DateTime.Now.AddSeconds(-300);
            _graphData.RemoveAll(p => p.Timestamp < cutoffTime);
        }

        private void OnUpdateTimerTick(object sender, EventArgs e)
        {
            UpdateParticles();
            DrawGraph();
        }

        private void UpdateParticles()
        {
            // Canvas-Größe prüfen
            if (QuantumParticlesCanvas.ActualWidth <= 0 || QuantumParticlesCanvas.ActualHeight <= 0)
                return;

            // Partikel aktualisieren
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var particle = _particles[i];

                // Bewegung
                particle.X += particle.SpeedX;
                particle.Y += particle.SpeedY;

                // Lebenszeit reduzieren
                particle.Life--;

                // Randbehandlung
                if (particle.X < 0 || particle.X > QuantumParticlesCanvas.ActualWidth ||
                    particle.Y < 0 || particle.Y > QuantumParticlesCanvas.ActualHeight ||
                    particle.Life <= 0)
                {
                    // Partikel neu initialisieren
                    particle.X = _random.NextDouble() * QuantumParticlesCanvas.ActualWidth;
                    particle.Y = _random.NextDouble() * QuantumParticlesCanvas.ActualHeight;
                    particle.Life = _random.NextDouble() * 100 + 50;
                }

                _particles[i] = particle;
            }

            // Partikel zeichnen
            DrawParticles();
        }

        private void DrawParticles()
        {
            QuantumParticlesCanvas.Children.Clear();

            foreach (var particle in _particles)
            {
                var ellipse = new Ellipse
                {
                    Width = particle.Size,
                    Height = particle.Size,
                    Fill = new SolidColorBrush(particle.Color),
                    Opacity = particle.Opacity
                };

                Canvas.SetLeft(ellipse, particle.X);
                Canvas.SetTop(ellipse, particle.Y);

                QuantumParticlesCanvas.Children.Add(ellipse);
            }
        }

        private void DrawGraph()
        {
            if (_graphData.Count < 2) return;

            LiveGraphCanvas.Children.Clear();

            // Graphtyp auswählen
            string selectedGraph = ((ComboBoxItem)GraphTypeCombo.SelectedItem)?.Content.ToString();

            // Daten für Graphen vorbereiten
            List<float> values = selectedGraph switch
            {
                "CPU Usage" => _graphData.Select(d => d.CpuUsage).ToList(),
                "FPS Timeline" => _graphData.Select(d => d.Fps).ToList(),
                "Network Latency" => _graphData.Select(d => (float)d.Ping).ToList(),
                "HitReg Advantage" => _graphData.Select(d => d.HitRegAdvantage).ToList(),
                _ => _graphData.Select(d => d.CpuUsage).ToList()
            };

            if (values.Count < 2) return;

            // Canvas-Dimensionen
            double canvasWidth = LiveGraphCanvas.ActualWidth;
            double canvasHeight = LiveGraphCanvas.ActualHeight - 20;

            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            // Werte skalieren
            float maxValue = values.Max();
            float minValue = values.Min();
            float valueRange = Math.Max(1, maxValue - minValue);

            // Linien zeichnen
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromRgb(106, 0, 255)),
                StrokeThickness = 2,
                Opacity = 0.8
            };

            for (int i = 0; i < values.Count; i++)
            {
                double x = (double)i / (values.Count - 1) * canvasWidth;
                double y = canvasHeight - ((values[i] - minValue) / valueRange * (canvasHeight - 20));

                polyline.Points.Add(new Point(x, y));
            }

            LiveGraphCanvas.Children.Add(polyline);

            // Punkte zeichnen (alle 5% der Datenpunkte)
            int pointInterval = Math.Max(1, values.Count / 20);
            for (int i = 0; i < values.Count; i += pointInterval)
            {
                double x = (double)i / (values.Count - 1) * canvasWidth;
                double y = canvasHeight - ((values[i] - minValue) / valueRange * (canvasHeight - 20));

                var point = new Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = new SolidColorBrush(Color.FromRgb(0, 212, 255)),
                    Opacity = 0.7
                };

                Canvas.SetLeft(point, x - 2);
                Canvas.SetTop(point, y - 2);

                LiveGraphCanvas.Children.Add(point);
            }

            // Graph-Info aktualisieren
            UpdateGraphInfo(selectedGraph, values);
        }

        private void UpdateGraphInfo(string graphType, List<float> values)
        {
            if (values.Count == 0) return;

            // Durchschnitt der letzten 60 Sekunden
            var recentValues = values.TakeLast(Math.Min(60, values.Count)).ToArray();
            float avgValue = recentValues.Length > 0 ? recentValues.Average() : 0;

            GraphInfoText.Text = $"{graphType.ToUpper()}: {avgValue:F1} (AVG)";
            GraphPeakText.Text = $"PEAK: {values.Max():F1} | MIN: {values.Min():F1}";
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            // Hintergrund-Quanteneffekt
            var time = DateTime.Now.Ticks / 10000000.0;
            var gradient = (LinearGradientBrush)Resources["QuantumGradientBrush"];

            if (gradient != null)
            {
                // Farbverlauf animieren
                foreach (var stop in gradient.GradientStops)
                {
                    double offsetChange = Math.Sin(time * 0.5 + stop.Offset * Math.PI) * 0.05;
                    stop.Offset = Math.Max(0, Math.Min(1, stop.Offset + offsetChange));
                }
            }
        }

        private void GraphTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Graphtyp geändert - Graph neu zeichnen
            DrawGraph();
        }

        /// <summary>
        /// Startet das Performance-Monitoring
        /// </summary>
        public void StartMonitoring()
        {
            _performanceMonitor.StartMonitoring(500);
            _updateTimer.Start();

            StatusText.Text = "SYSTEM: AKTIV";
            StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0));
        }

        /// <summary>
        /// Stoppt das Performance-Monitoring
        /// </summary>
        public void StopMonitoring()
        {
            _performanceMonitor.StopMonitoring();
            _updateTimer.Stop();

            StatusText.Text = "SYSTEM: INAKTIV";
            StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        }

        /// <summary>
        /// Setzt alle Daten zurück
        /// </summary>
        public void ResetData()
        {
            _graphData.Clear();
            _cpuHistory.Clear();
            _fpsHistory.Clear();
            _pingHistory.Clear();
            _hitRegHistory.Clear();

            LiveGraphCanvas.Children.Clear();

            // UI zurücksetzen
            CpuUsageText.Text = "0%";
            CpuUsageBar.Value = 0;
            GpuUsageText.Text = "0%";
            GpuUsageBar.Value = 0;
            RamUsageText.Text = "0 GB";
            RamUsageBar.Value = 0;
            FpsText.Text = "0";
            FpsStabilityBar.Value = 0;
            PingText.Text = "0 ms";
            PingQualityBar.Value = 0;
            HitRegAdvantageText.Text = "+0.0 ms";
            HitRegQualityBar.Value = 0;
            SystemLoadText.Text = "LOAD: 0%";
            SystemLoadBar.Value = 0;

            _startTime = DateTime.Now;
        }

        /// <summary>
        /// Exportiert Performance-Daten als CSV
        /// </summary>
        public void ExportData(string filePath)
        {
            try
            {
                using (var writer = new System.IO.StreamWriter(filePath))
                {
                    // Header
                    writer.WriteLine("Timestamp,CPU(%),RAM(GB),GPU(%),FPS,Ping(ms),HitRegAdvantage(ms)");

                    // Daten
                    foreach (var data in _graphData)
                    {
                        writer.WriteLine($"{data.Timestamp:yyyy-MM-dd HH:mm:ss},{data.CpuUsage:F2},{data.MemoryUsageGB:F2},{data.GpuUsage:F2},{data.Fps:F2},{data.Ping},{data.HitRegAdvantage:F2}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Export: {ex.Message}", "Export Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================
        // HELPER CLASSES
        // ============================================

        private class Particle
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Size { get; set; }
            public double SpeedX { get; set; }
            public double SpeedY { get; set; }
            public Color Color { get; set; }
            public double Opacity { get; set; }
            public double Life { get; set; }
        }

        private class PerformanceDataPoint
        {
            public DateTime Timestamp { get; set; }
            public float CpuUsage { get; set; }
            public float MemoryUsageGB { get; set; }
            public float GpuUsage { get; set; }
            public float Fps { get; set; }
            public int Ping { get; set; }
            public float HitRegAdvantage { get; set; }
        }
    }
}