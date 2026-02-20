using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hawkbat.Views;
using Hawkbat.Services;
using Hawkbat.Models;

namespace Hawkbat
{
    public partial class MainWindow : Window
    {
        private readonly SessionManager _sessionManager;

        public MainWindow()
        {
            try
            {
                LogDiagnostic("MainWindow constructor started");
                InitializeComponent();
                LogDiagnostic("InitializeComponent completed");

                _sessionManager = new SessionManager();
                LogDiagnostic("SessionManager created");

                // Show loading screen
                LogDiagnostic("Navigating to loading");
                NavigateToLoading();
                LogDiagnostic("MainWindow constructor completed successfully");
            }
            catch (Exception ex)
            {
                LogDiagnostic($"EXCEPTION: {ex}");
                string crashLog = Path.Combine(
                    AppContext.BaseDirectory,
                    "327HB_crash.txt");
                try
                {
                    File.WriteAllText(crashLog, ex.ToString());
                }
                catch { }
                throw;
            }
        }

        private void LogDiagnostic(string message)
        {
            try
            {
                string logPath = Path.Combine(AppContext.BaseDirectory, "327HB_startup.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:O}] {message}\n");
            }
            catch { }
        }

        private void NavigateToLoading()
        {
            LogDiagnostic("NavigateToLoading called");
            MainFrame.Dispatcher.Invoke(() =>
            {
                LogDiagnostic("MainFrame dispatcher invoked");
                MainFrame.Navigate(new LoadingPage());
                LogDiagnostic("LoadingPage navigated");
                _ = DoBackgroundInitializationAsync();
            });
        }

        private async System.Threading.Tasks.Task DoBackgroundInitializationAsync()
        {
            try
            {
                LogDiagnostic("DoBackgroundInitializationAsync started");
                
                // Scan Units folder
                LogDiagnostic("About to load unit packs");
                SessionState.InstalledUnits = UnitPackLoader.LoadUnitPacks();
                LogDiagnostic($"Loaded {SessionState.InstalledUnits?.Count ?? 0} unit packs");
            }
            catch (Exception ex)
            {
                LogDiagnostic($"DoBackgroundInitializationAsync exception: {ex}");
            }

            // Navigate to terms (shown in blue)
            LogDiagnostic("About to navigate to terms page");
            await System.Threading.Tasks.Task.CompletedTask;
            MainFrame.Dispatcher.Invoke(() =>
            {
                LogDiagnostic("Resetting theme to blue for Terms page");
                ApplyDefaultBlueTheme();
                LogDiagnostic("Terms page dispatcher invoked");
                MainFrame.Navigate(new TermsPage(_sessionManager, NavigateToUnitSelection));
                LogDiagnostic("TermsPage navigated");
            });
        }

        private void ApplyDefaultBlueTheme()
        {
            var blueColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078D4");
            var blueBrush = new System.Windows.Media.SolidColorBrush(blueColor);
            Application.Current.Resources["AccentColor"] = blueBrush;
            Application.Current.Resources["AccentBrush"] = blueBrush;
        }

        private void NavigateToTerms()
        {
            MainFrame.Navigate(new TermsPage(_sessionManager, NavigateToUnitSelection));
        }

        public void NavigateToUnitSelection()
        {
            MainFrame.Navigate(new UnitSelectionPage());
        }

        public void NavigateToDashboard()
        {
            MainFrame.Navigate(new DashboardPage(_sessionManager));
        }

        public void NavigateToUsername()
        {
            MainFrame.Navigate(new UsernamePage(_sessionManager, NavigateToDashboard));
        }

        public void NavigateToLocalScript()
        {
            MainFrame.Navigate(new LocalScriptPage(() => NavigateToDashboard()));
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object? sender, RoutedEventArgs? e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object? sender, RoutedEventArgs? e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs? e)
        {
            Close();
        }
    }
}
