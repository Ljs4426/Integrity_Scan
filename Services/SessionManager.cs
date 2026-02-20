using System;
using System.Security.Cryptography;
using System.Text;

namespace Hawkbat.Services
{
    public class SessionManager
    {
        private string? _usernameHash;
        private readonly string _sessionToken;
        private bool _termsAccepted;

        public SessionManager()
        {
            _sessionToken = GenerateSessionToken();
        }

        public string SessionToken => _sessionToken;

        public void SetUsernameHash(string hash)
        {
            _usernameHash = hash;
        }

        public string? UsernameHash => _usernameHash;

        public void SetTermsAccepted(bool accepted)
        {
            _termsAccepted = accepted;
        }

        public bool TermsAccepted => _termsAccepted;

        private static string GenerateSessionToken()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
