using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;
using Hawkbat.Services;

namespace Hawkbat
{
    public partial class App : Application
    {
        public App()
        {
            LogDiagnostic("[App.Constructor] Starting");
            // Set up exception handlers before XAML loads
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                LogDiagnostic($"[AppDomain.UnhandledException] {ex.ExceptionObject}");
                LogCrash(ex.ExceptionObject?.ToString() ?? "Unknown error");
            };

            DispatcherUnhandledException += (s, ex) =>
            {
                LogDiagnostic($"[DispatcherUnhandledException] {ex.Exception}");
                LogCrash(ex.Exception.ToString());
                ex.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                LogDiagnostic($"[TaskScheduler.UnobservedTaskException] {ex.Exception}");
                LogCrash(ex.Exception.ToString());
                ex.SetObserved();
            };
            LogDiagnostic("[App.Constructor] Complete");
        }

        private static void LogCrash(string errorMessage)
        {
            try
            {
                string crashLog = Path.Combine(
                    AppContext.BaseDirectory,
                    "327HB_crash.txt");
                File.WriteAllText(crashLog, $"[{DateTime.Now:O}]\n{errorMessage}");
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                LogDiagnostic("[App.OnStartup] Starting");
                base.OnStartup(e);
                LogDiagnostic("[App.OnStartup] Creating MainWindow");
                var mainWindow = new MainWindow();
                LogDiagnostic("[App.OnStartup] Showing window");
                mainWindow.Show();
                LogDiagnostic("[App.OnStartup] Window shown");
            }
            catch (Exception ex)
            {
                LogDiagnostic($"[App.OnStartup] Exception: {ex}");
                throw;
            }
        }

        private static void LogDiagnostic(string message)
        {
            try
            {
                string logPath = Path.Combine(AppContext.BaseDirectory, "327HB_startup.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:O}] {message}\n");
            }
            catch { }
        }
    }
}
