using System;
using System.Threading;

namespace LayoutFlag;

static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    static void Main()
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
        Application.Run(new FlagForm());
        GC.KeepAlive(_mutex);
    }
}
