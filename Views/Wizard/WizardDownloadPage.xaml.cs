using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Hawkbat.Services;

namespace Hawkbat.Views.Wizard
{
    public partial class WizardDownloadPage : Page
    {
        private List<string> _selectedPacks;
        private int _totalFiles;
        private int _completedFiles;

        public event Action OnDownloadComplete;

        public WizardDownloadPage(List<string> selectedPacks)
        {
            InitializeComponent();
            _selectedPacks = selectedPacks ?? new List<string>();
            _totalFiles = _selectedPacks.Count * 3; // unit.json, script.ps1, tos.txt
            if (_selectedPacks.Contains("327th_Hawkbat") || _selectedPacks.Contains("327TH_Hawkbat"))
                _totalFiles++; // logo.png
            _completedFiles = 0;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await StartDownload();
        }

        private async System.Threading.Tasks.Task StartDownload()
        {
            var unitsFolder = Path.Combine(AppContext.BaseDirectory, "Units");
            Directory.CreateDirectory(unitsFolder);

            foreach (var pack in _selectedPacks)
            {
                var files = GetFilesForPack(pack);
                foreach (var file in files)
                {
                    await DownloadFile(pack, file, unitsFolder);
                }
            }

            CompletionText.Visibility = Visibility.Visible;
            OnDownloadComplete?.Invoke();
        }

        private string[] GetFilesForPack(string pack)
        {
            if (pack == "327th_Hawkbat" || pack == "327TH_Hawkbat")
                return new[] { "unit.json", "script.ps1", "tos.txt", "logo.png" };
            return new[] { "unit.json", "script.ps1", "tos.txt" };
        }

        private async System.Threading.Tasks.Task DownloadFile(string pack, string file, string outputFolder)
        {
            string url = $"https://raw.githubusercontent.com/Ljs4426/327TH_HB_AC/main/Units/{pack}/{file}";
            string outputPath = Path.Combine(outputFolder, pack, file);

            bool success = await PackDownloader.DownloadFileAsync(url, outputPath);

            string status = success ? "[OK]" : "[FAILED]";
            string color = success ? "#00DD00" : "#FF6B6B";

            AppendLog($"{status} {file}", color);

            if (!success && file != "logo.png")
            {
                // Retry for non-optional files
                await System.Threading.Tasks.Task.Delay(500);
                success = await PackDownloader.DownloadFileAsync(url, outputPath);
                if (success)
                {
                    AppendLog($"[OK] {file} (retry)", "#00DD00");
                }
            }

            _completedFiles++;
            ProgressBar.Value = (_completedFiles / (double)_totalFiles) * 100;
        }

        private void AppendLog(string message, string color)
        {
            this.Dispatcher.Invoke(() =>
            {
                var run = new System.Windows.Documents.Run(message + "\n")
                {
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color))
                };
                LogText.Inlines.Add(run);
            });
        }
    }
}
