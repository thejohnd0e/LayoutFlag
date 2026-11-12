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
            return; // уже запущено — тихо выходим
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new FlagForm());
        GC.KeepAlive(_mutex);
    }
}
