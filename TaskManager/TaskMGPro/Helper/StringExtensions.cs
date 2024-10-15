using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public static class SqliteParameterHelper
    {
        /// <summary>
        /// 转化SqlParameter 适合简单类型，复杂类型慎用
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static SqliteParameter[] ToSqliteParameters(this object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var parameters = new List<SqliteParameter>();

            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                // 防止 null 值
                var value = property.GetValue(obj) ?? DBNull.Value;
                // 如果属性是数组或集合，则不处理
                if (property.PropertyType.IsArray || (property.PropertyType.IsClass && property.PropertyType != typeof(string)))
                {
                    continue;
                }
                // 处理枚举类型，将其转换为整数
                if (property.PropertyType.IsEnum)
                {
                    value = (int)value; // 将枚举转换为整数
                }
                // 处理 DateTime 类型
                if (property.PropertyType == typeof(DateTime))
                {
                    value = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
                }
                parameters.Add(new SqliteParameter($"@{property.Name}", value));
            }

            return parameters.ToArray();
        }
    }
}
