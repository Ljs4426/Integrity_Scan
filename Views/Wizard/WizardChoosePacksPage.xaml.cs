using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Hawkbat.Models;

namespace Hawkbat.Views.Wizard
{
    public partial class WizardChoosePacksPage : Page
    {
        private List<(string name, string folder, string description, string size)> _availablePacks = new();

        public WizardChoosePacksPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAvailablePacks();
        }

        private async System.Threading.Tasks.Task LoadAvailablePacks()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                string json = await client.GetStringAsync(
                    "https://raw.githubusercontent.com/Ljs4426/327TH_HB_AC/main/available_packs.json");

                using var doc = JsonDocument.Parse(json);
                _availablePacks.Clear();

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    _availablePacks.Add((
                        element.GetProperty("name").GetString() ?? "",
                        element.GetProperty("folder").GetString() ?? "",
                        element.GetProperty("description").GetString() ?? "",
                        element.GetProperty("size").GetString() ?? ""
                    ));
                }

                DisplayPacks();
            }
            catch
            {
                ErrorMessage.Visibility = Visibility.Visible;
                // Default to LOCAL on error
                AddPackRow("LOCAL", "LOCAL", "", "", true, true);
                SkipLinkContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void DisplayPacks()
        {
            PacksPanel.Children.Clear();
            foreach (var (name, folder, desc, size) in _availablePacks)
            {
                AddPackRow(name, folder, desc, size, folder == "LOCAL", false);
            }
        }

        private void AddPackRow(string name, string folder, string desc, string size, bool isChecked, bool isError)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };

            var checkbox = new CheckBox
            {
                IsChecked = isChecked,
                Tag = folder,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 12, 0)
            };

            var infoStack = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Top };

            var nameBlock = new TextBlock
            {
                Text = name,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };

            var descBlock = new TextBlock
            {
                Text = desc + (string.IsNullOrEmpty(size) ? "" : $" • {size}"),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            };

            infoStack.Children.Add(nameBlock);
            if (!string.IsNullOrEmpty(desc) || !string.IsNullOrEmpty(size))
                infoStack.Children.Add(descBlock);

            row.Children.Add(checkbox);
            row.Children.Add(infoStack);

            PacksPanel.Children.Add(row);
        }

        private void OnSkipClicked(object sender, RoutedEventArgs e)
        {
            // Select only LOCAL
            foreach (var child in PacksPanel.Children.OfType<StackPanel>())
            {
                if (child.Children[0] is CheckBox cb)
                {
                    cb.IsChecked = (string)cb.Tag == "LOCAL";
                }
            }
        }

        public List<string> GetSelectedPacks()
        {
            var result = new List<string>();
            foreach (var child in PacksPanel.Children.OfType<StackPanel>())
            {
                if (child.Children[0] is CheckBox cb && cb.IsChecked == true)
                {
                    result.Add((string)cb.Tag);
                }
            }

            // Ensure at least LOCAL is selected
            if (result.Count == 0)
                result.Add("LOCAL");

            return result;
        }
    }
}
