# Production build script for 327th Hawkbat Recording Policy
# Self-contained Release build with anti-tamper enabled
# Makes zero network calls — safe to distribute as a single file
# Run this script on Windows only

$downloadLink = "https://dotnet.microsoft.com/download"

try {
    $sdks = & dotnet --list-sdks 2>$null
} catch {
    Write-Error "ERROR: .NET 8 SDK is required. Download from $downloadLink"
    exit 1
}

if (-not $sdks) {
    Write-Error "ERROR: .NET 8 SDK is required. Download from $downloadLink"
    exit 1
}

$hasNet8 = $false
foreach ($line in $sdks) {
    if ($line -match '^8\.') {
        $hasNet8 = $true
        break
    }
}

if (-not $hasNet8) {
    Write-Error "ERROR: .NET 8 SDK is required. Download from $downloadLink"
    exit 1
}

$outDir = Join-Path -Path $PSScriptRoot -ChildPath 'ProductionBuild'
$tempDir = Join-Path -Path $PSScriptRoot -ChildPath '.publish-temp'

# Clean temp directory
if (Test-Path $tempDir) {
    Remove-Item -Recurse -Force -LiteralPath $tempDir -ErrorAction SilentlyContinue
}

dotnet publish 327TH_HB_AC.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true --output $tempDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed."
    Remove-Item -Recurse -Force -LiteralPath $tempDir -ErrorAction SilentlyContinue
    exit $LASTEXITCODE
}

$exePath = Join-Path -Path $tempDir -ChildPath '327TH_HB_AC.exe'
if (-not (Test-Path $exePath)) {
    Write-Error "ERROR: Release executable not found at $exePath"
    Remove-Item -Recurse -Force -LiteralPath $tempDir -ErrorAction SilentlyContinue
    exit 1
}

# Move from temp to final output directory
if (Test-Path $outDir) {
    try {
        Remove-Item -Recurse -Force -LiteralPath $outDir -ErrorAction Stop
    } catch {
        $timestamp = Get-Date -Format "yyyyMMddHHmmss"
        Rename-Item -Path $outDir -NewName "ProductionBuild_old_$timestamp" -Force -ErrorAction SilentlyContinue
    }
}

Move-Item -Path $tempDir -Destination $outDir -Force

$exePath = Join-Path -Path $outDir -ChildPath '327TH_HB_AC.exe'

Write-Host "SUCCESS: Production build complete." -ForegroundColor Green
Write-Host "Executable: $exePath"
Write-Host "This build is self-contained and ready to distribute."

Start-Process explorer.exe $outDir
