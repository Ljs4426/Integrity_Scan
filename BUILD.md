
Build and verification guide

Prerequisites
- Install .NET 8 SDK on Windows.
- This project targets WPF and Windows desktop APIs; Linux and macOS cannot build WPF projects for Windows.
- Visual Studio 2022 (with ".NET desktop development" workload) or the `dotnet` CLI are both acceptable.

Debug builds
- Command: `dotnet build --configuration Debug`
- Debug configuration defines `DISABLE_ANTI_TAMPER`, so anti-tamper checks and executable hash verification are skipped.
- In Debug builds `Config/BuildTimeHashHolder.g.cs` contains the placeholder value `PENDING`. This is expected for local testing.

Production builds
- Command: run `.uild-production.ps1` from the repository root.
- The script compiles in Release, embeds the build-time SHA-256 into `Config/BuildTimeHashHolder.g.cs`, verifies the embedded value is not `PENDING`, compares the embedded value to the computed SHA-256 of the produced executable, and opens the output folder.
- The finished executable is `.\ProductionBuild\327TH_HB_AC.exe` and is a self-contained single-file binary. The target machine does not need .NET installed to run it.

Verifying the embedded hash manually
Use PowerShell to compute the SHA-256 of the built executable and compare it to the generated file:

```powershell
# Compute file hash
Get-FileHash -Algorithm SHA256 -Path .\ProductionBuild\327TH_HB_AC.exe

# Print generated holder file
Get-Content .\Config\BuildTimeHashHolder.g.cs
```

What this application does and does not do
- This application performs recording policy scans locally.
- All logs are encrypted and written to `%LocalAppData%\327TH_Hawkbat`.
- The application makes zero network calls. No data leaves the machine.

Common build problems
- App closes immediately on launch in Release mode: this indicates the executable hash check failed. Rebuild with `dotnet build --configuration Release` or run `.uild-production.ps1` and verify the embedded hash matches the executable.
- Build fails with an error about `EnableWindowsTargeting`: this means the build is being run on Linux or macOS. Build on Windows only.
- `Config\BuildTimeHashHolder.g.cs` still contains `PENDING` after a Release build: the MSBuild target did not run. Ensure PowerShell is available and that the embed target was not skipped.
- PowerShell execution policy blocks scripts: run `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass` in the session before running the build scripts if needed.

