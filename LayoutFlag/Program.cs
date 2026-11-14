using System;
using System.Threading;
using System.Windows.Forms;

namespace LayoutFlag;

static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    static void Main(string[] args)
    {
        _mutex = new Mutex(true, @"Local\LayoutFlagSingleInstance", out bool createdNew);
        if (!createdNew && !AcquireAfterRestart(_mutex))
        {
            return; // действительно запущен другой живой экземпляр
        }

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new TrayAppContext(ParseInitialLanguage(args)));
        }
        finally
        {
            // Явно отпускаем, чтобы новый процесс после Application.Restart
            // не получил AbandonedMutexException
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }

    /// <summary>
    /// Application.Restart запускает новый процесс раньше смерти старого —
    /// даём старому до 3 секунд отпустить Mutex.
    /// </summary>
    private static bool AcquireAfterRestart(Mutex mutex)
    {
        try
        {
            return mutex.WaitOne(TimeSpan.FromSeconds(3));
        }
        catch (AbandonedMutexException)
        {
            // Прежний владелец завершился, не отпустив Mutex (например, упал) —
            // владение при этом всё равно переходит к нам
            return true;
        }
    }

    private static UiLanguage? ParseInitialLanguage(string[] args)
    {
        foreach (string arg in args)
        {
            if (arg.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
            {
                string val = arg.Substring("--lang=".Length).Trim().ToLowerInvariant();
                return val switch
                {
                    "ru" or "russian" => UiLanguage.Ru,
                    "en" or "english" => UiLanguage.En,
                    _ => null
                };
            }
        }
        return null;
    }
}
