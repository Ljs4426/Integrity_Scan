using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hawkbat.Config;
using Hawkbat.Models;
using Hawkbat.Services;

namespace Hawkbat.Views
{
    public partial class UnitSelectionPage : UserControl
    {
        public UnitSelectionPage()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadInstalledUnits();
        }

        private void LoadInstalledUnits()
        {
            UnitsPanel.Children.Clear();
            var units = SessionState.InstalledUnits;

            if (units.Count == 0)
            {
                NoUnitsMessage.Visibility = Visibility.Visible;
                return;
            }

            NoUnitsMessage.Visibility = Visibility.Collapsed;

            foreach (var unit in units)
            {
                var card = CreateUnitCard(unit);
                UnitsPanel.Children.Add(card);
            }
        }

        private Border CreateUnitCard(UnitPack unit)
        {
            var card = new Border
            {
                Width = 200,
                Height = 140,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent),
                BorderThickness = new Thickness(2),
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(unit.AccentColor)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(8),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Logo or placeholder
            var logoBorder = new Border
            {
                Height = 60,
                Width = 60,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            if (unit.HasLogo && !string.IsNullOrEmpty(unit.LogoPath) && File.Exists(unit.LogoPath))
            {
                try
                {
                    var image = new Image { Stretch = Stretch.Uniform };
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(unit.LogoPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    logoBorder.Child = image;
                    image.Source = bitmap;
                }
                catch { }
            }
            else
            {
                logoBorder.Child = new TextBlock
                {
                    Text = "◆",
                    FontSize = 48,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(unit.AccentColor)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            stack.Children.Add(logoBorder);

            // Unit name
            stack.Children.Add(new TextBlock
            {
                Text = unit.Name,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            });

            // Unit subtitle
            stack.Children.Add(new TextBlock
            {
                Text = unit.Subtitle,
                FontSize = 10,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(unit.AccentColor)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            });

            // Unit version
            stack.Children.Add(new TextBlock
            {
                Text = $"v{unit.Version}",
                FontSize = 9,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            card.Child = stack;

            // Click to select
            card.MouseLeftButtonDown += (s, e) =>
            {
                SessionState.SelectedUnit = unit;
                ThemeManager.ApplyUnitTheme(unit);
                
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    // Check if unit requires username
                    if (unit.RequireUsername)
                    {
                        // Show username page
                        mainWindow.NavigateToUsername();
                    }
                    else if (unit.Name == "LOCAL")
                    {
                        // LOCAL unit goes to script configuration
                        mainWindow.NavigateToLocalScript();
                    }
                    else
                    {
                        // Other units go directly to dashboard
                        mainWindow.NavigateToDashboard();
                    }
                }
            };

            // Hover effect
            card.MouseEnter += (s, e) =>
            {
                card.BorderThickness = new Thickness(3);
            };
            card.MouseLeave += (s, e) =>
            {
                card.BorderThickness = new Thickness(2);
            };

            return card;
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0 && files[0].EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    e.Effects = DragDropEffects.Copy;
                    DropZone.BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00FF00"));
                    DropZone.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A2A1A"));
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            e.Handled = true;
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            DropZone.BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078D4"));
            DropZone.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A1A1A"));
        }

        private async void OnDropZone(object sender, DragEventArgs e)
        {
            DropZone.BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078D4"));
            DropZone.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A1A1A"));

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0)
                {
                    await ExtractUnitPack(files[0]);
                }
            }
        }

        private void OnBrowseClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Zip files (*.zip)|*.zip",
                Title = "Select Unit Pack Zip File"
            };

            if (dialog.ShowDialog() == true)
            {
                _ = ExtractUnitPack(dialog.FileName);
            }
        }

        private async System.Threading.Tasks.Task ExtractUnitPack(string zipPath)
        {
            StatusMessage.Visibility = Visibility.Visible;
            StatusMessage.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078D4"));
            StatusMessage.Text = "⟳ Extracting unit pack...";
            BrowseButton.IsEnabled = false;
            DropZone.IsEnabled = false;

            try
            {
                var unitsFolder = Path.Combine(AppContext.BaseDirectory, "Units");
                Directory.CreateDirectory(unitsFolder);

                // Extract zip to temporary folder first
                var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempFolder);

                await System.Threading.Tasks.Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(zipPath, tempFolder);
                });

                // Find the unit folder (should contain unit.json)
                string? unitFolder = null;
                foreach (var dir in Directory.GetDirectories(tempFolder, "*", SearchOption.AllDirectories))
                {
                    if (File.Exists(Path.Combine(dir, "unit.json")))
                    {
                        unitFolder = dir;
                        break;
                    }
                }

                // Check if unit.json is directly in temp folder
                if (unitFolder == null && File.Exists(Path.Combine(tempFolder, "unit.json")))
                {
                    unitFolder = tempFolder;
                }

                if (unitFolder != null)
                {
                    // Read unit.json to get the unit name
                    var unitJsonPath = Path.Combine(unitFolder, "unit.json");
                    var json = File.ReadAllText(unitJsonPath);
                    using var doc = JsonDocument.Parse(json);
                    var unitName = doc.RootElement.GetProperty("name").GetString() ?? Path.GetFileNameWithoutExtension(zipPath);

                    // Create destination folder
                    var destFolder = Path.Combine(unitsFolder, unitName);
                    if (Directory.Exists(destFolder))
                    {
                        Directory.Delete(destFolder, true);
                    }
                    Directory.CreateDirectory(destFolder);

                    // Copy all files
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        foreach (var file in Directory.GetFiles(unitFolder, "*", SearchOption.AllDirectories))
                        {
                            var relativePath = Path.GetRelativePath(unitFolder, file);
                            var destPath = Path.Combine(destFolder, relativePath);
                            var destDir = Path.GetDirectoryName(destPath);
                            if (!string.IsNullOrEmpty(destDir))
                            {
                                Directory.CreateDirectory(destDir);
                            }
                            File.Copy(file, destPath, true);
                        }
                    });

                    StatusMessage.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00FF00"));
                    StatusMessage.Text = $"✓ Successfully installed {unitName}";

                    // Cleanup temp folder
                    try { Directory.Delete(tempFolder, true); } catch { }

                    // Reload units
                    SessionState.InstalledUnits = UnitPackLoader.LoadUnitPacks();
                    LoadInstalledUnits();
                }
                else
                {
                    StatusMessage.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF0000"));
                    StatusMessage.Text = "✗ Invalid unit pack: unit.json not found";
                    try { Directory.Delete(tempFolder, true); } catch { }
                }
            }
            catch (Exception ex)
            {
                StatusMessage.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF0000"));
                StatusMessage.Text = $"✗ Error: {ex.Message}";
            }
            finally
            {
                BrowseButton.IsEnabled = true;
                DropZone.IsEnabled = true;
            }
        }

        private void OnDownloadMoreClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/Ljs4426/327TH_HB_AC/tree/main/Units")
            {
                UseShellExecute = true
            });
        }
    }
}
