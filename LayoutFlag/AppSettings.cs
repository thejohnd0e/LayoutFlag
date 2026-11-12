using System;
using System.Collections.Generic;
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

public enum UiLanguage
{
    Auto,
    Ru,
    En
}

public class AppSettings
{
    public const int MaxBadges = 4;

    public int BadgeCount { get; set; } = 1;
    public List<Point> Locations { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Point? Location { get; set; }

    public BadgeMode Mode { get; set; } = BadgeMode.Flags;
    public BadgeShape Shape { get; set; } = BadgeShape.Rounded;
    public BadgeSize BadgeSize { get; set; } = BadgeSize.Medium;
    public UiLanguage Language { get; set; } = UiLanguage.Auto;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string DefaultPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LayoutFlag", "settings.json");

    internal static bool DefaultFileExists() => File.Exists(DefaultPath);

    public static AppSettings Load() => Load(DefaultPath);

    public static AppSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new AppSettings();
            var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOpts) ?? new AppSettings();
            loaded.BadgeCount = Math.Clamp(loaded.BadgeCount, 1, MaxBadges);
            loaded.Locations ??= new List<Point>();

            // Миграция старого формата с одиночной точкой Location
            if (loaded.Locations.Count == 0 && loaded.Location is not null)
            {
                loaded.Locations.Add(loaded.Location.Value);
            }
            loaded.Location = null;

            if (!Enum.IsDefined(loaded.Mode)) loaded.Mode = BadgeMode.Flags;
            if (!Enum.IsDefined(loaded.Shape)) loaded.Shape = BadgeShape.Rounded;
            if (!Enum.IsDefined(loaded.BadgeSize)) loaded.BadgeSize = BadgeSize.Medium;
            if (!Enum.IsDefined(loaded.Language)) loaded.Language = UiLanguage.Auto;
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
            Location = null;
            BadgeCount = Math.Clamp(BadgeCount, 1, MaxBadges);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOpts));
        }
        catch
        {
            // настройки — не критичные данные: молча пропускаем ошибку записи
        }
    }
}
