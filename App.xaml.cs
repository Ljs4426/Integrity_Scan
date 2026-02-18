using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace Hawkbat
{
    /// <summary>
    /// Application entry class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initialize global exception handlers on application startup.
        /// Logs all unhandled exceptions to crash log on desktop.
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                string crashLog = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "327HB_crash.txt");
                File.WriteAllText(crashLog, ex.ExceptionObject.ToString());
            };

            DispatcherUnhandledException += (s, ex) =>
            {
                string crashLog = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "327HB_crash.txt");
                File.WriteAllText(crashLog, ex.Exception.ToString());
                ex.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                string crashLog = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "327HB_crash.txt");
                File.WriteAllText(crashLog, ex.Exception.ToString());
                ex.SetObserved();
            };

            base.OnStartup(e);
        }
    }
}
