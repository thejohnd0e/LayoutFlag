using System;

namespace LayoutFlag;

public enum FlagId
{
    Ru,
    En,
    Unknown
}

public static class LayoutDetector
{
    public static FlagId GetFlagForHkl(IntPtr hkl)
    {
        ushort langId = (ushort)((long)hkl & 0xFFFF);
        return langId switch
        {
            0x0419 => FlagId.Ru,
            0x0409 => FlagId.En,
            _ => FlagId.Unknown
        };
    }
}
