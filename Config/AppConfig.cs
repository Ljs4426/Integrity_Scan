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
        /// Build-time hash enforcement is disabled in this version.
        /// </summary>
        public const string BuildTimeHash = "DISABLED";
    }
}
