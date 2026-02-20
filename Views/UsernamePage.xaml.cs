using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Hawkbat.Services;
using Hawkbat.Models;

namespace Hawkbat.Views
{
    public partial class UsernamePage : Page
    {
        private readonly SessionManager _sessionManager;
        private readonly Action _onContinue;

        public UsernamePage(SessionManager sessionManager, Action onContinue)
        {
            InitializeComponent();
            _sessionManager = sessionManager;
            _onContinue = onContinue;
            
            // Apply unit colors
            this.Loaded += (s, e) => ApplyUnitColors();
        }

        private void ApplyUnitColors()
        {
            // Apply selected unit's colors
            if (Models.SessionState.SelectedUnit != null)
            {
                ThemeManager.ApplyUnitTheme(Models.SessionState.SelectedUnit);
            }
        }

        private void OnContinueClicked(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter a username.", "Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Hash and store username
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(username);
            var hash = sha.ComputeHash(bytes);
            var hashedUsername = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            
            // Store in both SessionManager (for backward compatibility) and SessionState
            _sessionManager.SetUsernameHash(hashedUsername);
            SessionState.HashedUsername = hashedUsername;

            _onContinue?.Invoke();
        }
    }
}
