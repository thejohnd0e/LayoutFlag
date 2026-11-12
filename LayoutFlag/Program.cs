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
        if (!createdNew)
        {
            // Application.Restart запускает новый процесс раньше смерти старого —
            // даём старому до 3 секунд отпустить Mutex, иначе рестарт молча не сработает
            if (!_mutex.WaitOne(TimeSpan.FromSeconds(3)))
            {
                return; // действительно запущен другой живой экземпляр
            }
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext(ParseInitialLanguage(args)));
        GC.KeepAlive(_mutex);
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
