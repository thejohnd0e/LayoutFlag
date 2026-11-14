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
        // Младшее слово HKL — LANGID; его младшие 10 бит — основной язык (без региона),
        // чтобы en-GB, en-CA и т.п. тоже считались английскими
        int primaryLang = (int)((long)hkl & 0x3FF);
        return primaryLang switch
        {
            0x19 => FlagId.Ru, // LANG_RUSSIAN
            0x09 => FlagId.En, // LANG_ENGLISH
            _ => FlagId.Unknown
        };
    }
}
