using System;
using System.Security.Cryptography;
using System.Text;

namespace Hawkbat.Services
{
    /// <summary>
    /// Manages session information such as session token and username hash.
    /// </summary>
    public class SessionManager
    {
        private string? _usernameHash;
        private readonly string _sessionToken;
        private bool _termsAccepted;

        /// <summary>
        /// Create a new session with a cryptographically random token.
        /// </summary>
        public SessionManager()
        {
            _sessionToken = GenerateSessionToken();
        }

        /// <summary>
        /// Read-only session token.
        /// </summary>
        public string SessionToken => _sessionToken;

        /// <summary>
        /// Set hashed username (hex lowercase). Plaintext is never stored.
        /// </summary>
        public void SetUsernameHash(string hash)
        {
            _usernameHash = hash;
        }

        /// <summary>
        /// Get stored username hash, or null.
        /// </summary>
        public string? UsernameHash => _usernameHash;

        /// <summary>
        /// Mark terms accepted.
        /// </summary>
        public void SetTermsAccepted(bool accepted)
        {
            _termsAccepted = accepted;
        }

        /// <summary>
        /// Whether terms have been accepted.
        /// </summary>
        public bool TermsAccepted => _termsAccepted;

        private static string GenerateSessionToken()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
