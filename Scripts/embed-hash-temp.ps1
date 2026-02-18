$hash = (Get-FileHash 'C:\Users\Luke\Downloads\327TH_HB_AC\ProductionBuild\327TH_HB_AC.exe' -Algorithm SHA256).Hash
$content = @"
namespace Hawkbat.Config
{
    // AUTO-GENERATED FILE. Do not edit by hand.
    internal static class BuildTimeHashHolder
    {
        public const string BuildTimeHash = "$hash"
}
}
`"@
Set-Content -Path 'C:\Users\Luke\Downloads\327TH_HB_AC\Config\BuildTimeHashHolder.g.cs' -Value $content
