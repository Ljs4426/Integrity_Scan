using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Hawkbat.Models;
using Hawkbat.Services;

namespace Hawkbat.Views.Wizard
{
    public partial class WizardPage : UserControl
    {
        private int _currentStep = 0;
        private List<string> _selectedPacks = new();
        private Action _onWizardComplete;

        public WizardPage(Action onComplete)
        {
            InitializeComponent();
            _onWizardComplete = onComplete;
            ShowStep(0);
        }

        private void ShowStep(int step)
        {
            _currentStep = step;
            ProgressText.Text = $"Step {step + 1} of 3";

            switch (step)
            {
                case 0:
                    ContentFrame.Content = new WizardWelcomePage();
                    BackButton.Visibility = Visibility.Collapsed;
                    NextButton.Visibility = Visibility.Visible;
                    FinishButton.Visibility = Visibility.Collapsed;
                    break;

                case 1:
                    ContentFrame.Content = new WizardChoosePacksPage();
                    BackButton.Visibility = Visibility.Visible;
                    NextButton.Visibility = Visibility.Visible;
                    FinishButton.Visibility = Visibility.Collapsed;
                    break;

                case 2:
                    var downloadPage = new WizardDownloadPage(_selectedPacks);
                    downloadPage.OnDownloadComplete += () =>
                    {
                        NextButton.Visibility = Visibility.Collapsed;
                        FinishButton.Visibility = Visibility.Visible;
                    };
                    ContentFrame.Content = downloadPage;
                    BackButton.Visibility = Visibility.Collapsed;
                    NextButton.Visibility = Visibility.Visible;
                    FinishButton.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 0)
            {
                ShowStep(_currentStep - 1);
            }
        }

        private void OnNextClicked(object sender, RoutedEventArgs e)
        {
            if (_currentStep == 1)
            {
                // Get selected packs
                if (ContentFrame.Content is WizardChoosePacksPage choosePage)
                {
                    _selectedPacks = choosePage.GetSelectedPacks();
                }
            }

            if (_currentStep < 2)
            {
                ShowStep(_currentStep + 1);
            }
        }

        private void OnFinishClicked(object sender, RoutedEventArgs e)
        {
            // Reload units
            SessionState.InstalledUnits = UnitPackLoader.LoadUnitPacks();
            _onWizardComplete?.Invoke();
        }
    }
}
