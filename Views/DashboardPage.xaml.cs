using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Hawkbat.Services;

namespace Hawkbat.Views
{
    public partial class DashboardPage : Page
    {
        private readonly SessionManager _sessionManager;
        private readonly ScanEngine _scanEngine;
        private readonly Dictionary<string, BadgeElements> _badges;
        private readonly Dictionary<string, Brush> _statusBrushes;
        private CancellationTokenSource? _scanCts;
        private bool _isScanning;

        private sealed class BadgeElements
        {
            public BadgeElements(Border container, TextBlock text)
            {
                Container = container;
                Text = text;
            }

            public Border Container { get; }
            public TextBlock Text { get; }
        }

        public DashboardPage(SessionManager sessionManager)
        {
            InitializeComponent();
            _sessionManager = sessionManager;
            
            // Set unit display info
            if (Models.SessionState.SelectedUnit != null)
            {
                UnitNameText.Text = Models.SessionState.SelectedUnit.Name;
                UnitSubtitleText.Text = Models.SessionState.SelectedUnit.Subtitle;
                
                // Apply unit accent color
                if (!string.IsNullOrEmpty(Models.SessionState.SelectedUnit.AccentColor))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(Models.SessionState.SelectedUnit.AccentColor);
                        var accentBrush = new SolidColorBrush(color);
                        
                        // Update accent resources
                        this.Resources["AccentBrush"] = accentBrush;
                        this.Resources["PanelAccentBrush"] = accentBrush;
                        
                        // Also directly set border brushes and progress bar
                        ScanOutputBorder.BorderBrush = accentBrush;
                        RightPanelBorder.BorderBrush = accentBrush;
                        ScanProgress.Foreground = accentBrush;
                        StartScanButton.Background = accentBrush;
                    }
                    catch
                    {
                        // Fallback to default
                    }
                }
                
