using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Hawkbat.Models;

namespace Hawkbat.Services
{
    public static class UnitPackLoader
    {
        public static List<UnitPack> LoadUnitPacks()
        {
            var unitsPath = GetUnitsPath();

            if (!Directory.Exists(unitsPath))
            {
                Directory.CreateDirectory(unitsPath);
                CreateDefaultUnitPacks(unitsPath);
            }

            var unitPacks = new List<UnitPack>();
            var unitFolders = Directory.GetDirectories(unitsPath);

            foreach (var folder in unitFolders)
            {
                try
                {
                    var unit = LoadUnitPackFromFolder(folder);
                    if (unit != null)
                    {
                        unitPacks.Add(unit);
                    }
                }
                catch
                {
                    // Skip unit packs with missing or invalid files
                }
            }

            // Sort alphabetically, with LOCAL always last
            unitPacks = unitPacks
                .OrderBy(u => u.Name == "LOCAL" ? "~" : u.Name)  // ~ sorts after letters
                .ToList();

            return unitPacks;
        }

        private static string GetUnitsPath()
        {
            var exeDirectory = AppContext.BaseDirectory;
            return Path.Combine(exeDirectory, "Units");
        }

        private static UnitPack LoadUnitPackFromFolder(string folderPath)
        {
            var folderName = Path.GetFileName(folderPath);
            var unitJsonPath = Path.Combine(folderPath, "unit.json");
            var scriptPath = Path.Combine(folderPath, "script.ps1");
            var tosPath = Path.Combine(folderPath, "tos.txt");
            var logoPath = Path.Combine(folderPath, "logo.png");

            // Check for required files
            if (!File.Exists(unitJsonPath) || !File.Exists(scriptPath) || !File.Exists(tosPath))
            {
                throw new FileNotFoundException($"Missing required files in {folderName}");
            }

            // Parse unit.json
            var jsonText = File.ReadAllText(unitJsonPath);
            var jsonDoc = JsonDocument.Parse(jsonText);
            var root = jsonDoc.RootElement;

            var name = root.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == System.Text.Json.JsonValueKind.String
                ? nameProp.GetString() ?? folderName
                : folderName;

            var subtitle = root.TryGetProperty("subtitle", out var subtitleProp) && subtitleProp.ValueKind == System.Text.Json.JsonValueKind.String
                ? subtitleProp.GetString() ?? string.Empty
                : string.Empty;

            var accentColor = root.TryGetProperty("accentColor", out var accentProp) && accentProp.ValueKind == System.Text.Json.JsonValueKind.String
                ? accentProp.GetString() ?? "#3D1A6E"
                : "#3D1A6E";

            var textColor = root.TryGetProperty("textColor", out var textProp) && textProp.ValueKind == System.Text.Json.JsonValueKind.String
                ? textProp.GetString() ?? "#FFFFFF"
                : "#FFFFFF";

            var backgroundColor = root.TryGetProperty("backgroundColor", out var bgProp) && bgProp.ValueKind == System.Text.Json.JsonValueKind.String
                ? bgProp.GetString() ?? "#0A0A0F"
                : "#0A0A0F";

            var hasLogo = root.TryGetProperty("hasLogo", out var logoProp) && logoProp.ValueKind == System.Text.Json.JsonValueKind.True;

            var version = root.TryGetProperty("version", out var versionProp) && versionProp.ValueKind == System.Text.Json.JsonValueKind.String
                ? versionProp.GetString() ?? "1.0.0"
                : "1.0.0";

            var scanResults = new List<string>();
            if (root.TryGetProperty("scanResults", out var scanResultsProp) && scanResultsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in scanResultsProp.EnumerateArray())
                {
                    if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var resultName = item.GetString();
                        if (!string.IsNullOrEmpty(resultName))
                        {
                            scanResults.Add(resultName);
                        }
                    }
                }
            }

            var unit = new UnitPack
            {
                FolderPath = folderPath,
                Name = name,
                Subtitle = subtitle,
                AccentColor = accentColor,
                TextColor = textColor,
                BackgroundColor = backgroundColor,
                HasLogo = hasLogo,
                Version = version,
                ScriptPath = scriptPath,
                TosText = File.ReadAllText(tosPath),
                ScanResults = scanResults.ToArray()
            };

            if (unit.HasLogo && File.Exists(logoPath))
            {
                unit.LogoPath = logoPath;
            }
            else
            {
                unit.HasLogo = false;
            }

            return unit;
        }

        private static void CreateDefaultUnitPacks(string unitsPath)
        {
            // Create 327th_Hawkbat unit pack
            var hawkbatPath = Path.Combine(unitsPath, "327th_Hawkbat");
            Directory.CreateDirectory(hawkbatPath);

            var hawkbatJson = new
            {
                name = "327th Hawkbat",
                subtitle = "Recording Policy",
                accentColor = "#3D1A6E",
                textColor = "#FFFFFF",
                backgroundColor = "#0A0A0F",
                hasLogo = true,
                version = "1.0.0"
            };
            File.WriteAllText(Path.Combine(hawkbatPath, "unit.json"), JsonSerializer.Serialize(hawkbatJson, new JsonSerializerOptions { WriteIndented = true }));

            // Copy the RecordingPolicy.ps1 to the unit pack
            var recordingPolicyPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "RecordingPolicy.ps1");
            if (File.Exists(recordingPolicyPath))
            {
                File.Copy(recordingPolicyPath, Path.Combine(hawkbatPath, "script.ps1"));
            }

            // Create default ToS for 327th_Hawkbat
            var hawkbatTos = @"TERMS OF SERVICE — 327th Hawkbat Recording Policy
