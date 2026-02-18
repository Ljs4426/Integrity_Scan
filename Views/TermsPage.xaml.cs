using System;
using System.Windows;
using System.Windows.Controls;
using Hawkbat.Services;

namespace Hawkbat.Views
{
    /// <summary>
    /// Terms of Service acceptance page. User must accept to continue.
    /// </summary>
    public partial class TermsPage : Page
    {
        private readonly SessionManager _sessionManager;
        private readonly Action _onAccepted;

        /// <summary>
        /// Create the page with session manager and continuation action.
        /// </summary>
        public TermsPage(SessionManager sessionManager, Action onAccepted)
        {
            InitializeComponent();
            _sessionManager = sessionManager;
            _onAccepted = onAccepted;
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
