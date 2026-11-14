using LayoutFlag;
using System;
using Microsoft.Win32;

public class AutostartTests : IDisposable
{
    // Изолированный ключ: реальный Run-ключ пользователя тесты не трогают
    private readonly string _keyPath = $@"Software\LayoutFlagTests\{Guid.NewGuid():N}";
    private const string Value = "LayoutFlag";

    [Fact]
    public void Enable_ThenDisable_RoundTrips()
    {
        Assert.False(Autostart.IsEnabled(_keyPath, Value));

        Assert.True(Autostart.SetEnabled(_keyPath, Value, true, @"C:\Apps\LayoutFlag.exe"));
        Assert.True(Autostart.IsEnabled(_keyPath, Value));

        using (var key = Registry.CurrentUser.OpenSubKey(_keyPath)!)
        {
            Assert.Equal("\"C:\\Apps\\LayoutFlag.exe\"", key.GetValue(Value));
        }

        Assert.True(Autostart.SetEnabled(_keyPath, Value, false, string.Empty));
        Assert.False(Autostart.IsEnabled(_keyPath, Value));
    }

    [Fact]
    public void Disable_WhenMissing_DoesNotFail()
        => Assert.True(Autostart.SetEnabled(_keyPath, Value, false, string.Empty));

    [Fact]
    public void BuildCommand_QuotesPathWithSpaces()
        => Assert.Equal("\"C:\\Program Files\\LayoutFlag\\LayoutFlag.exe\"",
            Autostart.BuildCommand(@"C:\Program Files\LayoutFlag\LayoutFlag.exe"));

    public void Dispose()
    {
        try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\LayoutFlagTests", throwOnMissingSubKey: false); }
        catch { }
    }
}
