using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LayoutFlag;

public enum BadgeMode
{
    Flags,
    Color
}

public enum BadgeShape
{
    Rounded,
    Rectangle,
    Circle
}

public enum BadgeSize
{
    Small,
    Medium,
    Large
}

public class AppSettings
{
    public Point? Location { get; set; }
    public BadgeMode Mode { get; set; } = BadgeMode.Flags;
    public BadgeShape Shape { get; set; } = BadgeShape.Rounded;
    public BadgeSize BadgeSize { get; set; } = BadgeSize.Medium;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
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
            var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOpts) ?? new AppSettings();
            if (!Enum.IsDefined(loaded.Mode)) loaded.Mode = BadgeMode.Flags;
            if (!Enum.IsDefined(loaded.Shape)) loaded.Shape = BadgeShape.Rounded;
            if (!Enum.IsDefined(loaded.BadgeSize)) loaded.BadgeSize = BadgeSize.Medium;
            return loaded;
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
            // настройки — не критичные данные: молча пропускаем ошибку записи
        }
    }
}
