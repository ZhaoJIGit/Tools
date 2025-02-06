using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Notes.APP.Common
{
    public static class StartupManager
    {
        private const string AppName = "MyNotes"; // 你的应用名称
        private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// 设置开机启动
        /// </summary>
        public static void EnableAutoStartup()
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            key?.SetValue(AppName, exePath);
        }

        /// <summary>
        /// 取消开机启动
        /// </summary>
        public static void DisableAutoStartup()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
            key?.DeleteValue(AppName, false);
        }

        /// <summary>
        /// 检查是否已设置开机启动
        /// </summary>
        public static bool IsAutoStartupEnabled()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, false);
            return key?.GetValue(AppName) != null;
        }
    }
}
