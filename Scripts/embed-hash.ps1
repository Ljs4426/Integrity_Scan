# This script generates the BuildTimeHashHolder.g.cs file with the computed build-time hash.
# It is called by the MSBuild EmbedBuildTimeHash target during Release builds.
# Parameters:
#   - PublishDir: The publish output directory containing the final executable
#   - ProjectDir: The project root directory where Config/ resides

param(
    [string]$PublishDir,
    [string]$ProjectDir
)

# Trim trailing backslashes from parameters
$PublishDir = $PublishDir.TrimEnd('\')
$ProjectDir = $ProjectDir.TrimEnd('\')

$exePath = Join-Path $PublishDir "327TH_HB_AC.exe"
$outputPath = Join-Path $ProjectDir "Config" "BuildTimeHashHolder.g.cs"

if (-not (Test-Path $exePath)) {
    Write-Error "Executable not found at: $exePath"
    exit 1
}

# Compute the SHA-256 hash of the executable
$hash = (Get-FileHash $exePath -Algorithm SHA256).Hash

# Generate the C# file content with proper quoting
$content = @"
namespace Hawkbat.Config
{
    // AUTO-GENERATED FILE. Do not edit by hand.
    internal static class BuildTimeHashHolder
    {
        public const string BuildTimeHash = "$hash";
    }
}
"@

# Write the content to the output file
Set-Content -Path $outputPath -Value $content

Write-Host "Hash embedding complete: $hash"
