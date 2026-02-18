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
    /// <summary>
    /// Main dashboard showing scan controls and results.
    /// </summary>
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

        /// <summary>
        /// Create dashboard with session manager and UI callbacks.
        /// </summary>
        public DashboardPage(SessionManager sessionManager)
        {
            InitializeComponent();
            _sessionManager = sessionManager;
            _badges = new Dictionary<string, BadgeElements>(StringComparer.OrdinalIgnoreCase)
            {
                { "Files + Modules", new BadgeElements(BadgeFilesModules, BadgeFilesModulesText) },
                { "OS Check", new BadgeElements(BadgeOsCheck, BadgeOsCheckText) },
                { "Memory Integrity", new BadgeElements(BadgeMemoryIntegrity, BadgeMemoryIntegrityText) },
                { "Windows Defender", new BadgeElements(BadgeWindowsDefender, BadgeWindowsDefenderText) },
                { "Exclusions", new BadgeElements(BadgeExclusions, BadgeExclusionsText) },
                { "Threats", new BadgeElements(BadgeThreats, BadgeThreatsText) },
                { "Binary Sig", new BadgeElements(BadgeBinarySig, BadgeBinarySigText) },
                { "Process Explorer", new BadgeElements(BadgeProcessExplorer, BadgeProcessExplorerText) }
            };

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
    }
}
