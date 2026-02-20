using System;
using System.Windows;
using System.Windows.Controls;
using Hawkbat.Models;
using Hawkbat.Services;

namespace Hawkbat.Views
{
    public partial class TermsPage : Page
    {
        private readonly SessionManager _sessionManager;
        private readonly Action _onAccepted;

        public TermsPage(SessionManager sessionManager, Action onAccepted)
        {
            InitializeComponent();
            _sessionManager = sessionManager;
            _onAccepted = onAccepted;
            LoadUnitTermsOfService();
        }

        private void LoadUnitTermsOfService()
        {
            var unit = SessionState.SelectedUnit;
            if (unit != null)
            {
                // Update title with unit info
                Title = $"Terms of Service — {unit.Name}";
                
                // Load ToS text
                var termsTextBlock = TermsScroll.Content as TextBlock;
                if (termsTextBlock != null)
                {
                    termsTextBlock.Text = unit.TosText;
                }
            }
        }

        private void OnTermsScroll(object sender, ScrollChangedEventArgs e)
        {
            if (AcceptButton.IsEnabled)
            {
                return;
            }

            var reachedBottom = e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 1;
            if (reachedBottom)
            {
                AcceptButton.IsEnabled = true;
            }
        }

        private void OnAcceptClicked(object sender, RoutedEventArgs e)
        {
            _sessionManager.SetTermsAccepted(true);
            _onAccepted?.Invoke();
        }

        private void OnDeclineClicked(object sender, RoutedEventArgs e)
        {
            _sessionManager.SetTermsAccepted(false);
            Application.Current.Shutdown();
        }
    }
}
