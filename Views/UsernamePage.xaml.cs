using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Hawkbat.Services;

namespace Hawkbat.Views
{
    /// <summary>
    /// Page where user enters their username. The plaintext username is never stored.
    /// </summary>
    public partial class UsernamePage : Page
    {
        private readonly SessionManager _sessionManager;
        private readonly Action _onContinue;

        /// <summary>
        /// Create the page with session manager and continuation action.
        /// </summary>
        public UsernamePage(SessionManager sessionManager, Action onContinue)
        {
            InitializeComponent();
            _sessionManager = sessionManager;
            _onContinue = onContinue;
        }

        private void OnContinueClicked(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter a username.", "Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Hash username with SHA-256 and store only the hash.
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(username);
            var hash = sha.ComputeHash(bytes);
            _sessionManager.SetUsernameHash(BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant());

            _onContinue?.Invoke();
        }
    }
}