                // Hide logo for units without one
                if (!Models.SessionState.SelectedUnit.HasLogo)
                {
                    UnitLogo.Visibility = Visibility.Collapsed;
                }
                else
                {
                    UnitLogo.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(Models.SessionState.SelectedUnit.LogoPath, UriKind.RelativeOrAbsolute));
                }
            }
            
            _badges = new Dictionary<string, BadgeElements>(StringComparer.OrdinalIgnoreCase);
            
            // Dynamically generate scan result badges from unit configuration
            if (Models.SessionState.SelectedUnit != null && Models.SessionState.SelectedUnit.ScanResults != null)
            {
                foreach (var scanResultName in Models.SessionState.SelectedUnit.ScanResults)
                {
                    var grid = new Grid { Margin = new Thickness(0, ScanResultsPanel.Children.Count == 0 ? 2 : 6, 0, 0) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var label = new TextBlock
                    {
                        Text = scanResultName,
                        Foreground = (Brush)FindResource("TextBrush"),
                        FontSize = 11
                    };
                    Grid.SetColumn(label, 0);
                    grid.Children.Add(label);

                    var badge = new Border
                    {
                        Background = (Brush)FindResource("PendingBrush"),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(8, 2, 8, 2),
                        MinWidth = 70
                    };
                    Grid.SetColumn(badge, 1);

                    var badgeText = new TextBlock
                    {
                        Text = "PENDING",
                        Foreground = (Brush)FindResource("TextBrush"),
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    badge.Child = badgeText;
                    grid.Children.Add(badge);
                    ScanResultsPanel.Children.Add(grid);

                    _badges[scanResultName] = new BadgeElements(badge, badgeText);
                }
            }

            _statusBrushes = new Dictionary<string, Brush>(StringComparer.OrdinalIgnoreCase)
            {
                { "PENDING", (Brush)FindResource("PendingBrush") },
                { "PASS", (Brush)FindResource("SuccessBrush") },
                { "FAIL", (Brush)FindResource("FailureBrush") },
                { "WARNING", (Brush)FindResource("WarningBrush") }
            };

            ResetBadges();
            _scanEngine = new ScanEngine(_sessionManager, LogMessage, UpdateResult, UpdateProgress);
        }

        private void LogMessage(string category, string message)
        {
            var line = string.IsNullOrWhiteSpace(message)
                ? category
                : string.IsNullOrWhiteSpace(category) ? message : $"{category}: {message}";

            if (ShouldSuppressLine(line))
            {
                return;
            }

            var brush = GetLogBrush(line);
            var paragraph = new Paragraph { Margin = new Thickness(0) };
            paragraph.Inlines.Add(new Run($"[{DateTime.UtcNow:O}] {line}") { Foreground = brush });
            ScanLog.Document.Blocks.Add(paragraph);
            ScanLog.ScrollToEnd();
        }

        private void UpdateResult(string key, string status)
        {
            SetBadge(key, status);
        }

        private void UpdateProgress(double value)
        {
            ScanProgress.IsIndeterminate = false;
            var clamped = Math.Max(0, Math.Min(100, value));
            var animation = new DoubleAnimation(clamped, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            ScanProgress.BeginAnimation(ProgressBar.ValueProperty, animation);
        }

        private async void OnStartScan(object sender, RoutedEventArgs e)
        {
            if (_isScanning)
            {
                _scanCts?.Cancel();
                return;
            }

            _isScanning = true;
            _scanCts = new CancellationTokenSource();
            StartScanButton.Content = "Stop Scan";
            StartScanButton.Background = (Brush)FindResource("StopBrush");
            ScanLog.Document.Blocks.Clear();
            ResetBadges();
            ScanProgress.IsIndeterminate = false;
            ScanProgress.Value = 0;
            try
            {
                await _scanEngine.RunFullScanAsync(_scanCts.Token);
            }
            catch (OperationCanceledException)
            {
                ResetBadges();
                UpdateProgress(0);
            }
            catch (Exception ex)
            {
                LogMessage("Recording Policy", $"Scan failed: {ex.Message}");
                SetBadge("Process Explorer", "FAIL");
            }
            finally
            {
                _isScanning = false;
                _scanCts?.Dispose();
                _scanCts = null;
                StartScanButton.Content = "Start Scan";
                // Restore accent color
                StartScanButton.Background = (Brush)FindResource("AccentBrush");
                StartScanButton.Background = (Brush)FindResource("AccentBrush");
            }
        }

        private Brush GetLogBrush(string line)
        {
            if (line.Contains("Recording Policy Error: The operation completed successfully", StringComparison.OrdinalIgnoreCase))
            {
                return (Brush)FindResource("MutedTextBrush");
            }

            if (line.Contains("FAILURE", StringComparison.OrdinalIgnoreCase))
            {
                return (Brush)FindResource("FailureBrush");
            }

            if (line.Contains("WARNING", StringComparison.OrdinalIgnoreCase))
            {
                return (Brush)FindResource("WarningBrush");
            }

            if (line.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                return (Brush)FindResource("SuccessBrush");
            }

            return (Brush)FindResource("TextBrush");
        }

        private static bool ShouldSuppressLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return true;
            }

            var trimmed = line.Trim();
            return trimmed.StartsWith("Progress:", StringComparison.OrdinalIgnoreCase);
        }

        private void ResetBadges()
        {
            foreach (var entry in _badges)
            {
                ApplyBadge(entry.Value, "PENDING");
            }
        }

        private void SetBadge(string key, string status)
        {
            if (!_badges.TryGetValue(key, out var badge))
            {
                return;
            }

            var normalized = NormalizeStatus(status);
            ApplyBadge(badge, normalized);
        }

        private void ApplyBadge(BadgeElements badge, string status)
        {
            badge.Text.Text = status;
            badge.Container.Background = _statusBrushes.TryGetValue(status, out var brush)
                ? brush
                : (Brush)FindResource("PendingBrush");
        }

        private static string NormalizeStatus(string status)
        {
            var normalized = status?.Trim().ToUpperInvariant() ?? "PENDING";
            return normalized switch
            {
                "SUCCESS" => "PASS",
                "PASS" => "PASS",
                "FAILURE" => "FAIL",
                "FAILED" => "FAIL",
                "FAIL" => "FAIL",
                "WARNING" => "WARNING",
                "PENDING" => "PENDING",
                _ => normalized
            };
        }
        private void OnAddUnitPacks(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.NavigateToUnitSelection();
            }
        }
    }
}
