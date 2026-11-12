using System;
using System.Globalization;

namespace LayoutFlag;

public sealed record UiStrings(
    string Badges,
    string Style,
    string Flags,
    string Color,
    string Shape,
    string Rounded,
    string Rectangle,
    string Circle,
    string Size,
    string Small,
    string Medium,
    string Large,
    string Language,
    string LangAuto,
    string LangRu,
    string LangEn,
    string Restart,
    string Autostart,
    string Exit);

public static class Strings
{
    private static readonly UiStrings Russian = new(
        Badges: "Плашек",
        Style: "Вид",
        Flags: "Флаги",
        Color: "Цвет",
        Shape: "Форма",
        Rounded: "Скруглённая",
        Rectangle: "Прямоугольник",
        Circle: "Круг",
        Size: "Размер",
        Small: "Маленький",
        Medium: "Средний",
        Large: "Крупный",
        Language: "Язык",
        LangAuto: "Авто (система)",
        LangRu: "Русский",
        LangEn: "English",
        Restart: "Перезапустить",
        Autostart: "Автозапуск",
        Exit: "Выход");

    private static readonly UiStrings English = new(
        Badges: "Badges",
        Style: "Style",
        Flags: "Flags",
        Color: "Color",
        Shape: "Shape",
        Rounded: "Rounded",
        Rectangle: "Rectangle",
        Circle: "Circle",
        Size: "Size",
        Small: "Small",
        Medium: "Medium",
        Large: "Large",
        Language: "Language",
        LangAuto: "Auto (system)",
        LangRu: "Русский",
        LangEn: "English",
        Restart: "Restart",
        Autostart: "Run at startup",
        Exit: "Exit");

    public static bool IsRussian(UiLanguage lang, string systemTwoLetterIso) => lang switch
    {
        UiLanguage.Ru => true,
        UiLanguage.En => false,
        _ => systemTwoLetterIso.Equals("ru", StringComparison.OrdinalIgnoreCase)
    };

    public static UiStrings For(UiLanguage lang) =>
        For(IsRussian(lang, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

    public static UiStrings For(bool isRussian) => isRussian ? Russian : English;
}
