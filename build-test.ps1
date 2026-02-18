<#
  DEBUG TEST BUILD SCRIPT
  This script creates a local self-contained Debug test build for manual testing only.
  Do NOT distribute the output of this script to real users. Debug builds disable anti-tamper and contain no embedded release hash.
#>

try {
    $dotnetVersion = & dotnet --version 2>$null
} catch {
    Write-Error "dotnet not found. Install .NET 8 SDK from https://dotnet.microsoft.com/download"
    exit 1
}

if (-not $dotnetVersion) {
    Write-Error "dotnet not found. Install .NET 8 SDK from https://dotnet.microsoft.com/download"
    exit 1
}

# Parse version and ensure major >= 8
try {
    $v = [System.Version]::Parse($dotnetVersion.Trim())
} catch {
    Write-Warning "Unable to parse dotnet version string '$dotnetVersion'. Proceeding but build may fail."
    $v = New-Object System.Version 0,0,0,0
}

if ($v.Major -lt 8) {
    Write-Error "Installed dotnet SDK version $dotnetVersion is older than required .NET 8. Download: https://dotnet.microsoft.com/download"
    exit 1
}

Write-Host "Using dotnet SDK version $dotnetVersion"

$outDir = Join-Path -Path $PSScriptRoot -ChildPath 'TestBuild'

if (Test-Path $outDir) {
    Write-Host "Removing existing TestBuild folder..."
    Remove-Item -Recurse -Force -LiteralPath $outDir
}

Write-Host "Publishing self-contained Debug build to $outDir"

$publishCmd = @(
    'dotnet', 'publish', '--configuration', 'Debug', '--runtime', 'win-x64', '--self-contained', 'true',
    '-p:PublishSingleFile=true', "--output", $outDir
)

$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = $publishCmd[0]
$startInfo.Arguments = ($publishCmd[1..($publishCmd.Length - 1)] -join ' ')
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.UseShellExecute = $false

$p = New-Object System.Diagnostics.Process
$p.StartInfo = $startInfo
$p.Start() | Out-Null
$stdout = $p.StandardOutput.ReadToEnd()
$stderr = $p.StandardError.ReadToEnd()
$p.WaitForExit()

Write-Host $stdout
if ($p.ExitCode -ne 0) {
    Write-Error "dotnet publish failed with exit code $($p.ExitCode)"
    Write-Error $stderr
    exit $p.ExitCode
}

$exePath = Join-Path -Path $outDir -ChildPath '327TH_HB_AC.exe'
if (-not (Test-Path $exePath)) {
    Write-Error "Publish completed but executable not found at $exePath. Build failed."
    exit 1
}

$full = (Resolve-Path $exePath).ProviderPath
Write-Host "Build successful. Executable ready at: $full"

Start-Process explorer.exe $outDir
exit 0
