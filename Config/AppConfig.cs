using System;

namespace Hawkbat.Config
{
    /// <summary>
    /// Centralized application configuration for a local-only application.
    /// This application performs no network communication; all settings are local.
    /// </summary>
    public static class AppConfig
    {
        /// <summary>
        /// Current application version in semantic version format.
        /// Update this value when releasing new versions.
        /// </summary>
        public const string CurrentVersion = "1.0.0";
        /// <summary>
        /// Build-time executable SHA-256 hash.
        /// This value is populated into a generated file during the build process.
        /// If missing, the generated file contains the placeholder value "PENDING".
        /// </summary>
#if !DISABLE_ANTI_TAMPER
        public static string BuildTimeHash => BuildTimeHashHolder.BuildTimeHash;
#else
        public static string BuildTimeHash => "PENDING";
#endif
    }
}
