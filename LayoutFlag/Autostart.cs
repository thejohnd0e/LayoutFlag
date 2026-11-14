using System;
using System.IO;
using Microsoft.Win32;

namespace LayoutFlag;

/// <summary>
/// Автозапуск через HKCU\Software\Microsoft\Windows\CurrentVersion\Run — то же место,
/// куда пишет инсталлятор, поэтому меню и инсталлятор видят одно и то же состояние.
/// </summary>
public static class Autostart
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string ValueName = "LayoutFlag";

    private static string LegacyShortcutPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "LayoutFlag.lnk");

    public static string BuildCommand(string exePath) => $"\"{exePath}\"";

    public static bool IsEnabled() => IsEnabled(RunKeyPath, ValueName);

    public static bool IsEnabled(string keyPath, string valueName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: false);
            return key?.GetValue(valueName) is string s && s.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Никогда не бросает: при сбое возвращает false.</summary>
    public static bool SetEnabled(bool enable)
    {
        RemoveLegacyShortcut();
        string? exe = Environment.ProcessPath;
        if (enable && string.IsNullOrEmpty(exe)) return false;
        return SetEnabled(RunKeyPath, ValueName, enable, exe ?? string.Empty);
    }

    public static bool SetEnabled(string keyPath, string valueName, bool enable, string exePath)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(keyPath, writable: true);
            if (enable)
            {
                key.SetValue(valueName, BuildCommand(exePath), RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(valueName, throwOnMissingValue: false);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Миграция с версий ≤1.2.0, где автозапуск делался ярлыком в shell:startup.
    /// Если ярлык был — переносим его в реестр, чтобы не было двойного запуска.
    /// </summary>
    public static void MigrateLegacyShortcut()
    {
        try
        {
            if (!File.Exists(LegacyShortcutPath)) return;
            string? exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
            {
                SetEnabled(RunKeyPath, ValueName, true, exe);
            }
            File.Delete(LegacyShortcutPath);
        }
        catch
        {
            // не критично
        }
    }

    private static void RemoveLegacyShortcut()
    {
        try
        {
            if (File.Exists(LegacyShortcutPath)) File.Delete(LegacyShortcutPath);
        }
        catch
        {
            // не критично
        }
    }
}
