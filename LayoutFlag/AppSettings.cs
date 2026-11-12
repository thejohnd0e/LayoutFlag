using System;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace LayoutFlag;

public class AppSettings
{
    public Point Location { get; set; } = Point.Empty;
    public bool AutoStart { get; set; }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    private static string DefaultPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LayoutFlag", "settings.json");

    public static AppSettings Load() => Load(DefaultPath);

    public static AppSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save() => Save(DefaultPath);

    public void Save(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOpts));
        }
        catch
        {
            // позиция — не критичные данные: молча пропускаем
        }
    }
}
