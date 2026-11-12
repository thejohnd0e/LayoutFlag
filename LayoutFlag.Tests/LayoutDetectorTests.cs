using LayoutFlag;
using System;

public class LayoutDetectorTests
{
    [Theory]
    [InlineData(0x04190419u, FlagId.Ru)]
    [InlineData(0x04090409u, FlagId.En)]
    [InlineData(0x04070407u, FlagId.Unknown)]
    public void GetFlagForHkl_MapsLangId(uint hkl, FlagId expected)
        => Assert.Equal(expected, LayoutDetector.GetFlagForHkl((IntPtr)hkl));
}