Version 1.0 — Effective: 2026

1. ACCEPTANCE
By clicking Accept you confirm that you have read and understood these terms in full and agree to be bound by them. If you do not agree, click Decline and close the application. Continued use of this application constitutes ongoing acceptance of these terms.

2. PURPOSE AND SCOPE
The 327th Hawkbat Recording Policy tool is an integrity and compliance verification application designed for use by members and applicants of the 327th Hawkbat unit. Its sole purpose is to perform a local system check to verify that your machine is free from cheating software, unauthorised tools, and system integrity violations. This tool is required as a condition of participation in unit activities where fair play and system integrity are enforced.

3. WHAT THIS TOOL DOES
This application performs the following checks entirely on your local machine:

- Scans running processes for known cheating tools, memory editors, packet editors, debuggers, and game trainers
- Verifies Windows Defender status, real-time protection, and checks for suspicious exclusion paths
- Checks system file signatures and PowerShell module integrity
- Reads registry values related to system security features such as Memory Integrity
- Logs hardware information including CPU, GPU, RAM, and Windows version for identification purposes
- Launches Process Explorer to allow a full process list review

4. WHAT THIS TOOL DOES NOT DO
This application does not delete, modify, move, or quarantine any files on your machine. It does not transmit any data to an external server. It does not access personal files, documents, photos, or any data outside of system integrity and process information. It does not record keystrokes, screenshots, or any form of continuous monitoring. The tool runs once per session and stops when you close it.

5. DATA HANDLING AND PRIVACY
The username you enter is converted to a SHA-256 hash immediately on input. The plaintext version of your username is never stored on disk or used beyond the current session. Scan results are saved as encrypted log files in %LocalAppData%\327TH_Hawkbat\ on your machine only. These logs are encrypted using AES-256 and cannot be read without the session key. No data is uploaded, shared, or transmitted to any party under any circumstances. Log files remain on your machine and are your responsibility to manage.

6. ADMINISTRATOR PRIVILEGES
This application requires and will request Administrator privileges on launch. These privileges are necessary to read system security settings, check Windows Defender configuration, and verify file signatures. By accepting these terms you confirm that you are the owner of or are authorised to run administrative software on the machine you are using. Running this tool on a machine you do not own or are not authorised to administer is your sole responsibility.

7. SCAN RESULTS AND CONSEQUENCES
This tool reports findings only — it does not make decisions. Any action taken based on scan results, including removal from unit activities, is at the sole discretion of 327th Hawkbat leadership and is governed by unit rules and policies separate from this application. A FAILURE result does not automatically mean misconduct — context is always reviewed by unit staff. You are responsible for the state of your own machine. The presence of flagged software, suspicious exclusions, or integrity violations on your system is your responsibility regardless of how they got there.

8. VOLUNTARY USE
Use of this tool may be a requirement for participation in certain unit activities. If you do not wish to run this tool, you may decline and you will not be able to proceed. No one is compelled to install or run this application outside of voluntary participation in 327th Hawkbat activities.

9. NO WARRANTY
This application is provided as-is without warranty of any kind. The 327th Hawkbat makes no guarantees about the completeness, accuracy, or exhaustiveness of scan results. This tool does not guarantee the detection of all cheating software or integrity violations. A clean scan result does not constitute a certification of your system's integrity.

10. LIMITATION OF LIABILITY
The 327th Hawkbat and the developers of this application are not liable for any damage, data loss, system issues, or consequences arising from the use of this tool. You run this application at your own risk.

11. CHANGES TO THESE TERMS
These terms may be updated with new versions of the application. Continued use of updated versions constitutes acceptance of updated terms. The version number and effective date at the top of this document identify which terms apply to the version you are running.

12. CONTACT
If you have questions about these terms or the operation of this tool, contact 327th Hawkbat leadership through your normal unit communication channels.

By clicking Accept you confirm you have read these terms, understand what this application does, and agree to proceed.";

            File.WriteAllText(Path.Combine(hawkbatPath, "tos.txt"), hawkbatTos);

            // Create placeholder for logo (will be added separately)
            // For now, just create an empty file that the app will skip if missing

            // Create LOCAL unit pack
            var localPath = Path.Combine(unitsPath, "LOCAL");
            Directory.CreateDirectory(localPath);

            var localJson = new
            {
                name = "LOCAL",
                subtitle = "General Scan",
                accentColor = "#1A4A8A",
                textColor = "#FFFFFF",
                backgroundColor = "#0A0F1A",
                hasLogo = false,
                version = "1.0.0"
            };
            File.WriteAllText(Path.Combine(localPath, "unit.json"), JsonSerializer.Serialize(localJson, new JsonSerializerOptions { WriteIndented = true }));

            // Copy the same script for LOCAL unit
            if (File.Exists(recordingPolicyPath))
            {
                File.Copy(recordingPolicyPath, Path.Combine(localPath, "script.ps1"));
            }

            var localTos = @"LOCAL SCAN TOOL — General Anticheat
Version 1.0

This is a local system scanning tool with no unit affiliation. It performs integrity and security checks on your machine locally without transmitting any data externally.

By using this tool, you acknowledge that:
- All scans are performed locally on your machine only
- No data is sent to external servers
- Results are for informational purposes only
- You are responsible for addressing any issues found
- The tool is provided as-is without warranty

Accept these terms to proceed with the scan.";

            File.WriteAllText(Path.Combine(localPath, "tos.txt"), localTos);
        }
    }
}
