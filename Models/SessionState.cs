namespace Hawkbat.Models
{
    public enum LocalScriptOption
    {
        Default,
        Pasted,
        GitHub
    }

    public static class SessionState
    {
        public static string HashedUsername { get; set; }

        public static UnitPack SelectedUnit { get; set; }

        public static List<UnitPack> InstalledUnits { get; set; } = new();

        public static LocalScriptOption LastLocalScriptOption { get; set; } = LocalScriptOption.Default;
    }
}
