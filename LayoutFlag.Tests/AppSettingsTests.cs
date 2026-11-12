using LayoutFlag;
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
        Assert.Null(s.Location);
        Assert.Equal(BadgeMode.Flags, s.Mode);
        Assert.Equal(BadgeShape.Rounded, s.Shape);
        Assert.Equal(BadgeSize.Medium, s.BadgeSize);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var s = AppSettings.Load(Path.Combine(Path.GetTempPath(), "lf_no_such_file_xyz.json"));
        Assert.Null(s.Location);
        Assert.Equal(BadgeMode.Flags, s.Mode);
        Assert.Equal(BadgeShape.Rounded, s.Shape);
        Assert.Equal(BadgeSize.Medium, s.BadgeSize);
    }

    [Fact]
    public void Load_LegacyJsonWithoutAppearanceFields_KeepsLocationAndUsesAppearanceDefaults()
    {
        string path = TempFile("""{"Location":{"X":15,"Y":25}}""");
        var s = AppSettings.Load(path);
        Assert.Equal(new Point(15, 25), s.Location);
        Assert.Equal(BadgeMode.Flags, s.Mode);
        Assert.Equal(BadgeShape.Rounded, s.Shape);
        Assert.Equal(BadgeSize.Medium, s.BadgeSize);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        string path = Path.Combine(Path.GetTempPath(), $"lf_{Path.GetRandomFileName()}.json");
        var s = new AppSettings
        {
            Location = new Point(11, 22),
            Mode = BadgeMode.Color,
            Shape = BadgeShape.Circle,
            BadgeSize = BadgeSize.Large
        };
        s.Save(path);
        var loaded = AppSettings.Load(path);
        Assert.Equal(new Point(11, 22), loaded.Location);
        Assert.Equal(BadgeMode.Color, loaded.Mode);
        Assert.Equal(BadgeShape.Circle, loaded.Shape);
        Assert.Equal(BadgeSize.Large, loaded.BadgeSize);
    }
}
