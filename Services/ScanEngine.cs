using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace Hawkbat.Services
{
    /// <summary>
    /// Orchestrates recording policy checks and reports results through provided callbacks.
    /// </summary>
    public class ScanEngine
    {
        private readonly SessionManager _session;
        private readonly Action<string, string> _log;
        private readonly Action<string, string> _resultCallback;
        private readonly Action<double> _progressCallback;

        private static readonly string[] SuspiciousProcesses = new[]
        {
            "cheatengine", "processhacker", "x64dbg", "ollydbg", "windbg", "wireshark", "fiddler", "charles", "artmoney"
        };

        private static readonly string[] SectionOrder = new[]
        {
            "Files + Modules",
            "OS Check",
            "Memory Integrity",
            "Windows Defender",
            "Exclusions",
            "Threats",
            "Binary Sig",
            "Process Explorer"
        };

        private const double SectionProgressIncrement = 12.5;


        /// <summary>
        /// Create scan engine with session manager and callbacks for logging and results.
        /// </summary>
        public ScanEngine(SessionManager session, Action<string, string> log, Action<string, string> resultCallback, Action<double> progressCallback)
        {
            _session = session;
            _log = log;
            _resultCallback = resultCallback;
            _progressCallback = progressCallback;
        }

        /// <summary>
        /// Run the full suite of scans asynchronously.
        /// </summary>
        public async Task RunFullScanAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => RunRecordingPolicyScript(cancellationToken), cancellationToken);
        }

        private void RunAntiTamperChecks()
        {
            try
            {
                if (Debugger.IsAttached || SecurityUtilities.IsNativeDebuggerPresent())
                {
                    LogOnUi("AntiTamper", "Debugger detected. Exiting.");
                    Environment.Exit(1);
                }

                // Hash enforcement is disabled in this build.
                if (string.Equals(Hawkbat.Config.AppConfig.BuildTimeHash, "DISABLED", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                LogOnUi("AntiTamper", $"Anti-tamper error: {ex.Message}");
            }
        }

        private void CheckWindowsDefender()
        {
            try
            {
                LogOnUi("Defender", "Checking Windows Defender status.");
                var svc = new ServiceController("WinDefend");
                var running = svc.Status == ServiceControllerStatus.Running;
                if (!running)
                {
                    ResultOnUi("Windows Defender", "FAIL");
                    LogOnUi("Defender", "Windows Defender service is not running.");
                    return;
                }

                // Check real-time protection via registry
                using var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows Defender\\Real-Time Protection");
                var realtime = key?.GetValue("DisableRealtimeMonitoring") as int?;
                if (realtime == 1)
                {
                    ResultOnUi("Windows Defender RealTime", "FAIL");
                    LogOnUi("Defender", "Real-time protection is disabled.");
                }
                else
                {
                    ResultOnUi("Windows Defender", "PASS");
                    LogOnUi("Defender", "Defender running and real-time enabled.");
                }
            }
            catch (Exception ex)
            {
                LogOnUi("Defender", $"Error checking Defender: {ex.Message}");
                ResultOnUi("Windows Defender", "WARNING");
            }
        }

        private void CheckSystemFiles()
        {
            try
            {
                LogOnUi("SysFiles", "Checking PowerShell signature and modules.");
                var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
                var pwsh = Path.Combine(systemFolder, "WindowsPowerShell\\v1.0\\powershell.exe");
                if (File.Exists(pwsh))
                {
                    // Compute signature verification placeholder.
                    var hash = SecurityUtilities.ComputeFileHash(pwsh);
                    LogOnUi("SysFiles", $"PowerShell hash: {hash}");
                    ResultOnUi("PowerShell Signature", "PASS");
                }
                else
                {
                    ResultOnUi("PowerShell Signature", "WARNING");
                    LogOnUi("SysFiles", "PowerShell not found at expected path.");
                }

                // Check modules folder for unknown modules (simple heuristic)
                var modulesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WindowsPowerShell\\Modules");
                if (Directory.Exists(modulesDir))
                {
                    var modules = Directory.GetDirectories(modulesDir).Select(Path.GetFileName).ToList();
                    LogOnUi("SysFiles", $"PowerShell modules: {string.Join(", ", modules)}");
                }
            }
            catch (Exception ex)
            {
                LogOnUi("SysFiles", $"System files check failed: {ex.Message}");
            }
        }

        private void CheckMemoryIntegrity()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity");
                var val = key?.GetValue("Enabled") as int?;
                var enabled = val == 1;
                ResultOnUi("Memory Integrity", enabled ? "PASS" : "WARNING");
                LogOnUi("Memory", $"Memory integrity enabled: {enabled}");
            }
            catch (Exception ex)
            {
                LogOnUi("Memory", $"Memory integrity check error: {ex.Message}");
            }
        }

        private void LogHardwareInformation()
        {
            try
            {
                LogOnUi("Hardware", "Collecting hardware information.");
                var cpu = GetWmiString("Win32_Processor", "Name");
                var cores = GetWmiString("Win32_ComputerSystem", "NumberOfLogicalProcessors");
                var totalRam = GetWmiString("Win32_ComputerSystem", "TotalPhysicalMemory");
                var gpu = GetWmiString("Win32_VideoController", "Name");
                var os = GetWmiString("Win32_OperatingSystem", "Caption");

                LogOnUi("Hardware", $"CPU: {cpu}");
                LogOnUi("Hardware", $"Cores: {cores}");
                LogOnUi("Hardware", $"RAM: {totalRam}");
                LogOnUi("Hardware", $"GPU: {gpu}");
                LogOnUi("Hardware", $"OS: {os}");
                ResultOnUi("Hardware Info", "PASS");
            }
            catch (Exception ex)
            {
                LogOnUi("Hardware", $"Hardware info error: {ex.Message}");
            }
        }

        private void DetectSuspiciousTools()
        {
            try
            {
                LogOnUi("Detect", "Scanning running processes for suspicious tools.");
                var procs = Process.GetProcesses();
                foreach (var p in procs)
                {
                    try
                    {
                        var name = p.ProcessName.ToLowerInvariant();
                        if (SuspiciousProcesses.Any(sp => name.Contains(sp)))
                        {
                            ResultOnUi(name, "FAIL");
                            LogOnUi("Detect", $"Suspicious process found: {p.ProcessName} (PID {p.Id})");
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                LogOnUi("Detect", $"Suspicious tool detection failed: {ex.Message}");
            }
        }

        private void DetectVirtualization()
        {
            try
            {
                LogOnUi("VM", "Checking virtualization indicators.");
                var manufacturer = GetWmiString("Win32_ComputerSystem", "Manufacturer");
                var model = GetWmiString("Win32_ComputerSystem", "Model");
                var vmIndicators = new List<string> { manufacturer ?? string.Empty, model ?? string.Empty };
                var isVm = vmIndicators.Any(v => v.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0 || v.IndexOf("vmware", StringComparison.OrdinalIgnoreCase) >= 0 || v.IndexOf("virtualbox", StringComparison.OrdinalIgnoreCase) >= 0);
                ResultOnUi("Virtualization", isVm ? "WARNING" : "PASS");
                LogOnUi("VM", $"VM indicators: {string.Join(",", vmIndicators)}");
            }
            catch (Exception ex)
            {
                LogOnUi("VM", $"Virtualization check error: {ex.Message}");
            }
        }

        private void RunRecordingPolicyScript(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tempPath = ExtractEmbeddedScriptToTemp();
            if (string.IsNullOrWhiteSpace(tempPath))
            {
                LogOnUi("Recording Policy", "Failed to load embedded script resource.");
                ResultOnUi("Recording Policy Script", "FAILURE");
                return;
            }

            try
            {
                RunPowerShellFile(tempPath, cancellationToken);
            }
            finally
            {
                TryDeleteTempFile(tempPath);
            }
        }

        private void RunPowerShellFile(string scriptPath, CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            RunPowerShellProcess(psi, cancellationToken);
        }

        private void RunPowerShellProcess(ProcessStartInfo psi, CancellationToken cancellationToken)
        {
            var currentSection = string.Empty;
            var stateLock = new object();
            var completedSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sectionStatuses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var processExplorerStarted = false;
            var wasCancelled = false;

            ProgressOnUi(0);

            using var process = new Process { StartInfo = psi };
            using var registration = cancellationToken.Register(() =>
            {
                wasCancelled = true;
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                    }
                }
                catch { }

                LogOnUi(string.Empty, "[STOPPED] Scan cancelled by user.");
            });
            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data == null) return;
                HandleScriptOutputLine(args.Data, stateLock, completedSections, sectionStatuses, ref currentSection, ref processExplorerStarted);
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data == null) return;
                LogOnUi("Recording Policy Error", args.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (wasCancelled)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            CompleteSectionIfNeeded(stateLock, completedSections, currentSection);

            if (processExplorerStarted)
            {
                ResultOnUi("Process Explorer", process.ExitCode == 0 ? "PASS" : "FAIL");
                CompleteSection(stateLock, completedSections, "Process Explorer");
            }

            if (process.ExitCode != 0)
            {
                LogOnUi("Recording Policy", $"Script exited with code {process.ExitCode}.");
            }
        }

        private void HandleScriptOutputLine(
            string line,
            object stateLock,
            HashSet<string> completedSections,
            Dictionary<string, string> sectionStatuses,
            ref string currentSection,
            ref bool processExplorerStarted)
        {
            var trimmed = line.Trim();
            if (TryUpdateSection(trimmed, stateLock, completedSections, ref currentSection, ref processExplorerStarted))
            {
                LogOnUi(string.Empty, line);
                return;
            }

            LogOnUi(string.Empty, line);

            var activeSection = GetSectionSnapshot(stateLock, currentSection);
            if (!IsKnownSection(activeSection))
            {
                return;
            }

            if (TryParseStatusLine(trimmed, out var status))
            {
                UpdateSectionStatus(stateLock, sectionStatuses, activeSection, status);
            }
        }

        private bool TryUpdateSection(
            string line,
            object stateLock,
            HashSet<string> completedSections,
            ref string currentSection,
            ref bool processExplorerStarted)
        {
            if (line.Contains("Step 2 of 2", StringComparison.OrdinalIgnoreCase))
            {
                var previous = GetSectionSnapshot(stateLock, currentSection);
                CompleteSectionIfNeeded(stateLock, completedSections, previous);

                lock (stateLock)
                {
                    currentSection = "Process Explorer";
                }

                processExplorerStarted = true;
                ResultOnUi("Process Explorer", "PENDING");
                return true;
            }

            if (!line.StartsWith("--- ", StringComparison.Ordinal) || !line.EndsWith(" ---", StringComparison.Ordinal))
            {
                return false;
            }

            var section = line.Substring(4, line.Length - 8).Trim();
            if (string.IsNullOrWhiteSpace(section))
            {
                return false;
            }

            var previousSection = GetSectionSnapshot(stateLock, currentSection);
            CompleteSectionIfNeeded(stateLock, completedSections, previousSection);

            lock (stateLock)
            {
                currentSection = section;
            }

            return true;
        }

        private static string GetSectionSnapshot(object stateLock, string currentSection)
        {
            lock (stateLock)
            {
                return currentSection;
            }
        }

        private static bool TryParseStatusLine(string line, out string status)
        {
            status = string.Empty;

            if (line.StartsWith("SUCCESS:", StringComparison.OrdinalIgnoreCase))
            {
                status = "SUCCESS";
            }
            else if (line.StartsWith("FAILURE:", StringComparison.OrdinalIgnoreCase))
            {
                status = "FAILURE";
            }
            else if (line.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase))
            {
                status = "WARNING";
            }
            else
            {
                return false;
            }
            return true;
        }

        private void UpdateSectionStatus(
            object stateLock,
            Dictionary<string, string> sectionStatuses,
            string section,
            string status)
        {
            if (!IsKnownSection(section))
            {
                return;
            }

            lock (stateLock)
            {
                if (!sectionStatuses.TryGetValue(section, out var existing))
                {
                    existing = string.Empty;
                }

                var resolved = ResolveStatus(existing, status);
                if (!string.Equals(existing, resolved, StringComparison.OrdinalIgnoreCase))
                {
                    sectionStatuses[section] = resolved;
                    ResultOnUi(section, resolved);
                }
            }
        }

        private static string ResolveStatus(string existing, string incoming)
        {
            var existingRank = StatusRank(existing);
            var incomingRank = StatusRank(incoming);
            return incomingRank >= existingRank ? incoming : existing;
        }

        private static int StatusRank(string status)
        {
            return status.ToUpperInvariant() switch
            {
                "FAILURE" => 3,
                "WARNING" => 2,
                "SUCCESS" => 1,
                _ => 0
            };
        }

        private void CompleteSectionIfNeeded(
            object stateLock,
            HashSet<string> completedSections,
            string section)
        {
            if (!IsKnownSection(section) || section.Equals("Process Explorer", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            CompleteSection(stateLock, completedSections, section);
        }

        private void CompleteSection(
            object stateLock,
            HashSet<string> completedSections,
            string section)
        {
            if (!IsKnownSection(section))
            {
                return;
            }

            lock (stateLock)
            {
                if (!completedSections.Add(section))
                {
                    return;
                }

                var progress = completedSections.Count * SectionProgressIncrement;
                ProgressOnUi(Math.Min(100, progress));
            }
        }

        private static bool IsKnownSection(string section)
        {
            return Array.Exists(SectionOrder, s => string.Equals(s, section, StringComparison.OrdinalIgnoreCase));
        }

        private string? ExtractEmbeddedScriptToTemp()
        {
            var assembly = typeof(ScanEngine).Assembly;
            var resourceName = "Hawkbat.Scripts.RecordingPolicy.ps1";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return null;
            }

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            var tempPath = Path.Combine(Path.GetTempPath(), $"RecordingPolicy_{Guid.NewGuid():N}.ps1");
            File.WriteAllText(tempPath, content);
            return tempPath;
        }

        private void TryDeleteTempFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                LogOnUi("Recording Policy", $"Failed to clean up temp script: {ex.Message}");
            }
        }

        private void LogOnUi(string category, string message)
        {
            if (Application.Current?.Dispatcher == null)
            {
                _log(category, message);
                return;
            }

            Application.Current.Dispatcher.Invoke(() => _log(category, message));
        }

        private void ResultOnUi(string key, string status)
        {
            if (Application.Current?.Dispatcher == null)
            {
                _resultCallback(key, status);
                return;
            }

            Application.Current.Dispatcher.Invoke(() => _resultCallback(key, status));
        }

        private void ProgressOnUi(double value)
        {
            if (Application.Current?.Dispatcher == null)
            {
                _progressCallback(value);
                return;
            }

            Application.Current.Dispatcher.Invoke(() => _progressCallback(value));
        }

        private static string? GetWmiString(string wmiClass, string property)
        {
            try
            {
                using var search = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
                foreach (ManagementObject obj in search.Get())
                {
                    var v = obj[property];
                    if (v != null) return v.ToString();
                }
            }
            catch { }
            return null;
        }
    }
}
