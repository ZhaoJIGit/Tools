using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Notes.APP.Common
{
    public static class ColorHelper
    {
        public static SolidColorBrush HexToBrush(string hexColor)
        {
            // 确保 hexColor 是有效的 16 进制颜色字符串（例如 #FF5733）
            if (hexColor.StartsWith("#"))
            {
                hexColor = hexColor.Substring(1); // 去掉前面的 #
            }

            // 将 16 进制字符串转换为 Color 对象
            Color color = (Color)ColorConverter.ConvertFromString("#" + hexColor);

            // 创建并返回 SolidColorBrush
            return new SolidColorBrush(color);
        }
        public static Color HexToColor(string hexColor)
        {
            // 确保 hexColor 是有效的 16 进制颜色字符串（例如 #FF5733）
            if (hexColor.StartsWith("#"))
            {
                hexColor = hexColor.Substring(1); // 去掉前面的 #
            }

            // 将 16 进制字符串转换为 Color 对象
            Color color = (Color)ColorConverter.ConvertFromString("#" + hexColor);

            // 创建并返回 SolidColorBrush
            return color;
        }
        public static string ColorToHex(Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        public static Color MakeColorTransparent(Color color, double factor)
        {
            // Factor 取值范围是 0.0 到 1.0（越小越淡）
            factor = Math.Clamp(factor, 0.0, 1.0);
            return Color.FromArgb((byte)(color.A * factor), color.R, color.G, color.B);
        }
        private static readonly Random RandomGenerator = new Random();

        public static string GenerateRandomColor()
        {
            var hexColor = Color.FromArgb(
                (byte)RandomGenerator.Next(150,255), // A (透明度)
                (byte)RandomGenerator.Next(256), // R (红)
                (byte)RandomGenerator.Next(256), // G (绿)
                (byte)RandomGenerator.Next(256)  // B (蓝)
            );
            return ColorToHex(hexColor);
        }
        // 判断颜色是否为深色
        public static string GetColorByBackground(string hexColor)
        {
            var color = HexToColor(hexColor);
            double luminance = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;
            if (luminance < 128) // Luminance threshold to consider a color dark
            {
                return "#ffffff";
            }
            else
            {
                return "#000000";
            }
        }
        public static string ToHexColor(this Color color)
        {
            return ColorToHex(color);
        }
        public static Color ToColor(this string color)
        {
            return HexToColor(color);
        }
        public static SolidColorBrush ToSolidColorBrush(this string color)
        {
            return new SolidColorBrush(color.ToColor());
        }
        public static SolidColorBrush ToSolidColorBrush(this Color color)
        {
            return new SolidColorBrush(color);
        }
    }

}
