using LayoutFlag;
using System;

public class LayoutDetectorTests
{
    [Theory]
    [InlineData(0x04190419u, FlagId.Ru)]
    [InlineData(0x08190819u, FlagId.Ru)]      // ru-MD
    [InlineData(0x04090409u, FlagId.En)]      // en-US
    [InlineData(0x08090809u, FlagId.En)]      // en-GB
    [InlineData(0x10090409u, FlagId.En)]      // en-CA, раскладка US
    [InlineData(0xF0020409u, FlagId.En)]      // US-International (старшее слово — device handle)
    [InlineData(0x04070407u, FlagId.Unknown)] // de-DE
    [InlineData(0x04220422u, FlagId.Unknown)] // uk-UA
    public void GetFlagForHkl_MapsLangId(uint hkl, FlagId expected)
        => Assert.Equal(expected, LayoutDetector.GetFlagForHkl((IntPtr)(long)hkl));
}
