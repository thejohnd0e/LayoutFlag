using LayoutFlag;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

public class AppSettingsTests
{
    private static string TempFile(string content)
    {
        string path = Path.Combine(Path.GetTempPath(), $"lf_{Path.GetRandomFileName()}.json");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Load_BrokenJson_ReturnsDefaultsWithoutThrowing()
    {
        string path = TempFile("{ this is not json !!!");
        var s = AppSettings.Load(path);
        Assert.Equal(1, s.BadgeCount);
        Assert.Empty(s.Locations);
        Assert.Equal(BadgeMode.Flags, s.Mode);
        Assert.Equal(BadgeShape.Rounded, s.Shape);
        Assert.Equal(BadgeSize.Medium, s.BadgeSize);
        Assert.Equal(UiLanguage.Auto, s.Language);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var s = AppSettings.Load(Path.Combine(Path.GetTempPath(), "lf_no_such_file_xyz.json"));
        Assert.Equal(1, s.BadgeCount);
        Assert.Empty(s.Locations);
        Assert.Equal(BadgeMode.Flags, s.Mode);
        Assert.Equal(BadgeShape.Rounded, s.Shape);
        Assert.Equal(BadgeSize.Medium, s.BadgeSize);
        Assert.Equal(UiLanguage.Auto, s.Language);
    }

    [Fact]
    public void Load_LegacyJsonWithSingleLocation_MigratesToLocationsList()
    {
        string path = TempFile("""{"Location":{"X":15,"Y":25}}""");
        var s = AppSettings.Load(path);
        Assert.Equal(1, s.BadgeCount);
        Assert.Single(s.Locations);
        Assert.Equal(new Point(15, 25), s.Locations[0]);
        Assert.Equal(BadgeMode.Flags, s.Mode);
        Assert.Equal(BadgeShape.Rounded, s.Shape);
        Assert.Equal(BadgeSize.Medium, s.BadgeSize);
        Assert.Equal(UiLanguage.Auto, s.Language);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(2, 2)]
    [InlineData(4, 4)]
    [InlineData(99, 4)]
    public void Load_ClampsBadgeCountToValidRange(int rawCount, int expectedCount)
    {
        string path = TempFile($"{{\"BadgeCount\":{rawCount}}}");
        var s = AppSettings.Load(path);
        Assert.Equal(expectedCount, s.BadgeCount);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFieldsAndLocations()
    {
        string path = Path.Combine(Path.GetTempPath(), $"lf_{Path.GetRandomFileName()}.json");
        var s = new AppSettings
        {
            BadgeCount = 2,
            Locations = new List<Point> { new(11, 22), new(1950, 500) },
            Mode = BadgeMode.Color,
            Shape = BadgeShape.Circle,
            BadgeSize = BadgeSize.Large,
            Language = UiLanguage.En
        };
        s.Save(path);
        var loaded = AppSettings.Load(path);
        Assert.Equal(2, loaded.BadgeCount);
        Assert.Equal(new List<Point> { new(11, 22), new(1950, 500) }, loaded.Locations);
        Assert.Equal(BadgeMode.Color, loaded.Mode);
        Assert.Equal(BadgeShape.Circle, loaded.Shape);
        Assert.Equal(BadgeSize.Large, loaded.BadgeSize);
        Assert.Equal(UiLanguage.En, loaded.Language);
    }
}

public class AppSettingsAtomicSaveTests
{
    [Fact]
    public void Save_OverwritesExistingFile_AndLeavesNoTempFile()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"lf_{Path.GetRandomFileName()}");
        string path = Path.Combine(dir, "settings.json");

        new AppSettings { BadgeCount = 2 }.Save(path);
        new AppSettings { BadgeCount = 3 }.Save(path);

        Assert.Equal(3, AppSettings.Load(path).BadgeCount);
        Assert.False(File.Exists(path + ".tmp"));
        Directory.Delete(dir, true);
    }
}
