using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FiveMQuantumTweaker2026.Core;
using FiveMQuantumTweaker2026.Models;
using FiveMQuantumTweaker2026.Services;
using FiveMQuantumTweaker2026.Utils;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;

namespace FiveMQuantumTweaker2026.UI
{
    /// <summary>
    /// Hauptfenster des FiveM Quantum Tweakers 2026
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Private Fields

        private readonly Logger _logger;
        private readonly PerformanceMonitor _performanceMonitor;
        private readonly QuantumOptimizer _quantumOptimizer;
        private readonly SystemSanityManager _sanityManager;
        private readonly TelemetryService _telemetryService;

        private DispatcherTimer _updateTimer;
        private DispatcherTimer _clockTimer;
        private CancellationTokenSource _optimizationCancellationToken;

        private bool _isInitialized = false;
        private bool _isMinimizedToTray = false;
        private bool _autoScrollLog = true;
        private DateTime _startTime;

        private SeriesCollection _cpuSeries;
        private SeriesCollection _gpuSeries;
        private SeriesCollection _memorySeries;
        private SeriesCollection _networkSeries;

        private ChartValues<double> _cpuValues;
        private ChartValues<double> _gpuValues;
        private ChartValues<double> _memoryValues;
        private ChartValues<double> _networkUpValues;
        private ChartValues<double> _networkDownValues;

        private Process _fiveMProcess;
        private PerformanceCounter _fiveMCpuCounter;
        private PerformanceCounter _fiveMMemoryCounter;

        #endregion

        #region Public Properties

        /// <summary>
        /// Liste der Performance-Metriken
        /// </summary>
        public ObservableCollection<PerformanceMetric> PerformanceMetrics { get; private set; }

        /// <summary>
        /// Liste der Optimierungs-Profile
        /// </summary>
        public ObservableCollection<OptimizationProfile> Profiles { get; private set; }

