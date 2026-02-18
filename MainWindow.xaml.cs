using System;
using System.IO;
using System.Windows;
using Hawkbat.Views;
using Hawkbat.Services;

namespace Hawkbat
{
    /// <summary>
    /// Main window and navigation host.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SessionManager _sessionManager;

        /// <summary>
        /// Construct main window and initialize services.
        /// </summary>
        public MainWindow()
        {
            try
            {
                InitializeComponent();

                _sessionManager = new SessionManager();

                // Navigate to username entry on startup.
                MainFrame.Navigate(new UsernamePage(_sessionManager, NavigateToTerms));
            }
            catch (Exception ex)
            {
                string crashLog = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "327HB_crash.txt");
                File.WriteAllText(crashLog, ex.ToString());
                throw;
            }
        }

        private void NavigateToTerms()
        {
            MainFrame.Navigate(new TermsPage(_sessionManager, NavigateToDashboard));
        }

        private void NavigateToDashboard()
        {
            // Local-only startup: run anti-tamper checks are handled inside the scan engine.
            MainFrame.Navigate(new DashboardPage(_sessionManager));
        }
    }
}
