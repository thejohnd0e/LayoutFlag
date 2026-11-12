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
        Assert.Equal(Point.Empty, s.Location);
        Assert.False(s.AutoStart);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var s = AppSettings.Load(Path.Combine(Path.GetTempPath(), "lf_no_such_file_xyz.json"));
        Assert.Equal(Point.Empty, s.Location);
        Assert.False(s.AutoStart);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        string path = Path.Combine(Path.GetTempPath(), $"lf_{Path.GetRandomFileName()}.json");
        var s = new AppSettings { Location = new Point(11, 22), AutoStart = true };
        s.Save(path);
        var loaded = AppSettings.Load(path);
        Assert.Equal(new Point(11, 22), loaded.Location);
        Assert.True(loaded.AutoStart);
    }
}
