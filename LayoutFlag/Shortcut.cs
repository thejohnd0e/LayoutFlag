using System;
using System.IO;

namespace LayoutFlag;

public static class Shortcut
{
    /// <summary>
    /// Создать/удалить ярлык LayoutFlag.lnk в shell:startup.
    /// Никогда не бросает: сбой COM/файловой системы возвращает false.
    /// </summary>
    public static bool ToggleAutostart(bool enable)
    {
        try
        {
            string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string lnkPath = Path.Combine(startupDir, "LayoutFlag.lnk");

            if (!enable)
            {
                if (File.Exists(lnkPath)) File.Delete(lnkPath);
                return true;
            }

            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null) return false;
            dynamic shell = Activator.CreateInstance(shellType) ?? throw new InvalidOperationException("WScript.Shell unavailable");
            try
            {
                dynamic shortcut = shell.CreateShortcut(lnkPath);
                try
                {
                    shortcut.TargetPath = Environment.ProcessPath;
                    shortcut.WorkingDirectory = AppContext.BaseDirectory;
                    shortcut.Description = "LayoutFlag — индикатор раскладки";
                    shortcut.Save();
                    return true;
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
        catch
        {
            return false; // автозапуск — опция: тихо сообщаем о неудаче через меню
        }
    }

    public static bool AutostartEnabled()
    {
        string lnkPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup), "LayoutFlag.lnk");
        return File.Exists(lnkPath);
    }
}
