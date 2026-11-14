using LayoutFlag;
using System.Diagnostics;
using System.Text.RegularExpressions;

public class VersionTests
{
    [Fact]
    public void StringsVersion_MatchesAssemblyFileVersion()
    {
        var fv = FileVersionInfo.GetVersionInfo(typeof(Strings).Assembly.Location);
        Assert.Matches(new Regex(@"^\d+\.\d+\.\d+$"), Strings.Version);
        Assert.Equal($"{fv.FileMajorPart}.{fv.FileMinorPart}.{fv.FileBuildPart}", Strings.Version);
    }
}
