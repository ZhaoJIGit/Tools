using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TaskMGPro.Helper
{
    public static class StringExtensions
    {
        public static Color ToColor(this string hex)
        {
            if (string.IsNullOrEmpty(hex))
                throw new ArgumentException("Invalid hex color string.");

            // Remove the leading '#' if it exists
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);

            // Ensure the hex string is in correct length
            if (hex.Length == 6)
                hex = "FF" + hex; // Add alpha channel if missing

            // Convert hex to Color
            return (Color)ColorConverter.ConvertFromString("#" + hex);
        }
        public static Brush ToBrushColor(this string hex)
        {
            return new SolidColorBrush(hex.ToColor());
        }
    }
}
