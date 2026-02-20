namespace Hawkbat.Models
{
    public class UnitPack
    {
        public string FolderPath { get; set; }

        public string Name { get; set; }

        public string Subtitle { get; set; }

        public string AccentColor { get; set; }

        public string TextColor { get; set; }

        public string BackgroundColor { get; set; }

        public bool HasLogo { get; set; }

        public bool RequireUsername { get; set; } = false;

        public string Version { get; set; }

        public string LogoPath { get; set; }

        public string ScriptPath { get; set; }

        public string TosText { get; set; }

        public string[] ScanResults { get; set; } = new string[0];
    }
}
