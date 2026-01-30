using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace FiveMQuantumTweaker2026.UI
{
    /// <summary>
    /// Theme Manager für Dark/Lila Quantum Design
    /// </summary>
    public static class ThemeManager
    {
        // Theme-Konstanten
        public const string ThemeDark = "Dark";
        public const string ThemeQuantum = "Quantum";

        private static string _currentTheme = ThemeQuantum;

        // Quantum Color Palette
        public static readonly Color QuantumPrimary = Color.FromRgb(106, 0, 255);    // #6A00FF
        public static readonly Color QuantumSecondary = Color.FromRgb(157, 0, 255);  // #9D00FF
        public static readonly Color QuantumAccent = Color.FromRgb(0, 212, 255);     // #00D4FF
        public static readonly Color QuantumDark = Color.FromRgb(10, 10, 15);        // #0A0A0F
        public static readonly Color QuantumPanel = Color.FromRgb(21, 21, 32);       // #151520
        public static readonly Color QuantumBorder = Color.FromRgb(48, 48, 64);      // #303040
        public static readonly Color QuantumText = Color.FromRgb(255, 255, 255);     // #FFFFFF
        public static readonly Color QuantumTextSecondary = Color.FromRgb(160, 160, 176); // #A0A0B0

        // Dark Color Palette
        public static readonly Color DarkPrimary = Color.FromRgb(33, 33, 33);        // #212121
        public static readonly Color DarkSecondary = Color.FromRgb(66, 66, 66);      // #424242
        public static readonly Color DarkAccent = Color.FromRgb(0, 122, 204);        // #007ACC
        public static readonly Color DarkBackground = Color.FromRgb(15, 15, 15);     // #0F0F0F
        public static readonly Color DarkPanel = Color.FromRgb(25, 25, 25);          // #191919
        public static readonly Color DarkBorder = Color.FromRgb(51, 51, 51);         // #333333

        // Gradient Brushes
        public static LinearGradientBrush QuantumGradientBrush => new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop(QuantumPrimary, 0.0),
                new GradientStop(QuantumSecondary, 0.3),
                new GradientStop(QuantumAccent, 0.6),
                new GradientStop(QuantumPrimary, 1.0)
            }
        };

        public static LinearGradientButton QuantumButtonGradient => new LinearGradientButton
        {
            NormalGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(QuantumPrimary, 0.0),
                    new GradientStop(Color.FromRgb(86, 0, 205), 1.0)
                }
            },
            HoverGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(QuantumSecondary, 0.0),
                    new GradientStop(Color.FromRgb(126, 0, 235), 1.0)
                }
            },
            PressedGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(66, 0, 185), 0.0),
                    new GradientStop(QuantumPrimary, 1.0)
                }
            }
        };

        /// <summary>
        /// Aktuelles Theme
        /// </summary>
        public static string CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    OnThemeChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Event bei Theme-Änderung
        /// </summary>
        public static event EventHandler OnThemeChanged;

        /// <summary>
        /// Wende Theme auf Fenster an
        /// </summary>
        public static void ApplyTheme(Window window)
        {
            if (window == null) return;

            var resources = window.Resources;
            ApplyThemeToResourceDictionary(resources);

            // Window-Eigenschaften setzen
            window.Background = new SolidColorBrush(QuantumDark);
            window.Foreground = new SolidColorBrush(QuantumText);
            window.FontFamily = new FontFamily("Segoe UI");
        }

        /// <summary>
        /// Wende Theme auf ResourceDictionary an
        /// </summary>
        public static void ApplyThemeToResourceDictionary(ResourceDictionary resources)
        {
            if (resources == null) return;

            resources.Clear();

            // Basis-Farben
            resources["QuantumPrimaryColor"] = new SolidColorBrush(QuantumPrimary);
            resources["QuantumSecondaryColor"] = new SolidColorBrush(QuantumSecondary);
            resources["QuantumAccentColor"] = new SolidColorBrush(QuantumAccent);
            resources["QuantumDarkColor"] = new SolidColorBrush(QuantumDark);
            resources["QuantumPanelColor"] = new SolidColorBrush(QuantumPanel);
            resources["QuantumBorderColor"] = new SolidColorBrush(QuantumBorder);
            resources["QuantumTextColor"] = new SolidColorBrush(QuantumText);
            resources["QuantumTextSecondaryColor"] = new SolidColorBrush(QuantumTextSecondary);

            // Gradients
            resources["QuantumGradientBrush"] = QuantumGradientBrush;
            resources["QuantumButtonGradient"] = QuantumButtonGradient;

            // Control-Templates
            CreateControlTemplates(resources);

            // Styles
            CreateStyles(resources);

            // Effects
            CreateEffects(resources);

            // Animations
            CreateAnimations(resources);
        }

        private static void CreateControlTemplates(ResourceDictionary resources)
        {
            // Button Template
            var buttonStyle = new Style(typeof(System.Windows.Controls.Button));
            buttonStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty,
                new StaticResourceExtension("QuantumButtonGradient")));
            buttonStyle.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty,
                new StaticResourceExtension("QuantumTextColor")));
            buttonStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty,
                new StaticResourceExtension("QuantumBorderColor")));
            buttonStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BorderThicknessProperty,
                new Thickness(1)));
            buttonStyle.Setters.Add(new Setter(System.Windows.Controls.Control.CornerRadiusProperty,
                new CornerRadius(8)));
            buttonStyle.Setters.Add(new Setter(System.Windows.Controls.Control.FontWeightProperty,
                FontWeights.SemiBold));
            buttonStyle.Setters.Add(new Setter(System.Windows.Controls.Control.PaddingProperty,
                new Thickness(12, 8, 12, 8)));

            // Triggers
            var triggerHover = new Trigger
            {
                Property = System.Windows.Controls.Button.IsMouseOverProperty,
                Value = true
            };
            triggerHover.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty,
                new Binding("HoverGradient") { Source = QuantumButtonGradient }));
            triggerHover.Setters.Add(new Setter(System.Windows.Controls.Control.EffectProperty,
                CreateGlowEffect(QuantumSecondary)));

            var triggerPressed = new Trigger
            {
                Property = System.Windows.Controls.Button.IsPressedProperty,
                Value = true
            };
            triggerPressed.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty,
                new Binding("PressedGradient") { Source = QuantumButtonGradient }));

            buttonStyle.Triggers.Add(triggerHover);
            buttonStyle.Triggers.Add(triggerPressed);

            resources["QuantumButtonStyle"] = buttonStyle;

            // TextBox Template
            var textBoxStyle = new Style(typeof(System.Windows.Controls.TextBox));
            textBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty,
                new SolidColorBrush(Color.FromArgb(30, 106, 0, 255))));
            textBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty,
                new StaticResourceExtension("QuantumTextColor")));
            textBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty,
                new StaticResourceExtension("QuantumBorderColor")));
            textBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BorderThicknessProperty,
                new Thickness(1)));
            textBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.CornerRadiusProperty,
                new CornerRadius(4)));
            textBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.PaddingProperty,
                new Thickness(8)));

            resources["QuantumTextBoxStyle"] = textBoxStyle;

            // ProgressBar Template
            var progressBarStyle = new Style(typeof(System.Windows.Controls.ProgressBar));
            progressBarStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty,
                new SolidColorBrush(QuantumBorder)));
            progressBarStyle.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty,
                QuantumGradientBrush));
            progressBarStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BorderThicknessProperty,
                new Thickness(0)));
            progressBarStyle.Setters.Add(new Setter(System.Windows.Controls.Control.HeightProperty,
                8.0));

            resources["QuantumProgressBarStyle"] = progressBarStyle;

            // ComboBox Template
            var comboBoxStyle = new Style(typeof(System.Windows.Controls.ComboBox));
            comboBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty,
                new SolidColorBrush(Color.FromArgb(40, 106, 0, 255))));
            comboBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty,
                new StaticResourceExtension("QuantumTextColor")));
            comboBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty,
                new StaticResourceExtension("QuantumBorderColor")));
            comboBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BorderThicknessProperty,
                new Thickness(1)));
            comboBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.CornerRadiusProperty,
                new CornerRadius(4)));

            resources["QuantumComboBoxStyle"] = comboBoxStyle;

            // CheckBox Template
            var checkBoxStyle = new Style(typeof(System.Windows.Controls.CheckBox));
            checkBoxStyle.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty,
                new StaticResourceExtension("QuantumTextColor")));

            resources["QuantumCheckBoxStyle"] = checkBoxStyle;
        }

        private static void CreateStyles(ResourceDictionary resources)
        {
            // Window Style
            var windowStyle = new Style(typeof(Window));
            windowStyle.Setters.Add(new Setter(Window.BackgroundProperty,
                new StaticResourceExtension("QuantumDarkColor")));
            windowStyle.Setters.Add(new Setter(Window.ForegroundProperty,
                new StaticResourceExtension("QuantumTextColor")));
            windowStyle.Setters.Add(new Setter(Window.FontFamilyProperty,
                new FontFamily("Segoe UI")));
            windowStyle.Setters.Add(new Setter(Window.BorderBrushProperty,
                new StaticResourceExtension("QuantumBorderColor")));
            windowStyle.Setters.Add(new Setter(Window.BorderThicknessProperty,
                new Thickness(1)));

            resources["QuantumWindowStyle"] = windowStyle;

            // Label Style
            var labelStyle = new Style(typeof(System.Windows.Controls.Label));
            labelStyle.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty,
                new StaticResourceExtension("QuantumTextSecondaryColor")));
            labelStyle.Setters.Add(new Setter(System.Windows.Controls.Control.FontWeightProperty,
                FontWeights.SemiBold));
            labelStyle.Setters.Add(new Setter(System.Windows.Controls.Control.MarginProperty,
                new Thickness(0, 0, 0, 4)));

            resources["QuantumLabelStyle"] = labelStyle;

            // Card Style (Border)
            var cardStyle = new Style(typeof(System.Windows.Controls.Border));
            cardStyle.Setters.Add(new Setter(System.Windows.Controls.Border.BackgroundProperty,
                new StaticResourceExtension("QuantumPanelColor")));
            cardStyle.Setters.Add(new Setter(System.Windows.Controls.Border.BorderBrushProperty,
                new StaticResourceExtension("QuantumBorderColor")));
            cardStyle.Setters.Add(new Setter(System.Windows.Controls.Border.BorderThicknessProperty,
                new Thickness(1)));
            cardStyle.Setters.Add(new Setter(System.Windows.Controls.Border.CornerRadiusProperty,
                new CornerRadius(10)));
            cardStyle.Setters.Add(new Setter(System.Windows.Controls.Border.PaddingProperty,
                new Thickness(15)));
            cardStyle.Setters.Add(new Setter(System.Windows.Controls.Border.EffectProperty,
                CreateShadowEffect()));

            resources["QuantumCardStyle"] = cardStyle;

            // Title Text Style
            var titleStyle = new Style(typeof(System.Windows.Controls.TextBlock));
            titleStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.ForegroundProperty,
                new StaticResourceExtension("QuantumTextColor")));
            titleStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontSizeProperty,
                18.0));
            titleStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontWeightProperty,
                FontWeights.Bold));
            titleStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.MarginProperty,
                new Thickness(0, 0, 0, 8)));

            resources["QuantumTitleStyle"] = titleStyle;

            // Subtitle Text Style
            var subtitleStyle = new Style(typeof(System.Windows.Controls.TextBlock));
            subtitleStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.ForegroundProperty,
                new StaticResourceExtension("QuantumTextSecondaryColor")));
            subtitleStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontSizeProperty,
                12.0));
            subtitleStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.MarginProperty,
                new Thickness(0, 0, 0, 12)));

            resources["QuantumSubtitleStyle"] = subtitleStyle;

            // Metric Value Style
            var metricValueStyle = new Style(typeof(System.Windows.Controls.TextBlock));
            metricValueStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.ForegroundProperty,
                new StaticResourceExtension("QuantumTextColor")));
            metricValueStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontSizeProperty,
                16.0));
            metricValueStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontWeightProperty,
                FontWeights.Bold));
            metricValueStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.VerticalAlignmentProperty,
                System.Windows.VerticalAlignment.Center));

            resources["QuantumMetricValueStyle"] = metricValueStyle;

            // Metric Label Style
            var metricLabelStyle = new Style(typeof(System.Windows.Controls.TextBlock));
            metricLabelStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.ForegroundProperty,
                new StaticResourceExtension("QuantumTextSecondaryColor")));
            metricLabelStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontSizeProperty,
                11.0));
            metricLabelStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontWeightProperty,
                FontWeights.SemiBold));
            metricLabelStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.VerticalAlignmentProperty,
                System.Windows.VerticalAlignment.Center));

            resources["QuantumMetricLabelStyle"] = metricLabelStyle;

            // Status Indicator Style
            var statusIndicatorStyle = new Style(typeof(Ellipse));
            statusIndicatorStyle.Setters.Add(new Setter(Ellipse.WidthProperty, 8.0));
            statusIndicatorStyle.Setters.Add(new Setter(Ellipse.HeightProperty, 8.0));
            statusIndicatorStyle.Setters.Add(new Setter(Ellipse.MarginProperty,
                new Thickness(0, 0, 6, 0)));

            resources["QuantumStatusIndicatorStyle"] = statusIndicatorStyle;

            // FPS Indicator Style
            var fpsIndicatorStyle = new Style(typeof(Ellipse));
            fpsIndicatorStyle.Setters.Add(new Setter(Ellipse.WidthProperty, 12.0));
            fpsIndicatorStyle.Setters.Add(new Setter(Ellipse.HeightProperty, 12.0));
            fpsIndicatorStyle.Setters.Add(new Setter(Ellipse.MarginProperty,
                new Thickness(0, 0, 8, 0)));

            resources["QuantumFpsIndicatorStyle"] = fpsIndicatorStyle;
        }

        private static void CreateEffects(ResourceDictionary resources)
        {
            // Quantum Glow Effect
            var quantumGlow = new DropShadowEffect
            {
                Color = QuantumSecondary,
                BlurRadius = 20,
                ShadowDepth = 0,
                Opacity = 0.7
            };
            resources["QuantumGlowEffect"] = quantumGlow;

            // Subtle Shadow Effect
            var subtleShadow = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 15,
                ShadowDepth = 0,
                Opacity = 0.3
            };
            resources["QuantumShadowEffect"] = subtleShadow;

            // Green Glow (for good status)
            var greenGlow = new DropShadowEffect
            {
                Color = Colors.LimeGreen,
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.7
            };
            resources["QuantumGreenGlowEffect"] = greenGlow;

            // Red Glow (for bad status)
            var redGlow = new DropShadowEffect
            {
                Color = Colors.Red,
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.7
            };
            resources["QuantumRedGlowEffect"] = redGlow;

            // Yellow Glow (for warning)
            var yellowGlow = new DropShadowEffect
            {
                Color = Colors.Yellow,
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.7
            };
            resources["QuantumYellowGlowEffect"] = yellowGlow;
        }

        private static void CreateAnimations(ResourceDictionary resources)
        {
            // Quantum Pulse Animation
            var quantumPulse = new Storyboard();

            var pulseAnimation = new DoubleAnimationUsingKeyFrames();
            pulseAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0.3, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
            pulseAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0.8, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1))));
            pulseAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0.3, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))));
            pulseAnimation.RepeatBehavior = RepeatBehavior.Forever;

            Storyboard.SetTargetProperty(pulseAnimation, new PropertyPath("Opacity"));
            quantumPulse.Children.Add(pulseAnimation);

            resources["QuantumPulseAnimation"] = quantumPulse;

            // HitReg Spin Animation
            var hitRegSpin = new Storyboard();

            var spinAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(2),
                RepeatBehavior = RepeatBehavior.Forever
            };

            Storyboard.SetTargetProperty(spinAnimation, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
            hitRegSpin.Children.Add(spinAnimation);

            resources["HitRegSpinAnimation"] = hitRegSpin;

            // Fade In Animation
            var fadeIn = new Storyboard();

            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath("Opacity"));
            fadeIn.Children.Add(fadeInAnimation);

            resources["FadeInAnimation"] = fadeIn;

            // Fade Out Animation
            var fadeOut = new Storyboard();

            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath("Opacity"));
            fadeOut.Children.Add(fadeOutAnimation);

            resources["FadeOutAnimation"] = fadeOut;

            // Slide In From Right Animation
            var slideInRight = new Storyboard();

            var slideAnimation = new ThicknessAnimation
            {
                From = new Thickness(100, 0, -100, 0),
                To = new Thickness(0),
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTargetProperty(slideAnimation, new PropertyPath("Margin"));
            slideInRight.Children.Add(slideAnimation);

            resources["SlideInRightAnimation"] = slideInRight;
        }

        /// <summary>
        /// Erstellt einen Glow-Effekt für Buttons
        /// </summary>
        public static DropShadowEffect CreateGlowEffect(Color color)
        {
            return new DropShadowEffect
            {
                Color = color,
                BlurRadius = 15,
                ShadowDepth = 0,
                Opacity = 0.7
            };
        }

        /// <summary>
        /// Erstellt einen Schatten-Effekt für Karten
        /// </summary>
        public static DropShadowEffect CreateShadowEffect()
        {
            return new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 15,
                ShadowDepth = 0,
                Opacity = 0.3
            };
        }

        /// <summary>
        /// Erstellt eine tiefe Kopie des aktuellen ResourceDictionary
        /// </summary>
        public static ResourceDictionary CloneTheme()
        {
            var dict = new ResourceDictionary();
            ApplyThemeToResourceDictionary(dict);
            return dict;
        }

        /// <summary>
        /// Erstellt einen Animationseffekt für Theme-Übergänge
        /// </summary>
        public static void AnimateThemeTransition(FrameworkElement element, string newTheme)
        {
            if (element == null) return;

            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
                BeginTime = TimeSpan.FromMilliseconds(200)
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeOut);
            storyboard.Children.Add(fadeIn);

            Storyboard.SetTarget(fadeOut, element);
            Storyboard.SetTarget(fadeIn, element);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));

            // Theme wechseln nach erster Animation
            fadeOut.Completed += (s, e) =>
            {
                CurrentTheme = newTheme;
                if (element is Window window)
                {
                    ApplyTheme(window);
                }
                else if (element is FrameworkElement fe)
                {
                    ApplyThemeToResourceDictionary(fe.Resources);
                }
            };

            storyboard.Begin(element);
        }

        /// <summary>
        /// Startet Quantum Pulse Animation auf einem Element
        /// </summary>
        public static void StartQuantumPulse(FrameworkElement element)
        {
            if (element == null) return;

            var animation = (Storyboard)Application.Current.Resources["QuantumPulseAnimation"];
            if (animation != null)
            {
                Storyboard.SetTarget(animation, element);
                animation.Begin();
            }
        }

        /// <summary>
        /// Stoppt Quantum Pulse Animation
        /// </summary>
        public static void StopQuantumPulse(FrameworkElement element)
        {
            if (element == null) return;

            var animation = (Storyboard)Application.Current.Resources["QuantumPulseAnimation"];
            if (animation != null)
            {
                animation.Stop();
                element.Opacity = 1.0;
            }
        }

        /// <summary>
        /// Startet HitReg Spin Animation
        /// </summary>
        public static void StartHitRegSpin(FrameworkElement element)
        {
            if (element == null) return;

            element.RenderTransform = new RotateTransform();
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            var animation = (Storyboard)Application.Current.Resources["HitRegSpinAnimation"];
            if (animation != null)
            {
                Storyboard.SetTarget(animation, element);
                animation.Begin();
            }
        }

        /// <summary>
        /// Gibt den passenden Farbwert für einen Performance-Wert zurück
        /// </summary>
        public static Color GetPerformanceColor(float value, float maxValue = 100)
        {
            float percentage = value / maxValue;

            return percentage switch
            {
                < 0.3 => Colors.LimeGreen,      // Gut (0-30%)
                < 0.6 => Colors.GreenYellow,    // Ok (30-60%)
                < 0.8 => Colors.Yellow,         // Warnung (60-80%)
                < 0.9 => Colors.Orange,         // Kritisch (80-90%)
                _ => Colors.Red                 // Gefahr (90-100%)
            };
        }

        /// <summary>
        /// Gibt den passenden Farbwert für Ping zurück
        /// </summary>
        public static Color GetPingColor(int ping)
        {
            return ping switch
            {
                < 20 => Colors.LimeGreen,      // Sehr gut
                < 50 => Colors.GreenYellow,    // Gut
                < 100 => Colors.Yellow,        // Ok
                < 150 => Colors.Orange,        // Schlecht
                _ => Colors.Red                // Sehr schlecht
            };
        }

        /// <summary>
        /// Gibt den passenden Farbwert für FPS zurück
        /// </summary>
        public static Color GetFpsColor(float fps)
        {
            return fps switch
            {
                > 100 => Colors.LimeGreen,     // Sehr gut
                > 60 => Colors.GreenYellow,    // Gut
                > 30 => Colors.Yellow,         // Ok
                > 15 => Colors.Orange,         // Schlecht
                _ => Colors.Red                // Sehr schlecht
            };
        }

        /// <summary>
        /// Erstellt einen Farbverlauf basierend auf Performance
        /// </summary>
        public static LinearGradientBrush CreatePerformanceGradient(float value, float maxValue = 100)
        {
            float percentage = Math.Min(1.0f, value / maxValue);
            Color color = GetPerformanceColor(value, maxValue);

            return new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(color, percentage),
                    new GradientStop(Color.FromRgb(48, 48, 64), percentage)
                }
            };
        }

        /// <summary>
        /// Wende Theme auf alle Fenster der Anwendung an
        /// </summary>
        public static void ApplyThemeToAllWindows()
        {
            foreach (Window window in Application.Current.Windows)
            {
                ApplyTheme(window);
            }
        }

        /// <summary>
        /// Toggle zwischen Quantum und Dark Theme
        /// </summary>
        public static void ToggleTheme()
        {
            CurrentTheme = CurrentTheme == ThemeQuantum ? ThemeDark : ThemeQuantum;
            ApplyThemeToAllWindows();
        }

        /// <summary>
        /// Initialisiert das Theme-System
        /// </summary>
        public static void Initialize()
        {
            // Prüfe Systemeinstellungen für Dark Mode
            try
            {
                // Windows Dark Mode Erkennung
                var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

                if (registryKey != null)
                {
                    object appsUseLightTheme = registryKey.GetValue("AppsUseLightTheme");
                    if (appsUseLightTheme != null && (int)appsUseLightTheme == 0)
                    {
                        // System ist im Dark Mode
                        _currentTheme = ThemeQuantum;
                    }
                }
            }
            catch
            {
                // Default bleibt Quantum
                _currentTheme = ThemeQuantum;
            }
        }
    }

    /// <summary>
    /// Data Class für Button Gradienten
    /// </summary>
    public class LinearGradientButton
    {
        public LinearGradientBrush NormalGradient { get; set; }
        public LinearGradientBrush HoverGradient { get; set; }
        public LinearGradientBrush PressedGradient { get; set; }
    }
}