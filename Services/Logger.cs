using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Hawkbat.Services;

namespace Hawkbat.Services
{
    public class Logger
    {
        private readonly SessionManager _session;
        private readonly string _logDirectory;
        private readonly string _logPath;
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public Logger(SessionManager session)
        {
            _session = session;
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "327TH_Hawkbat");
            Directory.CreateDirectory(_logDirectory);
            var start = DateTime.UtcNow;
            _logPath = Path.Combine(_logDirectory, $"session_{start:yyyyMMdd_HHmmss}.logbin");

            // Derive encryption keys and write header
            var (k, iv) = SecurityUtilities.DeriveKeyAndIv(_session.SessionToken);
            _key = k;
            _iv = iv;

            try
            {
                var header = $"{{\"sessionStart\": \"{start:O}\", \"version\": \"{Hawkbat.Config.AppConfig.CurrentVersion}\", \"usernameHash\": \"{_session.UsernameHash ?? string.Empty}\"}}";
                WriteBlockInternal("HEADER", header);
            }
            catch
            {
                // Do not throw from logger creation
            }
        }

        private void WriteBlockInternal(string level, string message)
        {
            var payload = $"{DateTime.UtcNow:O}|{level}|{message}";
            var plain = Encoding.UTF8.GetBytes(payload);
            var cipher = SecurityUtilities.EncryptBlock(plain, _key, _iv);

            // Prepend length header for block splitting
            var len = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(cipher.Length));
            using var fs = new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            fs.Write(len, 0, len.Length);
            fs.Write(cipher, 0, cipher.Length);
        }

        public void Write(string message, string level = "INFO")
        {
            try
            {
                WriteBlockInternal(level, message);
            }
            catch
            {
                // Swallow to avoid UI crash
            }
        }

        public void WriteFinal(string message)
        {
            try
            {
                WriteBlockInternal("FINAL", message);
            }
            catch
            {
            }
        }
    }
}
