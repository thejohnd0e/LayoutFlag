using System;
using System.IO;

namespace LayoutFlag;

public static class Shortcut
{
    /// <summary>Создать/удалить ярлык LayoutFlag.lnk в shell:startup.</summary>
    public static void ToggleAutostart(bool enable)
    {
        string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string lnkPath = Path.Combine(startupDir, "LayoutFlag.lnk");

        if (!enable)
        {
            if (File.Exists(lnkPath)) File.Delete(lnkPath);
            return;
        }

        Type shellType = Type.GetTypeFromProgID("WScript.Shell")!;
        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic shortcut = shell.CreateShortcut(lnkPath);
            try
            {
                shortcut.TargetPath = Environment.ProcessPath;
                shortcut.WorkingDirectory = AppContext.BaseDirectory;
                shortcut.Description = "LayoutFlag — индикатор раскладки";
                shortcut.Save();
            }
            finally
            {
                ((IDisposable)shortcut).Dispose();
            }
        }
        finally
        {
            ((IDisposable)shell).Dispose();
        }
    }

    public static bool AutostartEnabled()
    {
        string lnkPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup), "LayoutFlag.lnk");
        return File.Exists(lnkPath);
    }
}
