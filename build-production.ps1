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
if (Test-Path $outDir) {
    Remove-Item -Recurse -Force -LiteralPath $outDir
}

# Clean the generated hash file before building to avoid duplicate definition errors
$hashFile = Join-Path $PSScriptRoot 'Config\BuildTimeHashHolder.g.cs'
if (Test-Path $hashFile) {
    Remove-Item -Force -LiteralPath $hashFile
}

dotnet publish --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true --output .\ProductionBuild
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed."
    exit $LASTEXITCODE
}

$exePath = Join-Path -Path $outDir -ChildPath '327TH_HB_AC.exe'
if (-not (Test-Path $exePath)) {
    Write-Error "ERROR: Release executable not found at $exePath"
    exit 1
}

$hashFile = Join-Path $PSScriptRoot 'Config\BuildTimeHashHolder.g.cs'
if (-not (Test-Path $hashFile)) {
    Write-Error "ERROR: Build-time hash file not found at Config\BuildTimeHashHolder.g.cs"
    exit 1
}

$hashContent = Get-Content $hashFile -Raw
if ($hashContent -match 'PENDING') {
    Write-Host "ERROR: Build-time hash was not embedded. This build is not safe to distribute."
    Write-Host "Run 'dotnet build --configuration Release' first, then run this script again."
    exit 1
}

if ($hashContent -match 'BuildTimeHash\s*=\s*"([0-9A-Fa-f]{64})"') {
    $embeddedHash = $matches[1]
} else {
    Write-Error "ERROR: Embedded hash not found or invalid in Config\BuildTimeHashHolder.g.cs"
    exit 1
}

$fileHash = (Get-FileHash -Path $exePath -Algorithm SHA256).Hash
Write-Host "Executable SHA-256:  $fileHash"
Write-Host "Embedded hash:       $embeddedHash"

if ($fileHash -ne $embeddedHash) {
    Write-Host "ERROR: Embedded hash does not match executable hash." -ForegroundColor Red
    exit 1
}

Write-Host "SUCCESS: Hash verified. Build is ready to distribute." -ForegroundColor Green
Write-Host "Executable: $exePath"
Write-Host "This build is self-contained and ready to distribute."

Start-Process explorer.exe $outDir
