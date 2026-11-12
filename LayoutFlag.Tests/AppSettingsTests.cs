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
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(2, 2)]
    [InlineData(99, 3)]
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
            BadgeSize = BadgeSize.Large
        };
        s.Save(path);
        var loaded = AppSettings.Load(path);
        Assert.Equal(2, loaded.BadgeCount);
        Assert.Equal(new List<Point> { new(11, 22), new(1950, 500) }, loaded.Locations);
        Assert.Equal(BadgeMode.Color, loaded.Mode);
        Assert.Equal(BadgeShape.Circle, loaded.Shape);
        Assert.Equal(BadgeSize.Large, loaded.BadgeSize);
    }
}
