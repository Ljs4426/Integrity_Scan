using System.Windows;
using System.Windows.Media;
using Hawkbat.Models;

namespace Hawkbat.Services
{
    public static class ThemeManager
    {
        public static void ApplyUnitTheme(UnitPack unit)
        {
            if (unit == null)
            {
                return;
            }

            var dict = Application.Current.Resources;

            try
            {
                var accentBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(unit.AccentColor));
                dict["AccentColor"] = accentBrush;
                dict["AccentBrush"] = accentBrush;  // Also update AccentBrush for compatibility
            }
            catch { }

            try
            {
                dict["BackgroundColor"] = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(unit.BackgroundColor));
            }
            catch { }

            try
            {
                dict["TextColor"] = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(unit.TextColor));
            }
            catch { }
        }
    }
}
