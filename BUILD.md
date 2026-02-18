
# Build and Verification Guide

## Prerequisites
- Install .NET 8 SDK on Windows.
- This project targets WPF and Windows desktop APIs; Linux and macOS cannot build WPF projects for Windows.
- Visual Studio 2022 (with ".NET desktop development" workload) or the `dotnet` CLI are both acceptable.

## Debug Builds
- Command: `dotnet build 327TH_HB_AC.csproj --configuration Debug`
- Debug builds are fully functional for local testing.

## Production Builds
- Command: run `./build-production.ps1` from the repository root.
- The script publishes a Release build as a self-contained, single-file executable.
- The finished executable is `./ProductionBuild/327TH_HB_AC.exe` and requires no .NET installation to run.

## What This Application Does
- This application performs recording policy scans locally.
- All logs are encrypted and written to `%LocalAppData%\327TH_Hawkbat`.
- The application makes zero network calls. No data leaves the machine.

## Common Build Problems
- Build fails with an error about `EnableWindowsTargeting`: this means the build is being run on Linux or macOS. Build on Windows only.
- PowerShell execution policy blocks scripts: run `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass` in the session before running the build scripts if needed.

