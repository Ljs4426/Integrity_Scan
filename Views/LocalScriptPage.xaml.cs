using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hawkbat.Models;
using Hawkbat.Services;

namespace Hawkbat.Views
{
    public partial class LocalScriptPage : UserControl
    {
        private Action _onComplete;

        public LocalScriptPage(Action onComplete)
        {
            InitializeComponent();
            _onComplete = onComplete;
        }

        private void OnRunClicked(object sender, RoutedEventArgs e)
        {
            var script = ScriptTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(script))
            {
                MessageBox.Show("Please paste a PowerShell script before running.", "No Script", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Basic PowerShell syntax validation
            var keywords = new[] { "function", "Write-Host", "Get-", "Set-", "$", "param" };
            bool hasKeyword = false;
            foreach (var keyword in keywords)
            {
                if (script.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    hasKeyword = true;
                    break;
                }
            }

            if (!hasKeyword)
            {
                var result = MessageBox.Show(
                    "Script doesn't appear to contain PowerShell syntax. Run anyway?",
                    "Validation Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                    
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            // Save and run script
            SaveScript(script);
            SessionState.LastLocalScriptOption = LocalScriptOption.Pasted;
            
            // Launch PowerShell process
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "Units", "LOCAL", "script.ps1");
            
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = true,  // Interactive window with admin
                    Verb = "runas"
                };

                var process = System.Diagnostics.Process.Start(psi);
                
                if (process != null)
                {
                    // Wait for completion
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        process.WaitForExit();
                        
                        // Continue to dashboard
                        Dispatcher.Invoke(() =>
                        {
                            _onComplete?.Invoke();
                        });
                    });
                }
                else
                {
                    MessageBox.Show("Failed to start PowerShell process.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching script: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveScript(string scriptContent)
        {
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "Units", "LOCAL", "script.ps1");
            var scriptDir = Path.GetDirectoryName(scriptPath) ?? "";
            Directory.CreateDirectory(scriptDir);
            File.WriteAllText(scriptPath, scriptContent);

            if (SessionState.SelectedUnit != null)
            {
                SessionState.SelectedUnit.ScriptPath = scriptPath;
            }
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            // Return to unit selection
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.NavigateToUnitSelection();
        }
    }
}
