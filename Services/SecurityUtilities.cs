using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Hawkbat.Services
{
    public static class SecurityUtilities
    {
        private const int KeySize = 32;
        private const int IvSize = 16;

        public static (byte[] Key, byte[] IV) DeriveKeyAndIv(string sessionToken)
        {
            var machineGuid = GetMachineGuid();
            using var kdf = new Rfc2898DeriveBytes(sessionToken + machineGuid, Encoding.UTF8.GetBytes("hawkbat_salt"), 100_000, HashAlgorithmName.SHA256);
            var key = kdf.GetBytes(KeySize);
            var iv = kdf.GetBytes(IvSize);
            return (key, iv);
        }

        public static byte[] EncryptBlock(byte[] plaintext, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;

            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(plaintext, 0, plaintext.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        public static string GetMachineGuid()
        {
            try
            {
                using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Microsoft\\Cryptography");
                var v = key?.GetValue("MachineGuid") as string;
                return v ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string ComputeFileHash(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            var hash = sha.ComputeHash(fs);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }

        public static bool IsNativeDebuggerPresent()
        {
            return NativeMethods.IsDebuggerPresent();
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("kernel32.dll")]
            public static extern bool IsDebuggerPresent();
        }
    }
}