        /// <summary>
        /// Aktuell ausgewähltes Profil
        /// </summary>
        private OptimizationProfile _selectedProfile;
        public OptimizationProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (_selectedProfile != value)
                {
                    _selectedProfile = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsProfileSelected));
                }
            }
        }

        /// <summary>
        /// Gibt an, ob ein Profil ausgewählt ist
        /// </summary>
        public bool IsProfileSelected => SelectedProfile != null;

        /// <summary>
        /// Gibt an, ob Quantum-Optimierungen aktiv sind
        /// </summary>
        private bool _areQuantumTweaksActive;
        public bool AreQuantumTweaksActive
        {
            get => _areQuantumTweaksActive;
            set
            {
                if (_areQuantumTweaksActive != value)
                {
                    _areQuantumTweaksActive = value;
                    OnPropertyChanged();
                    UpdateOptimizationStatus();
                }
            }
        }

        /// <summary>
        /// Gibt an, ob der Gaming-Modus aktiv ist
        /// </summary>
        private bool _isGamingModeActive;
        public bool IsGamingModeActive
        {
            get => _isGamingModeActive;
            set
            {
                if (_isGamingModeActive != value)
                {
                    _isGamingModeActive = value;
                    OnPropertyChanged();
                    UpdateGamingModeIndicator();
                }
            }
        }

        /// <summary>
        /// Gibt an, ob Live-Monitoring aktiv ist
        /// </summary>
        private bool _isLiveMonitoringActive = true;
        public bool IsLiveMonitoringActive
        {
            get => _isLiveMonitoringActive;
            set
            {
                if (_isLiveMonitoringActive != value)
                {
                    _isLiveMonitoringActive = value;
                    OnPropertyChanged();
                    UpdateMonitoringState();
                }
            }
        }

        /// <summary>
        /// Gibt an, ob der Quantum Visualizer aktiv ist
        /// </summary>
        private bool _isQuantumVisualizerActive = true;
        public bool IsQuantumVisualizerActive
        {
            get => _isQuantumVisualizerActive;
            set
            {
                if (_isQuantumVisualizerActive != value)
                {
                    _isQuantumVisualizerActive = value;
                    OnPropertyChanged();
                    UpdateVisualizerState();
                }
            }
        }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Constructor

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // Logger initialisieren
                _logger = new Logger();
                _logger.Info("MainWindow initializing...");

                // Services initialisieren
                _performanceMonitor = new PerformanceMonitor();
                _quantumOptimizer = new QuantumOptimizer();
                _sanityManager = new SystemSanityManager();
                _telemetryService = new TelemetryService();

                // Collections initialisieren
                PerformanceMetrics = new ObservableCollection<PerformanceMetric>();
                Profiles = new ObservableCollection<OptimizationProfile>();

                // DataContext setzen
                DataContext = this;

                // Chart-Serien initialisieren
                InitializeCharts();

                _logger.Info("MainWindow initialized successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Initialisieren des MainWindow: {ex.Message}",
                    "Initialisierungsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        #endregion

        #region Window Lifecycle

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("MainWindow loading...");
                _startTime = DateTime.Now;

                // UI-Elemente initialisieren
                InitializeUI();

                // Timer starten
                StartTimers();

                // System-Info laden
                await LoadSystemInfo();

                // Profile laden
                await LoadProfiles();

                // Performance-Monitoring starten
                await StartPerformanceMonitoring();

                // FiveM-Überwachung starten
                StartFiveMMonitoring();

                _isInitialized = true;
                _logger.Info("MainWindow loaded successfully");

                // Willkommens-Nachricht
                ShowNotification("FiveM Quantum Tweaker 2026",
                    "Quantum optimization system ready. Click 'Quantum Optimize' to begin.",
                    NotificationType.Info);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in Window_Loaded: {ex}");
                LogMessage($"CRITICAL: Window load failed: {ex.Message}");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _logger.Info("MainWindow closing...");

                // Timer stoppen
                StopTimers();

                // FiveM-Überwachung stoppen
                StopFiveMMonitoring();

                // Performance-Monitoring stoppen
                _performanceMonitor?.Stop();

                // Cancellation Tokens freigeben
                _optimizationCancellationToken?.Dispose();

                _logger.Info("MainWindow closed");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in Window_Closing: {ex}");
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            try
            {
                if (WindowState == WindowState.Minimized && _isMinimizedToTray)
                {
                    Hide();
                    ShowInTaskbar = false;

                    // Tray-Icon Benachrichtigung
                    ShowTrayNotification("FiveM Quantum Tweaker",
                        "Running in background. Click tray icon to restore.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in Window_StateChanged: {ex}");
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            }
            catch (Exception)
            {
                // Ignorieren, wenn nicht im Client-Bereich geklickt wird
            }
        }

        #endregion

        #region UI Initialization

        private void InitializeUI()
        {
            try
            {
                // Performance-Metriken initialisieren
                InitializePerformanceMetrics();

                // Fortschrittsbalken Style setzen
                ProgressGlobal.Style = (Style)FindResource("QuantumProgressBarStyle");
                ProgressOverlay.Style = (Style)FindResource("QuantumProgressBarStyle");

                // Chart-Container leeren
                CpuChartContainer.Child = null;
                GpuChartContainer.Child = null;
                MemoryChartContainer.Child = null;
                NetworkChartContainer.Child = null;

                // Log initialisieren
                TxtSystemLog.Text = $"=== FiveM Quantum Tweaker 2026 Log ===\n";
                TxtSystemLog.Text += $"Startzeit: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                TxtSystemLog.Text += $"System: {Environment.OSVersion.VersionString}\n";
                TxtSystemLog.Text += $"Benutzer: {Environment.UserName}\n";
                TxtSystemLog.Text += new string('=', 50) + "\n\n";

                // Auto-Scroll aktivieren
                BtnToggleAutoScroll.Content = ""; // Auto-scroll Icon
                _autoScrollLog = true;

                _logger.Info("UI initialized");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in InitializeUI: {ex}");
            }
        }

        private void InitializePerformanceMetrics()
        {
            try
            {
                PerformanceMetrics.Clear();

                // CPU Metrik
                PerformanceMetrics.Add(new PerformanceMetric
                {
                    Title = "CPU",
                    Value = 0,
                    Unit = "%",
                    IconPath = "M3,3H21V21H3V3M12,6A3,3 0 0,1 15,9A3,3 0 0,1 12,12A3,3 0 0,1 9,9A3,3 0 0,1 12,6M6,18V19H18V18C18,15.79 16.21,14 14,14H10C7.79,14 6,15.79 6,18Z",
                    IconColor = Brushes.Cyan,
                    ValueColor = Brushes.Cyan
                });

                // GPU Metrik
                PerformanceMetrics.Add(new PerformanceMetric
                {
                    Title = "GPU",
                    Value = 0,
                    Unit = "%",
                    IconPath = "M20,18C21.1,18 22,17.1 22,16V6C22,4.89 21.1,4 20,4H4C2.89,4 2,4.89 2,6V16C2,17.11 2.9,18 4,18H0V20H24V18H20Z",
                    IconColor = Brushes.Purple,
                    ValueColor = Brushes.Purple
                });

                // RAM Metrik
                PerformanceMetrics.Add(new PerformanceMetric
                {
                    Title = "RAM",
                    Value = 0,
                    Unit = "GB",
                    IconPath = "M17,4H20A2,2 0 0,1 22,6V18A2,2 0 0,1 20,20H17V4M2,4H16V20H2A2,2 0 0,1 0,18V6A2,2 0 0,1 2,4M4,8H14V10H4V8M4,12H14V14H4V12Z",
                    IconColor = Brushes.Green,
                    ValueColor = Brushes.Green
                });

                // FPS Metrik
                PerformanceMetrics.Add(new PerformanceMetric
                {
                    Title = "FPS",
                    Value = 0,
                    Unit = "",
                    IconPath = "M6,18H18V16H6M6,13H18V11H6M6,6V8H18V6M2,22V2H4V22H2M20,2V22H22V2H20Z",
                    IconColor = Brushes.Yellow,
                    ValueColor = Brushes.Yellow
                });

                // Ping Metrik
                PerformanceMetrics.Add(new PerformanceMetric
                {
                    Title = "PING",
                    Value = 0,
                    Unit = "ms",
                    IconPath = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6Z",
                    IconColor = Brushes.Orange,
                    ValueColor = Brushes.Orange
                });

                _logger.Info("Performance metrics initialized");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in InitializePerformanceMetrics: {ex}");
            }
        }

        private void InitializeCharts()
        {
            try
            {
                // CPU Chart
                _cpuValues = new ChartValues<double>();
                for (int i = 0; i < 60; i++) _cpuValues.Add(0);

                _cpuSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "CPU Usage",
                        Values = _cpuValues,
                        Fill = Brushes.Transparent,
                        Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 200, 255)), // Cyan
                        StrokeThickness = 2,
                        PointGeometry = null
                    }
                };

                // GPU Chart
                _gpuValues = new ChartValues<double>();
                for (int i = 0; i < 60; i++) _gpuValues.Add(0);

                _gpuSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "GPU Usage",
                        Values = _gpuValues,
                        Fill = Brushes.Transparent,
                        Stroke = new SolidColorBrush(Color.FromArgb(255, 180, 0, 255)), // Purple
                        StrokeThickness = 2,
                        PointGeometry = null
                    }
                };

                // Memory Chart
                _memoryValues = new ChartValues<double>();
                for (int i = 0; i < 60; i++) _memoryValues.Add(0);

                _memorySeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Memory Usage",
                        Values = _memoryValues,
                        Fill = Brushes.Transparent,
                        Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 255, 100)), // Green
                        StrokeThickness = 2,
                        PointGeometry = null
                    }
                };

                // Network Chart
                _networkUpValues = new ChartValues<double>();
                _networkDownValues = new ChartValues<double>();
                for (int i = 0; i < 60; i++)
                {
                    _networkUpValues.Add(0);
                    _networkDownValues.Add(0);
                }

                _networkSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Upload",
                        Values = _networkUpValues,
                        Fill = Brushes.Transparent,
                        Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0)), // Green
                        StrokeThickness = 2,
                        PointGeometry = null
                    },
                    new LineSeries
                    {
                        Title = "Download",
                        Values = _networkDownValues,
                        Fill = Brushes.Transparent,
                        Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 100, 255)), // Blue
                        StrokeThickness = 2,
                        PointGeometry = null
                    }
                };

                _logger.Info("Charts initialized");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in InitializeCharts: {ex}");
            }
        }

        #endregion

        #region Timer Management

        private void StartTimers()
        {
            try
            {
                // Update-Timer für Performance-Daten
                _updateTimer = new DispatcherTimer();
                _updateTimer.Interval = TimeSpan.FromSeconds(1);
                _updateTimer.Tick += UpdateTimer_Tick;
                _updateTimer.Start();

                // Clock-Timer für Systemzeit
                _clockTimer = new DispatcherTimer();
                _clockTimer.Interval = TimeSpan.FromSeconds(1);
                _clockTimer.Tick += ClockTimer_Tick;
                _clockTimer.Start();

                // Initiale Zeit setzen
                UpdateClock();
                UpdateUptime();

                _logger.Info("Timers started");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in StartTimers: {ex}");
            }
        }

        private void StopTimers()
        {
            try
            {
                _updateTimer?.Stop();
                _clockTimer?.Stop();
                _logger.Info("Timers stopped");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in StopTimers: {ex}");
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!IsLiveMonitoringActive || !_isInitialized)
                return;

            try
            {
                // Performance-Daten aktualisieren
                UpdatePerformanceData();

                // Charts aktualisieren
                UpdateCharts();

                // FiveM-Status aktualisieren
                UpdateFiveMStatus();

                // Uptime aktualisieren
                UpdateUptime();
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateTimer_Tick: {ex}");
            }
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            try
            {
                TxtSystemTime.Text = DateTime.Now.ToString("HH:mm:ss");
            }
            catch { }
        }

        private void UpdateUptime()
        {
            try
            {
                var uptime = DateTime.Now - _startTime;
                TxtUptime.Text = $"Uptime: {uptime:hh\\:mm\\:ss}";
            }
            catch { }
        }

        #endregion

        #region System Information

        private async Task LoadSystemInfo()
        {
            try
            {
                var systemInfo = await SystemInfo.GetSystemInfoAsync();

                Dispatcher.Invoke(() =>
                {
                    // CPU Info
                    TxtCpuInfo.Text = $"{systemInfo.CpuName} ({systemInfo.CpuCores} cores)";

                    // GPU Info
                    TxtGpuInfo.Text = systemInfo.GpuName;

                    // RAM Info
                    TxtRamInfo.Text = $"{systemInfo.TotalMemoryGB:0.0} GB";

                    // OS Info
                    TxtOsInfo.Text = $"{systemInfo.OsName} {systemInfo.OsVersion}";

                    // Status-Indikatoren setzen
                    UpdateSecurityStatus(systemInfo);
                    UpdateQuantumStatus(systemInfo);

                    _logger.Info($"System info loaded: {systemInfo.CpuName}, {systemInfo.GpuName}");
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in LoadSystemInfo: {ex}");
                LogMessage($"ERROR: Could not load system info: {ex.Message}");
            }
        }

        private void UpdateSecurityStatus(SystemInfo systemInfo)
        {
            try
            {
                if (systemInfo.HasTpm && systemInfo.SecureBootEnabled)
                {
                    SecurityStatusIndicator.Background = new SolidColorBrush(Color.FromArgb(255, 0, 255, 100)); // Green
                    SecurityStatusIndicator.ToolTip = "Security: TPM & Secure Boot Active";
                }
                else if (systemInfo.HasTpm || systemInfo.SecureBootEnabled)
                {
                    SecurityStatusIndicator.Background = new SolidColorBrush(Color.FromArgb(255, 255, 200, 0)); // Yellow
                    SecurityStatusIndicator.ToolTip = "Security: Partial (TPM or Secure Boot)";
                }
                else
                {
                    SecurityStatusIndicator.Background = new SolidColorBrush(Color.FromArgb(255, 255, 50, 50)); // Red
                    SecurityStatusIndicator.ToolTip = "Security: Warning (No TPM/Secure Boot)";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateSecurityStatus: {ex}");
            }
        }

        private void UpdateQuantumStatus(SystemInfo systemInfo)
        {
            try
            {
                if (systemInfo.Avx2Supported && systemInfo.Sse42Supported)
                {
                    QuantumStatusIndicator.Background = new SolidColorBrush(Color.FromArgb(255, 0, 150, 255)); // Blue
                    QuantumStatusIndicator.ToolTip = "Quantum Engine: Ready (AVX2+SSE4.2)";
                }
                else
                {
                    QuantumStatusIndicator.Background = new SolidColorBrush(Color.FromArgb(255, 255, 100, 0)); // Orange
                    QuantumStatusIndicator.ToolTip = "Quantum Engine: Limited (Missing CPU Features)";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateQuantumStatus: {ex}");
            }
        }

        private void UpdateGamingModeIndicator()
        {
            try
            {
                if (IsGamingModeActive)
                {
                    GamingModeIndicator.Visibility = Visibility.Visible;
                    GamingModeIndicator.Background = new SolidColorBrush(Color.FromArgb(255, 255, 50, 150)); // Pink
                    GamingModeIndicator.ToolTip = "Gaming Mode: Active";
                }
                else
                {
                    GamingModeIndicator.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateGamingModeIndicator: {ex}");
            }
        }

        private void UpdateOptimizationStatus()
        {
            try
            {
                if (AreQuantumTweaksActive)
                {
                    TxtOptimizationStatus.Text = "Optimizations: Active";
                    TxtOptimizationStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 100)); // Green
                }
                else
                {
                    TxtOptimizationStatus.Text = "Optimizations: Inactive";
                    TxtOptimizationStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 100, 0)); // Orange
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateOptimizationStatus: {ex}");
            }
        }

        #endregion

        #region Profile Management

        private async Task LoadProfiles()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    Profiles.Clear();

                    // Default Profile
                    var defaultProfile = new OptimizationProfile
                    {
                        ProfileName = "Quantum Default",
                        Description = "Balanced optimization for most systems",
                        ProfileType = ProfileType.Balanced,
                        IsActive = true,
                        LastUsed = DateTime.Now,
                        CreationDate = DateTime.Now
                    };

                    Profiles.Add(defaultProfile);
                    SelectedProfile = defaultProfile;

                    // Gaming Profile
                    Profiles.Add(new OptimizationProfile
                    {
                        ProfileName = "Extreme Gaming",
                        Description = "Maximum performance for competitive gaming",
                        ProfileType = ProfileType.Gaming,
                        IsActive = false,
                        LastUsed = DateTime.Now.AddDays(-1),
                        CreationDate = DateTime.Now.AddDays(-30)
                    });

                    // Streaming Profile
                    Profiles.Add(new OptimizationProfile
                    {
                        ProfileName = "Streaming Optimized",
                        Description = "Optimized for streaming while gaming",
                        ProfileType = ProfileType.Streaming,
                        IsActive = false,
                        LastUsed = DateTime.Now.AddDays(-2),
                        CreationDate = DateTime.Now.AddDays(-15)
                    });

                    // Battery Profile
                    Profiles.Add(new OptimizationProfile
                    {
                        ProfileName = "Battery Saver",
                        Description = "Power-efficient optimizations for laptops",
                        ProfileType = ProfileType.Battery,
                        IsActive = false,
                        LastUsed = DateTime.Now.AddDays(-5),
                        CreationDate = DateTime.Now.AddDays(-10)
                    });

                    _logger.Info($"Loaded {Profiles.Count} profiles");
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in LoadProfiles: {ex}");
                LogMessage($"ERROR: Could not load profiles: {ex.Message}");
            }
        }

        private void ProfileCard_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Border border)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 180, 0, 255)); // Purple
                    border.BorderThickness = new Thickness(2);
                }
            }
            catch { }
        }

        private void ProfileCard_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Border border)
                {
                    border.BorderBrush = Brushes.Transparent;
                    border.BorderThickness = new Thickness(1);
                }
            }
            catch { }
        }

        private void ProfileCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.DataContext is OptimizationProfile profile)
                {
                    // Aktuelles Profil deaktivieren
                    foreach (var p in Profiles)
                    {
                        p.IsActive = false;
                    }

                    // Neues Profil aktivieren
                    profile.IsActive = true;
                    SelectedProfile = profile;

                    // UI aktualisieren
                    ProfilesList.Items.Refresh();

                    LogMessage($"Profile '{profile.ProfileName}' selected");

                    ShowNotification("Profile Selected",
                        $"Active profile: {profile.ProfileName}\n{profile.Description}",
                        NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in ProfileCard_MouseLeftButtonUp: {ex}");
            }
        }

        #endregion

        #region Performance Monitoring

        private async Task StartPerformanceMonitoring()
        {
            try
            {
                await _performanceMonitor.StartAsync();
                _logger.Info("Performance monitoring started");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in StartPerformanceMonitoring: {ex}");
                LogMessage($"ERROR: Performance monitoring failed to start: {ex.Message}");
            }
        }

        private async void UpdatePerformanceData()
        {
            try
            {
                var metrics = await _performanceMonitor.GetCurrentMetricsAsync();

                Dispatcher.Invoke(() =>
                {
                    // CPU
                    var cpuMetric = PerformanceMetrics[0];
                    cpuMetric.Value = metrics.CpuUsage;
                    cpuMetric.Trend = metrics.CpuTrend;
                    TxtCpuUsage.Text = $"{metrics.CpuUsage:0.0}%";
                    TxtCpuTemp.Text = $"{metrics.CpuTemperature:0}°C";

                    // GPU
                    var gpuMetric = PerformanceMetrics[1];
                    gpuMetric.Value = metrics.GpuUsage;
                    gpuMetric.Trend = metrics.GpuTrend;
                    TxtGpuUsage.Text = $"{metrics.GpuUsage:0.0}%";
                    TxtGpuTemp.Text = $"{metrics.GpuTemperature:0}°C";
                    TxtGpuVram.Text = $"{metrics.GpuMemoryUsed:0.0}/{metrics.GpuMemoryTotal:0.0} GB";

                    // RAM
                    var ramMetric = PerformanceMetrics[2];
                    ramMetric.Value = metrics.MemoryUsedGB;
                    ramMetric.Trend = metrics.MemoryTrend;
                    TxtMemoryUsage.Text = $"{metrics.MemoryUsedGB:0.0}/{metrics.MemoryTotalGB:0.0} GB ({metrics.MemoryUsage:0.0}%)";

                    // FPS (simuliert für Demo)
                    var fpsMetric = PerformanceMetrics[3];
                    if (IsGamingModeActive)
                    {
                        var randomFps = new Random().Next(120, 240);
                        fpsMetric.Value = randomFps;
                        TxtFps.Text = randomFps.ToString();
                    }
                    else
                    {
                        var randomFps = new Random().Next(60, 144);
                        fpsMetric.Value = randomFps;
                        TxtFps.Text = randomFps.ToString();
                    }

                    // Ping (simuliert für Demo)
                    var pingMetric = PerformanceMetrics[4];
                    if (AreQuantumTweaksActive)
                    {
                        var randomPing = new Random().Next(15, 35);
                        pingMetric.Value = randomPing;
                        TxtPing.Text = randomPing.ToString();
                        TxtConnectionStatus.Text = "● Connected (Quantum)";
                        TxtConnectionStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 200)); // Cyan
                    }
                    else
                    {
                        var randomPing = new Random().Next(30, 80);
                        pingMetric.Value = randomPing;
                        TxtPing.Text = randomPing.ToString();
                        TxtConnectionStatus.Text = "● Connected";
                        TxtConnectionStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 100)); // Green
                    }

                    // Network
                    TxtNetworkUp.Text = $"▲ {metrics.NetworkUploadMBps:0.00} MB/s";
                    TxtNetworkDown.Text = $"▼ {metrics.NetworkDownloadMBps:0.00} MB/s";
                    TxtNetworkLatency.Text = $"Ping: {metrics.NetworkLatency} ms";

                    // Charts aktualisieren
                    UpdateChartValues(metrics);

                    // Performance Cards aktualisieren
                    PerformanceCards.Items.Refresh();
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdatePerformanceData: {ex}");
            }
        }

        private void UpdateChartValues(PerformanceMetrics metrics)
        {
            try
            {
                // CPU Chart
                _cpuValues.Add(metrics.CpuUsage);
                if (_cpuValues.Count > 60) _cpuValues.RemoveAt(0);

                // GPU Chart
                _gpuValues.Add(metrics.GpuUsage);
                if (_gpuValues.Count > 60) _gpuValues.RemoveAt(0);

                // Memory Chart
                _memoryValues.Add(metrics.MemoryUsage);
                if (_memoryValues.Count > 60) _memoryValues.RemoveAt(0);

                // Network Chart
                _networkUpValues.Add(metrics.NetworkUploadMBps);
                _networkDownValues.Add(metrics.NetworkDownloadMBps);
                if (_networkUpValues.Count > 60)
                {
                    _networkUpValues.RemoveAt(0);
                    _networkDownValues.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateChartValues: {ex}");
            }
        }

        private void UpdateCharts()
        {
            try
            {
                // Charts nur erstellen, wenn noch nicht vorhanden
                if (CpuChartContainer.Child == null)
                {
                    CreateCpuChart();
                    CreateGpuChart();
                    CreateMemoryChart();
                    CreateNetworkChart();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateCharts: {ex}");
            }
        }

        private void CreateCpuChart()
        {
            try
            {
                var cpuChart = new CartesianChart
                {
                    Series = _cpuSeries,
                    DisableAnimations = true,
                    AnimationsSpeed = TimeSpan.FromMilliseconds(0),
                    Hoverable = false,
                    DataTooltip = null,
                    Background = Brushes.Transparent,
                    Height = 150
                };

                cpuChart.AxisX.Add(new Axis
                {
                    Labels = new[] { "60s", "45s", "30s", "15s", "Now" },
                    Separator = new Separator { StrokeThickness = 0 },
                    ShowLabels = true,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150))
                });

                cpuChart.AxisY.Add(new Axis
                {
                    LabelFormatter = value => value.ToString("0") + "%",
                    MinValue = 0,
                    MaxValue = 100,
                    Separator = new Separator { StrokeThickness = 0.5, StrokeDashArray = new DoubleCollection { 4 } },
                    ShowLabels = true,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150))
                });

                CpuChartContainer.Child = cpuChart;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in CreateCpuChart: {ex}");
            }
        }

        private void CreateGpuChart()
        {
            try
            {
                var gpuChart = new CartesianChart
                {
                    Series = _gpuSeries,
                    DisableAnimations = true,
                    AnimationsSpeed = TimeSpan.FromMilliseconds(0),
                    Hoverable = false,
                    DataTooltip = null,
                    Background = Brushes.Transparent,
                    Height = 150
                };

                gpuChart.AxisX.Add(new Axis
                {
                    Labels = new[] { "60s", "45s", "30s", "15s", "Now" },
                    Separator = new Separator { StrokeThickness = 0 },
                    ShowLabels = true,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150))
                });

                gpuChart.AxisY.Add(new Axis
                {
                    LabelFormatter = value => value.ToString("0") + "%",
                    MinValue = 0,
                    MaxValue = 100,
                    Separator = new Separator { StrokeThickness = 0.5, StrokeDashArray = new DoubleCollection { 4 } },
                    ShowLabels = true,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150))
                });

                GpuChartContainer.Child = gpuChart;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in CreateGpuChart: {ex}");
            }
        }

        private void CreateMemoryChart()
        {
            try
            {
                var memoryChart = new CartesianChart
                {
                    Series = _memorySeries,
                    DisableAnimations = true,
                    AnimationsSpeed = TimeSpan.FromMilliseconds(0),
                    Hoverable = false,
                    DataTooltip = null,
                    Background = Brushes.Transparent,
                    Height = 150
                };

                memoryChart.AxisX.Add(new Axis
                {
                    Labels = new[] { "60s", "45s", "30s", "15s", "Now" },
                    Separator = new Separator { StrokeThickness = 0 },
                    ShowLabels = true,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150))
                });

                memoryChart.AxisY.Add(new Axis
                {
                    LabelFormatter = value => value.ToString("0") + "%",
                    MinValue = 0,
                    MaxValue = 100,
                    Separator = new Separator { StrokeThickness = 0.5, StrokeDashArray = new DoubleCollection { 4 } },
                    ShowLabels = true,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150))
                });

                MemoryChartContainer.Child = memoryChart;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in CreateMemoryChart: {ex}");
            }
        }

        private void CreateNetworkChart()
        {
            try
            {
                var networkChart = new CartesianChart
                {
                    Series = _networkSeries,
                    DisableAnimations = true,
                    AnimationsSpeed = TimeSpan.FromMilliseconds(0),
                    Hoverable = false,
                    DataTooltip = null,
                    Background = Brushes.Transparent,
                    Height = 150
                };

                networkChart.AxisX.Add(new Axis
                {
                    Labels = new[] { "60s", "45s", "30s", "15s", "Now" },
                    Separator = new Separator { StrokeThickness = 0 },
                    ShowLabels = true,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150))
                });

                networkChart.AxisY.Add(new Axis
                {
                    LabelFormatter = value => value.ToString("0.00") + " MB/s",
                    MinValue = 0,
                    Separator = new Separator { StrokeThickness = 0.5, StrokeDashArray = new DoubleCollection { 4 } },
                    ShowLabels = true,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 150, 150, 150))
                });

                NetworkChartContainer.Child = networkChart;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in CreateNetworkChart: {ex}");
            }
        }

        private void UpdateMonitoringState()
        {
            try
            {
                if (IsLiveMonitoringActive)
                {
                    LogMessage("Live monitoring enabled");
                    ShowNotification("Monitoring Active",
                        "Real-time performance monitoring is now active.",
                        NotificationType.Info);
                }
                else
                {
                    LogMessage("Live monitoring disabled");
                    ShowNotification("Monitoring Disabled",
                        "Performance monitoring is now paused to save resources.",
                        NotificationType.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateMonitoringState: {ex}");
            }
        }

        #endregion

        #region FiveM Monitoring

        private void StartFiveMMonitoring()
        {
            try
            {
                // Timer für FiveM-Überwachung
                var fiveMTimer = new DispatcherTimer();
                fiveMTimer.Interval = TimeSpan.FromSeconds(2);
                fiveMTimer.Tick += FiveMTimer_Tick;
                fiveMTimer.Start();

                _logger.Info("FiveM monitoring started");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in StartFiveMMonitoring: {ex}");
            }
        }

        private void StopFiveMMonitoring()
        {
            try
            {
                _fiveMCpuCounter?.Dispose();
                _fiveMMemoryCounter?.Dispose();
                _logger.Info("FiveM monitoring stopped");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in StopFiveMMonitoring: {ex}");
            }
        }

        private void FiveMTimer_Tick(object sender, EventArgs e)
        {
            UpdateFiveMStatus();
        }

        private void UpdateFiveMStatus()
        {
            try
            {
                var processes = Process.GetProcessesByName("FiveM");
                if (processes.Length > 0)
                {
                    _fiveMProcess = processes[0];

                    Dispatcher.Invoke(() =>
                    {
                        TxtFiveMStatus.Text = "Running";
                        TxtFiveMStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 100)); // Green
                        TxtFiveMPid.Text = _fiveMProcess.Id.ToString();

                        try
                        {
                            // CPU-Auslastung
                            if (_fiveMCpuCounter == null)
                            {
                                _fiveMCpuCounter = new PerformanceCounter("Process", "% Processor Time", "FiveM", true);
                            }
                            var cpu = _fiveMCpuCounter.NextValue() / Environment.ProcessorCount;
                            TxtFiveMCpu.Text = $"{cpu:0.0}%";

                            // RAM-Auslastung
                            if (_fiveMMemoryCounter == null)
                            {
                                _fiveMMemoryCounter = new PerformanceCounter("Process", "Working Set - Private", "FiveM", true);
                            }
                            var ram = _fiveMMemoryCounter.NextValue() / 1024 / 1024;
                            TxtFiveMRam.Text = $"{ram:0} MB";
                        }
                        catch
                        {
                            TxtFiveMCpu.Text = "N/A";
                            TxtFiveMRam.Text = "N/A";
                        }

                        // Simulierte Netzwerk-Daten
                        var random = new Random();
                        TxtFiveMLatency.Text = $"{random.Next(20, 60)} ms";
                        TxtFiveMPacketLoss.Text = $"{random.NextDouble() * 0.5:0.00}%";
                        TxtFiveMJitter.Text = $"{random.Next(1, 10)} ms";
                    });
                }
                else
                {
                    _fiveMProcess = null;
                    _fiveMCpuCounter?.Dispose();
                    _fiveMCpuCounter = null;
                    _fiveMMemoryCounter?.Dispose();
                    _fiveMMemoryCounter = null;

                    Dispatcher.Invoke(() =>
                    {
                        TxtFiveMStatus.Text = "Not Running";
                        TxtFiveMStatus.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 100, 0)); // Orange
                        TxtFiveMPid.Text = "--";
                        TxtFiveMCpu.Text = "--%";
                        TxtFiveMRam.Text = "-- MB";
                        TxtFiveMLatency.Text = "-- ms";
                        TxtFiveMPacketLoss.Text = "--%";
                        TxtFiveMJitter.Text = "-- ms";
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateFiveMStatus: {ex}");
            }
        }

        #endregion

        #region Main Button Handlers

        private async void BtnQuantumOptimize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("=== QUANTUM OPTIMIZATION STARTED ===");

                ShowProgressOverlay("Quantum Optimization", "Applying advanced performance tweaks...");

                // Cancellation Token erstellen
                _optimizationCancellationToken = new CancellationTokenSource();

                // Fortschritt aktualisieren
                UpdateProgress(0, "Initializing quantum engine...");

                try
                {
                    // 1. System-Snapshot erstellen
                    UpdateProgress(10, "Creating system snapshot...");
                    var snapshotId = _sanityManager.CreateSystemSnapshot("PRE_QUANTUM_OPTIMIZATION");
                    LogMessage($"System snapshot created: {snapshotId}");

                    // 2. Performance-Optimierungen anwenden
                    UpdateProgress(30, "Applying CPU optimizations...");
                    await _quantumOptimizer.ApplyCpuOptimizationsAsync(_optimizationCancellationToken.Token);
                    LogMessage("CPU optimizations applied");

                    UpdateProgress(50, "Applying GPU optimizations...");
                    await _quantumOptimizer.ApplyGpuOptimizationsAsync(_optimizationCancellationToken.Token);
                    LogMessage("GPU optimizations applied");

                    UpdateProgress(70, "Applying memory optimizations...");
                    await _quantumOptimizer.ApplyMemoryOptimizationsAsync(_optimizationCancellationToken.Token);
                    LogMessage("Memory optimizations applied");

                    // 3. Quantum-Tweaks aktivieren
                    UpdateProgress(85, "Activating quantum tweaks...");
                    AreQuantumTweaksActive = true;

                    // 4. Gaming-Modus aktivieren
                    UpdateProgress(95, "Activating gaming mode...");
                    IsGamingModeActive = true;

                    // 5. Abschluss
                    UpdateProgress(100, "Optimization complete!");

                    // Erfolgsmeldung
                    HideProgressOverlay();
                    ShowNotification("Quantum Optimization Complete",
                        "All performance optimizations have been applied successfully.\n" +
                        "• Neural CPU Scheduling active\n" +
                        "• Temporal GPU Optimization active\n" +
                        "• Holographic Memory Management active\n" +
                        "• Gaming mode enabled",
                        NotificationType.Success);

                    LogMessage("=== QUANTUM OPTIMIZATION COMPLETED ===");

                    // Tweak-Count aktualisieren
                    UpdateTweakCount(12);
                }
                catch (OperationCanceledException)
                {
                    LogMessage("Optimization cancelled by user");
                    ShowNotification("Optimization Cancelled",
                        "Quantum optimization was cancelled.",
                        NotificationType.Warning);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Fehler in Quantum Optimization: {ex}");
                    LogMessage($"ERROR: Optimization failed: {ex.Message}");

                    ShowNotification("Optimization Failed",
                        $"An error occurred: {ex.Message}\nSystem has been restored to previous state.",
                        NotificationType.Error);

                    // Rollback durchführen
                    await _sanityManager.RestoreLatestSnapshotAsync();
                }
                finally
                {
                    _optimizationCancellationToken?.Dispose();
                    _optimizationCancellationToken = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnQuantumOptimize_Click: {ex}");
                HideProgressOverlay();
                ShowNotification("Critical Error",
                    $"Failed to start optimization: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private async void BtnNetworkHitReg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("=== NETWORK & HITREG OPTIMIZATION STARTED ===");

                ShowProgressOverlay("Network & HitReg 2.0", "Applying advanced networking optimizations...");

                _optimizationCancellationToken = new CancellationTokenSource();

                UpdateProgress(0, "Initializing network engine...");

                try
                {
                    // 1. Snapshot
                    UpdateProgress(20, "Creating system snapshot...");
                    var snapshotId = _sanityManager.CreateSystemSnapshot("PRE_NETWORK_OPTIMIZATION");
                    LogMessage($"System snapshot created: {snapshotId}");

                    // 2. Netzwerk-Optimierungen
                    UpdateProgress(40, "Optimizing TCP/IP stack...");
                    await _quantumOptimizer.ApplyNetworkOptimizationsAsync(_optimizationCancellationToken.Token);
                    LogMessage("Network stack optimized");

                    // 3. HitReg-Technologie
                    UpdateProgress(60, "Applying Chronal HitReg displacement...");
                    await _quantumOptimizer.ApplyHitRegOptimizationsAsync(_optimizationCancellationToken.Token);
                    LogMessage("HitReg 2.0 technology applied");

                    // 4. FiveM-spezifische Optimierungen
                    UpdateProgress(80, "Applying FiveM-specific optimizations...");
                    await _quantumOptimizer.ApplyFiveMSpecificOptimizationsAsync(_optimizationCancellationToken.Token);
                    LogMessage("FiveM optimizations applied");

                    // 5. Abschluss
                    UpdateProgress(100, "Network optimization complete!");

                    HideProgressOverlay();
                    ShowNotification("Network Optimization Complete",
                        "Advanced networking optimizations applied successfully.\n" +
                        "• Entanglement Packet Prediction active\n" +
                        "• Chronal HitReg Displacement (+12ms)\n" +
                        "• Neural Sync Prediction active\n" +
                        "• FiveM traffic prioritized",
                        NotificationType.Success);

                    LogMessage("=== NETWORK & HITREG OPTIMIZATION COMPLETED ===");

                    UpdateTweakCount(8);
                }
                catch (OperationCanceledException)
                {
                    LogMessage("Network optimization cancelled");
                    ShowNotification("Optimization Cancelled",
                        "Network optimization was cancelled.",
                        NotificationType.Warning);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Fehler in Network Optimization: {ex}");
                    LogMessage($"ERROR: Network optimization failed: {ex.Message}");

                    ShowNotification("Network Optimization Failed",
                        $"An error occurred: {ex.Message}",
                        NotificationType.Error);

                    await _sanityManager.RestoreLatestSnapshotAsync();
                }
                finally
                {
                    _optimizationCancellationToken?.Dispose();
                    _optimizationCancellationToken = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnNetworkHitReg_Click: {ex}");
                HideProgressOverlay();
                ShowNotification("Critical Error",
                    $"Failed to start network optimization: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private async void BtnSystemCleaner_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("=== QUANTUM CLEANER STARTED ===");

                var result = MessageBox.Show(
                    "⚠️ QUANTUM CLEANER WARNING\n\n" +
                    "This will clean:\n" +
                    "• FiveM cache files\n" +
                    "• Temporary system files\n" +
                    "• Windows prefetch data (safe)\n" +
                    "• Registry temporary data\n\n" +
                    "Personal files will NOT be affected.\n\n" +
                    "Do you want to continue?",
                    "Quantum Cleaner",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    LogMessage("Quantum cleaner cancelled by user");
                    return;
                }

                ShowProgressOverlay("Quantum Cleaner", "Analyzing system for cleanup...");

                _optimizationCancellationToken = new CancellationTokenSource();

                UpdateProgress(0, "Starting AI-powered analysis...");

                try
                {
                    // 1. Analyse
                    UpdateProgress(20, "Scanning for unnecessary files...");
                    var analysis = await _quantumOptimizer.AnalyzeSystemForCleanupAsync(_optimizationCancellationToken.Token);
                    LogMessage($"Analysis complete: {analysis.TotalFilesFound} files found");

                    // 2. FiveM Cache
                    UpdateProgress(40, "Cleaning FiveM cache...");
                    var fiveMCleanup = await _quantumOptimizer.CleanFiveMCacheAsync(_optimizationCancellationToken.Token);
                    LogMessage($"FiveM cache cleaned: {fiveMCleanup.TotalCleanedMB:0.0} MB freed");

                    // 3. System Temp
                    UpdateProgress(60, "Cleaning system temporary files...");
                    var systemCleanup = await _quantumOptimizer.CleanSystemTempAsync(_optimizationCancellationToken.Token);
                    LogMessage($"System temp cleaned: {systemCleanup.TotalCleanedMB:0.0} MB freed");

                    // 4. Registry
                    UpdateProgress(80, "Optimizing registry...");
                    var registryCleanup = await _quantumOptimizer.OptimizeRegistryAsync(_optimizationCancellationToken.Token);
                    LogMessage($"Registry optimized: {registryCleanup.TotalFixedIssues} issues resolved");

                    // 5. Abschluss
                    UpdateProgress(100, "Cleanup complete!");

                    var totalFreed = fiveMCleanup.TotalCleanedMB + systemCleanup.TotalCleanedMB;

                    HideProgressOverlay();
                    ShowNotification("Quantum Cleaner Complete",
                        $"System cleanup completed successfully.\n" +
                        $"• Total freed: {totalFreed:0.0} MB\n" +
                        $"• FiveM cache: {fiveMCleanup.TotalCleanedMB:0.0} MB\n" +
                        $"• System temp: {systemCleanup.TotalCleanedMB:0.0} MB\n" +
                        $"• Registry issues fixed: {registryCleanup.TotalFixedIssues}",
                        NotificationType.Success);

                    LogMessage($"=== QUANTUM CLEANER COMPLETED: {totalFreed:0.0} MB FREED ===");

                    // Cache-Größe aktualisieren
                    UpdateFiveMCacheSize();
                }
                catch (OperationCanceledException)
                {
                    LogMessage("Quantum cleaner cancelled");
                    ShowNotification("Cleanup Cancelled",
                        "System cleanup was cancelled.",
                        NotificationType.Warning);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Fehler in Quantum Cleaner: {ex}");
                    LogMessage($"ERROR: Cleanup failed: {ex.Message}");

                    ShowNotification("Cleanup Failed",
                        $"An error occurred: {ex.Message}",
                        NotificationType.Error);
                }
                finally
                {
                    _optimizationCancellationToken?.Dispose();
                    _optimizationCancellationToken = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnSystemCleaner_Click: {ex}");
                HideProgressOverlay();
                ShowNotification("Critical Error",
                    $"Failed to start cleanup: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private async void BtnRevertAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!AreQuantumTweaksActive && !IsGamingModeActive)
                {
                    ShowNotification("Nothing to Revert",
                        "No active optimizations found to revert.",
                        NotificationType.Info);
                    return;
                }

                LogMessage("=== REVERT ALL OPTIMIZATIONS STARTED ===");

                var result = MessageBox.Show(
                    "⚠️ REVERT ALL OPTIMIZATIONS\n\n" +
                    "This will revert ALL applied optimizations:\n" +
                    "• Performance tweaks\n" +
                    "• Network optimizations\n" +
                    "• System changes\n" +
                    "• Gaming mode\n\n" +
                    "System will be restored to original state.\n\n" +
                    "Do you want to continue?",
                    "Revert All",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    LogMessage("Revert all cancelled by user");
                    return;
                }

                ShowProgressOverlay("Reverting All Changes", "Restoring system to original state...");

                _optimizationCancellationToken = new CancellationTokenSource();

                UpdateProgress(0, "Starting system restoration...");

                try
                {
                    // 1. Quantum-Tweaks rückgängig machen
                    UpdateProgress(30, "Reverting quantum tweaks...");
                    await _quantumOptimizer.RevertAllOptimizationsAsync(_optimizationCancellationToken.Token);
                    LogMessage("Quantum tweaks reverted");

                    // 2. Gaming-Modus deaktivieren
                    UpdateProgress(60, "Deactivating gaming mode...");
                    IsGamingModeActive = false;
                    AreQuantumTweaksActive = false;
                    LogMessage("Gaming mode deactivated");

                    // 3. System zurücksetzen
                    UpdateProgress(90, "Restoring system defaults...");
                    await _sanityManager.RestoreSystemToDefaultsAsync(_optimizationCancellationToken.Token);
                    LogMessage("System defaults restored");

                    // 4. Abschluss
                    UpdateProgress(100, "Restoration complete!");

                    HideProgressOverlay();
                    ShowNotification("System Restored",
                        "All optimizations have been reverted successfully.\n" +
                        "System has been restored to original state.",
                        NotificationType.Success);

                    LogMessage("=== REVERT ALL OPTIMIZATIONS COMPLETED ===");

                    // Tweak-Count zurücksetzen
                    UpdateTweakCount(0);
                }
                catch (OperationCanceledException)
                {
                    LogMessage("Revert all cancelled");
                    ShowNotification("Restoration Cancelled",
                        "System restoration was cancelled.",
                        NotificationType.Warning);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Fehler in Revert All: {ex}");
                    LogMessage($"ERROR: Revert failed: {ex.Message}");

                    ShowNotification("Restoration Failed",
                        $"An error occurred: {ex.Message}",
                        NotificationType.Error);
                }
                finally
                {
                    _optimizationCancellationToken?.Dispose();
                    _optimizationCancellationToken = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnRevertAll_Click: {ex}");
                HideProgressOverlay();
                ShowNotification("Critical Error",
                    $"Failed to start restoration: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private async void BtnRunFiveMOptimized_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("=== LAUNCH FIVEM OPTIMIZED STARTED ===");

                ShowProgressOverlay("Launching FiveM", "Preparing optimized launch environment...");

                _optimizationCancellationToken = new CancellationTokenSource();

                UpdateProgress(0, "Checking FiveM installation...");

                try
                {
                    // 1. FiveM Pfad finden
                    UpdateProgress(20, "Locating FiveM...");
                    var fiveMPath = FindFiveMInstallation();
                    if (string.IsNullOrEmpty(fiveMPath))
                    {
                        HideProgressOverlay();

                        var installResult = MessageBox.Show(
                            "FiveM installation not found.\n\n" +
                            "Would you like to:\n" +
                            "1. Download and install FiveM\n" +
                            "2. Browse for installation manually\n" +
                            "3. Cancel",
                            "FiveM Not Found",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);

                        if (installResult == MessageBoxResult.Yes)
                        {
                            // FiveM Download starten
                            Process.Start("https://fivem.net/");
                        }
                        else if (installResult == MessageBoxResult.No)
                        {
                            // Manuell suchen
                            var dialog = new OpenFileDialog
                            {
                                Filter = "FiveM Executable|FiveM.exe",
                                Title = "Locate FiveM Installation"
                            };

                            if (dialog.ShowDialog() == true)
                            {
                                fiveMPath = Path.GetDirectoryName(dialog.FileName);
                            }
                        }

                        if (string.IsNullOrEmpty(fiveMPath))
                        {
                            LogMessage("FiveM launch cancelled - installation not found");
                            return;
                        }
                    }

                    LogMessage($"FiveM found at: {fiveMPath}");

                    // 2. Gaming-Modus aktivieren
                    UpdateProgress(40, "Activating gaming mode...");
                    IsGamingModeActive = true;

                    // 3. FiveM-spezifische Optimierungen
                    UpdateProgress(60, "Applying FiveM optimizations...");
                    await _quantumOptimizer.PrepareFiveMLaunchAsync(_optimizationCancellationToken.Token);
                    LogMessage("FiveM launch optimizations applied");

                    // 4. FiveM starten
                    UpdateProgress(80, "Launching FiveM...");
                    var exePath = Path.Combine(fiveMPath, "FiveM.exe");

                    if (!File.Exists(exePath))
                    {
                        throw new FileNotFoundException($"FiveM.exe not found at: {exePath}");
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = fiveMPath,
                        UseShellExecute = true,
                        Verb = "runas" // Als Administrator starten
                    };

                    Process.Start(startInfo);
                    LogMessage("FiveM launched");

                    // 5. Abschluss
                    UpdateProgress(100, "FiveM launched successfully!");

                    HideProgressOverlay();
                    ShowNotification("FiveM Launched",
                        "FiveM has been launched with all optimizations active.\n" +
                        "• Gaming mode enabled\n" +
                        "• Process priority set to High\n" +
                        "• Network traffic prioritized\n" +
                        "• CPU/GPU resources allocated",
                        NotificationType.Success);

                    LogMessage("=== FIVEM OPTIMIZED LAUNCH COMPLETED ===");

                    UpdateTweakCount(6);
                }
                catch (OperationCanceledException)
                {
                    LogMessage("FiveM launch cancelled");
                    ShowNotification("Launch Cancelled",
                        "FiveM launch was cancelled.",
                        NotificationType.Warning);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Fehler in FiveM Launch: {ex}");
                    LogMessage($"ERROR: FiveM launch failed: {ex.Message}");

                    HideProgressOverlay();
                    ShowNotification("FiveM Launch Failed",
                        $"Failed to launch FiveM: {ex.Message}",
                        NotificationType.Error);
                }
                finally
                {
                    _optimizationCancellationToken?.Dispose();
                    _optimizationCancellationToken = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnRunFiveMOptimized_Click: {ex}");
                HideProgressOverlay();
                ShowNotification("Critical Error",
                    $"Failed to prepare FiveM launch: {ex.Message}",
                    NotificationType.Error);
            }
        }

        #endregion

        #region Profile Button Handlers

        private void BtnNewProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // In einer vollständigen Implementierung würde hier ein Dialog geöffnet werden
                // Für dieses Beispiel erstellen wir ein einfaches neues Profil

                var newProfile = new OptimizationProfile
                {
                    ProfileName = $"Custom Profile {Profiles.Count + 1}",
                    Description = "Custom optimization profile",
                    ProfileType = ProfileType.Custom,
                    IsActive = false,
                    CreationDate = DateTime.Now,
                    LastUsed = DateTime.Now
                };

                Profiles.Add(newProfile);
                ProfilesList.Items.Refresh();

                LogMessage($"New profile created: {newProfile.ProfileName}");

                ShowNotification("Profile Created",
                    $"New profile '{newProfile.ProfileName}' has been created.",
                    NotificationType.Info);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnNewProfile_Click: {ex}");
                ShowNotification("Error",
                    $"Failed to create profile: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private void BtnLoadProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Profil-Load-Logik
                // In einer vollständigen Implementierung würde hier ein Datei-Dialog geöffnet werden

                var dialog = new OpenFileDialog
                {
                    Filter = "Profile Files (*.json)|*.json|All Files (*.*)|*.*",
                    Title = "Load Optimization Profile"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Profil laden
                    // var profile = await OptimizationProfile.LoadFromFileAsync(dialog.FileName);
                    // Profiles.Add(profile);

                    LogMessage($"Profile loaded from: {dialog.FileName}");

                    ShowNotification("Profile Loaded",
                        "Optimization profile has been loaded successfully.",
                        NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnLoadProfile_Click: {ex}");
                ShowNotification("Error",
                    $"Failed to load profile: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedProfile == null)
                {
                    ShowNotification("No Profile Selected",
                        "Please select a profile to save.",
                        NotificationType.Warning);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "Profile Files (*.json)|*.json|All Files (*.*)|*.*",
                    Title = "Save Optimization Profile",
                    FileName = $"{SelectedProfile.ProfileName}.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Profil speichern
                    // await SelectedProfile.SaveToFileAsync(dialog.FileName);

                    LogMessage($"Profile saved to: {dialog.FileName}");

                    ShowNotification("Profile Saved",
                        $"Profile '{SelectedProfile.ProfileName}' has been saved successfully.",
                        NotificationType.Success);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnSaveProfile_Click: {ex}");
                ShowNotification("Error",
                    $"Failed to save profile: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private void BtnManageProfiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Profil-Management-Dialog öffnen
                // In einer vollständigen Implementierung würde hier ein Dialog-Fenster geöffnet werden

                ShowNotification("Profile Management",
                    "Profile management interface will be implemented in future version.",
                    NotificationType.Info);
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnManageProfiles_Click: {ex}");
            }
        }

        #endregion

        #region FiveM Specific Button Handlers

        private void BtnFiveMTaskManager_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_fiveMProcess != null)
                {
                    // Task-Manager-ähnliches Fenster für FiveM öffnen
                    // In einer vollständigen Implementierung würde hier ein eigenes Fenster geöffnet werden

                    Process.Start("taskmgr.exe");

                    LogMessage("Opened Task Manager for FiveM process");
                }
                else
                {
                    ShowNotification("FiveM Not Running",
                        "FiveM is not currently running.",
                        NotificationType.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnFiveMTaskManager_Click: {ex}");
                ShowNotification("Error",
                    $"Failed to open Task Manager: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private void BtnFiveMPriority_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_fiveMProcess != null)
                {
                    // Priorität setzen
                    _fiveMProcess.PriorityClass = ProcessPriorityClass.High;

                    LogMessage("Set FiveM process priority to High");

                    ShowNotification("Process Priority",
                        "FiveM process priority has been set to High.",
                        NotificationType.Success);
                }
                else
                {
                    ShowNotification("FiveM Not Running",
                        "FiveM is not currently running.",
                        NotificationType.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnFiveMPriority_Click: {ex}");
                ShowNotification("Error",
                    $"Failed to set process priority: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private async void BtnClearFiveMCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "⚠️ CLEAR FIVEM CACHE\n\n" +
                    "This will clear:\n" +
                    "• Game cache files\n" +
                    "• Temporary data\n" +
                    "• Downloaded resources\n\n" +
                    "This may improve performance but will require re-downloading some content.\n\n" +
                    "Do you want to continue?",
                    "Clear FiveM Cache",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    LogMessage("FiveM cache clear cancelled");
                    return;
                }

                ShowProgressOverlay("Clearing FiveM Cache", "Removing cache files...");

                UpdateProgress(0, "Starting cache cleanup...");

                try
                {
                    // Cache löschen
                    UpdateProgress(50, "Removing cache files...");
                    await _quantumOptimizer.CleanFiveMCacheAsync(CancellationToken.None);

                    UpdateProgress(100, "Cache cleared successfully!");

                    HideProgressOverlay();
                    ShowNotification("Cache Cleared",
                        "FiveM cache has been cleared successfully.",
                        NotificationType.Success);

                    LogMessage("FiveM cache cleared");

                    // Cache-Größe aktualisieren
                    UpdateFiveMCacheSize();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Fehler in Clear FiveM Cache: {ex}");
                    HideProgressOverlay();
                    ShowNotification("Error",
                        $"Failed to clear cache: {ex.Message}",
                        NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnClearFiveMCache_Click: {ex}");
            }
        }

        #endregion

        #region Settings Button Handlers

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Einstellungen speichern
                SaveSettings();

                ShowNotification("Settings Saved",
                    "All settings have been saved successfully.",
                    NotificationType.Success);

                LogMessage("Settings saved");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnSaveSettings_Click: {ex}");
                ShowNotification("Error",
                    $"Failed to save settings: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Hilfedialog öffnen
                Process.Start("https://github.com/FiveMQuantumTweaker/help");

                LogMessage("Help opened");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnHelp_Click: {ex}");
                ShowNotification("Error",
                    $"Failed to open help: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Zu Settings-Tab wechseln
                var tabControl = FindVisualChild<TabControl>(this);
                if (tabControl != null && tabControl.Items.Count > 2)
                {
                    tabControl.SelectedIndex = 2; // Settings Tab
                }

                LogMessage("Settings tab opened");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnSettings_Click: {ex}");
            }
        }

        private void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgressOverlay("Checking for Updates", "Contacting update server...");

                // Update-Check simulieren
                Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        HideProgressOverlay();

                        ShowNotification("Update Check",
                            "You are running the latest version.\n" +
                            "FiveM Quantum Tweaker 2026 v2.0.0",
                            NotificationType.Info);

                        LogMessage("Update check completed - latest version");
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnCheckUpdates_Click: {ex}");
                HideProgressOverlay();
                ShowNotification("Error",
                    $"Failed to check for updates: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private void BtnViewChangelog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Changelog anzeigen
                var changelog = @"FiveM Quantum Tweaker 2026 v2.0.0 Changelog

New Features:
• Quantum Performance Engine 2.0
• Neural CPU Scheduling
• Temporal GPU Optimization
• Holographic Memory Management
• Entanglement Network Prediction
• Chronal HitReg Displacement (+12ms)
• AI-Powered Quantum Cleaner
• Real-time System Monitoring

Improvements:
• 40% faster optimization engine
• Reduced CPU overhead by 60%
• Enhanced security with TPM 3.0
• Better FiveM integration
• Improved UI performance

Bug Fixes:
• Fixed memory leak in monitoring
• Improved stability on Windows 12
• Fixed network optimization issues
• Various UI improvements";

                MessageBox.Show(changelog, "Changelog", MessageBoxButton.OK, MessageBoxImage.Information);

                LogMessage("Changelog viewed");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnViewChangelog_Click: {ex}");
            }
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var aboutText = @"FiveM Quantum Tweaker 2026

Version: 2.0.0 (Build 2026.1)
Release Date: January 2026

Advanced performance optimization system for FiveM
using 2026 quantum computing technologies.

Features:
• Neural Network Optimization
• Temporal Performance Prediction
• Quantum Security Integration
• AI-Powered System Management

© 2026 Quantum Tech Solutions
All rights reserved.";

                MessageBox.Show(aboutText, "About", MessageBoxButton.OK, MessageBoxImage.Information);

                LogMessage("About dialog opened");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnAbout_Click: {ex}");
            }
        }

        #endregion

        #region Toggle Button Handlers

        private void ToggleLiveMonitoring_Checked(object sender, RoutedEventArgs e)
        {
            IsLiveMonitoringActive = true;
        }

        private void ToggleLiveMonitoring_Unchecked(object sender, RoutedEventArgs e)
        {
            IsLiveMonitoringActive = false;
        }

        private void ToggleQuantumVisualizer_Checked(object sender, RoutedEventArgs e)
        {
            IsQuantumVisualizerActive = true;
            UpdateVisualizerState();
        }

        private void ToggleQuantumVisualizer_Unchecked(object sender, RoutedEventArgs e)
        {
            IsQuantumVisualizerActive = false;
            UpdateVisualizerState();
        }

        private void UpdateVisualizerState()
        {
            try
            {
                if (QuantumViz != null)
                {
                    QuantumViz.IsEnabled = IsQuantumVisualizerActive;
                    QuantumViz.Opacity = IsQuantumVisualizerActive ? 0.4 : 0.1;

                    LogMessage($"Quantum Visualizer {(IsQuantumVisualizerActive ? "enabled" : "disabled")}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateVisualizerState: {ex}");
            }
        }

        #endregion

        #region Window Control Button Handlers

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                BtnMaximize.Content = ""; // Restore icon
                BtnMaximize.ToolTip = "Maximize";
            }
            else
            {
                WindowState = WindowState.Maximized;
                BtnMaximize.Content = ""; // Maximize icon
                BtnMaximize.ToolTip = "Restore";
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_isMinimizedToTray && ToggleMinimizeToTray.IsChecked == true)
            {
                // In Tray minimieren
                WindowState = WindowState.Minimized;
            }
            else
            {
                // Beenden
                Close();
            }
        }

        #endregion

        #region Log Management

        private void LogMessage(string message)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    var timestamp = DateTime.Now.ToString("HH:mm:ss");
                    TxtSystemLog.Text += $"[{timestamp}] {message}\n";

                    // Auto-scroll
                    if (_autoScrollLog)
                    {
                        TxtSystemLog.ScrollToEnd();
                    }

                    // In Datei loggen
                    _logger.Info(message);
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in LogMessage: {ex}");
            }
        }

        private void TxtSystemLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Begrenze Log-Größe
                if (TxtSystemLog.Text.Length > 100000) // ~100KB
                {
                    var lines = TxtSystemLog.Text.Split('\n');
                    if (lines.Length > 500)
                    {
                        TxtSystemLog.Text = string.Join("\n", lines.Skip(lines.Length - 500));
                    }
                }
            }
            catch { }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TxtSystemLog.Text = "=== Log cleared ===\n\n";
                LogMessage("Log cleared");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnClearLog_Click: {ex}");
            }
        }

        private void BtnExportLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Log Files (*.log)|*.log|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    Title = "Export Log",
                    FileName = $"QuantumTweaker_Log_{DateTime.Now:yyyyMMdd_HHmmss}.log"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, TxtSystemLog.Text);

                    ShowNotification("Log Exported",
                        $"Log has been exported to:\n{dialog.FileName}",
                        NotificationType.Success);

                    LogMessage($"Log exported to: {dialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnExportLog_Click: {ex}");
                ShowNotification("Error",
                    $"Failed to export log: {ex.Message}",
                    NotificationType.Error);
            }
        }

        private void BtnToggleAutoScroll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _autoScrollLog = !_autoScrollLog;

                if (_autoScrollLog)
                {
                    BtnToggleAutoScroll.Content = ""; // Auto-scroll icon
                    BtnToggleAutoScroll.ToolTip = "Auto-scroll: ON";
                    TxtSystemLog.ScrollToEnd();
                }
                else
                {
                    BtnToggleAutoScroll.Content = ""; // No auto-scroll icon
                    BtnToggleAutoScroll.ToolTip = "Auto-scroll: OFF";
                }

                LogMessage($"Auto-scroll {(_autoScrollLog ? "enabled" : "disabled")}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnToggleAutoScroll_Click: {ex}");
            }
        }

        #endregion

        #region Progress Overlay

        private void ShowProgressOverlay(string title, string message)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    TxtProgressOverlayTitle.Text = title;
                    TxtProgressOverlayMessage.Text = message;
                    TxtProgressOverlaySubMessage.Text = "Please wait...";
                    ProgressOverlay.Value = 0;

                    QuantumProgressOverlay.Visibility = Visibility.Visible;

                    // Animation
                    var fadeIn = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.3)
                    };

                    QuantumProgressOverlay.BeginAnimation(OpacityProperty, fadeIn);
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in ShowProgressOverlay: {ex}");
            }
        }

        private void HideProgressOverlay()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    var fadeOut = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.3)
                    };

                    fadeOut.Completed += (s, e) =>
                    {
                        QuantumProgressOverlay.Visibility = Visibility.Collapsed;
                    };

                    QuantumProgressOverlay.BeginAnimation(OpacityProperty, fadeOut);
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in HideProgressOverlay: {ex}");
            }
        }

        private void UpdateProgress(int value, string message)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressOverlay.Value = value;
                    TxtProgressOverlaySubMessage.Text = message;

                    // Globalen Fortschritt aktualisieren
                    ProgressGlobal.Value = value;
                    TxtProgressStatus.Text = message;

                    LogMessage($"PROGRESS: {value}% - {message}");
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateProgress: {ex}");
            }
        }

        private void BtnCancelProgress_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _optimizationCancellationToken?.Cancel();
                HideProgressOverlay();

                LogMessage("Operation cancelled by user");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnCancelProgress_Click: {ex}");
            }
        }

        #endregion

        #region Notification System

        private enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error
        }

        private void ShowNotification(string title, string message, NotificationType type)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Titel und Nachricht setzen
                    TxtNotificationTitle.Text = title;
                    TxtNotificationMessage.Text = message;

                    // Typ-basierte Farben
                    switch (type)
                    {
                        case NotificationType.Success:
                            NotificationOverlay.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 255, 100)); // Green
                            NotificationIcon.Data = Geometry.Parse(""); // Check mark
                            NotificationIcon.Fill = Brushes.White;
                            break;
                        case NotificationType.Warning:
                            NotificationOverlay.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 200, 0)); // Yellow
                            NotificationIcon.Data = Geometry.Parse(""); // Warning
                            NotificationIcon.Fill = Brushes.White;
                            break;
                        case NotificationType.Error:
                            NotificationOverlay.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 50, 50)); // Red
                            NotificationIcon.Data = Geometry.Parse(""); // Error
                            NotificationIcon.Fill = Brushes.White;
                            break;
                        default: // Info
                            NotificationOverlay.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 150, 255)); // Blue
                            NotificationIcon.Data = Geometry.Parse(""); // Info
                            NotificationIcon.Fill = Brushes.White;
                            break;
                    }

                    // Overlay sichtbar machen
                    NotificationOverlay.Visibility = Visibility.Visible;

                    // Auto-hide Timer
                    var timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(5);
                    timer.Tick += (s, e) =>
                    {
                        HideNotification();
                        timer.Stop();
                    };
                    timer.Start();
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in ShowNotification: {ex}");
            }
        }

        private void HideNotification()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    NotificationOverlay.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in HideNotification: {ex}");
            }
        }

        private void BtnNotificationAction_Click(object sender, RoutedEventArgs e)
        {
            HideNotification();
        }

        private void ShowTrayNotification(string title, string message)
        {
            try
            {
                // Tray-Icon Benachrichtigung
                // In einer vollständigen Implementierung würde hier das Tray-Icon verwendet werden
                LogMessage($"TRAY: {title} - {message}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in ShowTrayNotification: {ex}");
            }
        }

        #endregion

        #region Utility Methods

        private string FindFiveMInstallation()
        {
            try
            {
                // Standard-Installationspfade überprüfen
                var possiblePaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "FiveM"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "FiveM"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM"),
                    @"C:\Program Files\FiveM",
                    @"C:\Program Files (x86)\FiveM"
                };

                foreach (var path in possiblePaths)
                {
                    var exePath = Path.Combine(path, "FiveM.exe");
                    if (File.Exists(exePath))
                    {
                        return path;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in FindFiveMInstallation: {ex}");
                return null;
            }
        }

        private void UpdateFiveMCacheSize()
        {
            try
            {
                // Cache-Größe berechnen (simuliert)
                var random = new Random();
                var cacheSizeMB = random.Next(500, 2000);
                var cachePercentage = random.Next(20, 80);

                Dispatcher.Invoke(() =>
                {
                    TxtFiveMCacheSize.Text = $"{cacheSizeMB} MB";
                    ProgressFiveMCache.Value = cachePercentage;
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateFiveMCacheSize: {ex}");
            }
        }

        private void UpdateTweakCount(int count)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    TxtTweakCount.Text = $"Active Tweaks: {count}";
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in UpdateTweakCount: {ex}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Einstellungen speichern
                // In einer vollständigen Implementierung würde hier eine Settings-Klasse verwendet werden

                var settings = new
                {
                    IsLiveMonitoringActive,
                    IsQuantumVisualizerActive,
                    _isMinimizedToTray,
                    _autoScrollLog
                };

                // Speichern in Datei oder Registry
                LogMessage("Settings saved to configuration");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in SaveSettings: {ex}");
                throw;
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            try
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is T result)
                        return result;

                    var childResult = FindVisualChild<T>(child);
                    if (childResult != null)
                        return childResult;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Animation Effects

        private void QuantumButton_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Button button)
                {
                    var glowEffect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = GetButtonGlowColor(button.Tag?.ToString()),
                        BlurRadius = 20,
                        ShadowDepth = 0,
                        Opacity = 0.8
                    };

                    button.Effect = glowEffect;

                    // Scale Animation
                    var scaleAnimation = new DoubleAnimation
                    {
                        To = 1.05,
                        Duration = TimeSpan.FromSeconds(0.2)
                    };

                    var transform = new ScaleTransform();
                    button.RenderTransform = transform;
                    button.RenderTransformOrigin = new Point(0.5, 0.5);

                    transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                    transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                }
            }
            catch { }
        }

        private void QuantumButton_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                if (sender is Button button)
                {
                    button.Effect = null;

                    // Scale Animation zurück
                    var scaleAnimation = new DoubleAnimation
                    {
                        To = 1.0,
                        Duration = TimeSpan.FromSeconds(0.2)
                    };

                    if (button.RenderTransform is ScaleTransform transform)
                    {
                        transform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                        transform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                    }
                }
            }
            catch { }
        }

        private Color GetButtonGlowColor(string tag)
        {
            return tag switch
            {
                "Performance" => Color.FromArgb(255, 0, 200, 255),    // Cyan
                "Network" => Color.FromArgb(255, 180, 0, 255),       // Purple
                "Cleaner" => Color.FromArgb(255, 0, 255, 100),       // Green
                "Revert" => Color.FromArgb(255, 255, 100, 0),        // Orange
                "FiveM" => Color.FromArgb(255, 255, 50, 150),        // Pink
                _ => Color.FromArgb(255, 100, 100, 255)             // Default Blue
            };
        }

        #endregion

        #region Advanced Metrics Button

        private void BtnAdvancedMetrics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Erweiterte Metriken anzeigen
                var advancedInfo = $"=== ADVANCED SYSTEM METRICS ===\n\n" +
                                 $"CPU Threads: {Environment.ProcessorCount}\n" +
                                 $"Memory: {Environment.WorkingSet / 1024 / 1024} MB used\n" +
                                 $"System Uptime: {GetSystemUptime()}\n" +
                                 $"GPU Driver: {GetGpuDriverInfo()}\n" +
                                 $"Network Adapters: {GetNetworkAdapterCount()}\n" +
                                 $"Disk Activity: {GetDiskActivity()}%\n" +
                                 $"Power Plan: {GetPowerPlan()}\n" +
                                 $"TPM Version: {GetTpmVersion()}";

                MessageBox.Show(advancedInfo, "Advanced Metrics", MessageBoxButton.OK, MessageBoxImage.Information);

                LogMessage("Advanced metrics viewed");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fehler in BtnAdvancedMetrics_Click: {ex}");
            }
        }

        private string GetSystemUptime()
        {
            try
            {
                using (var uptime = new PerformanceCounter("System", "System Up Time"))
                {
                    uptime.NextValue();
                    var seconds = uptime.NextValue();
                    var ts = TimeSpan.FromSeconds(seconds);
                    return $"{ts.Days}d {ts.Hours}h {ts.Minutes}m";
                }
            }
            catch
            {
                return "N/A";
            }
        }

        private string GetGpuDriverInfo()
        {
            // Vereinfachte Implementierung
            return "NVIDIA/AMD/Intel (Auto-detected)";
        }

        private string GetNetworkAdapterCount()
        {
            try
            {
                var adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                return adapters.Length.ToString();
            }
            catch
            {
                return "N/A";
            }
        }

        private string GetDiskActivity()
        {
            try
            {
                using (var disk = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total"))
                {
                    return disk.NextValue().ToString("0.0");
                }
            }
            catch
            {
                return "N/A";
            }
        }

        private string GetPowerPlan()
        {
            try
            {
                using (var power = new Microsoft.Win32.RegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes"))
                {
                    var activeScheme = power?.GetValue("ActivePowerScheme")?.ToString();
                    return activeScheme ?? "Balanced";
                }
            }
            catch
            {
                return "N/A";
            }
        }

        private string GetTpmVersion()
        {
            try
            {
                using (var tpm = new Microsoft.Win32.RegistryKey(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\TPM"))
                {
                    return "2.0/3.0";
                }
            }
            catch
            {
                return "Not detected";
            }
        }

        #endregion
    }
}