using LayoutFlag;

public class LocalizationTests
{
    [Theory]
    [InlineData(UiLanguage.Ru, "en", true)]
    [InlineData(UiLanguage.En, "ru", false)]
    [InlineData(UiLanguage.Auto, "ru", true)]
    [InlineData(UiLanguage.Auto, "RU", true)]
    [InlineData(UiLanguage.Auto, "en", false)]
    [InlineData(UiLanguage.Auto, "de", false)]
    public void IsRussian_ResolvesExplicitAndAuto(UiLanguage lang, string systemIso, bool expected)
    {
        Assert.Equal(expected, Strings.IsRussian(lang, systemIso));
    }

    [Fact]
    public void For_RussianAndEnglish_ProvideDistinctNonEmptyStrings()
    {
        var ru = Strings.For(isRussian: true);
        var en = Strings.For(isRussian: false);

        Assert.Equal("Плашек", ru.Badges);
        Assert.Equal("Badges", en.Badges);
        Assert.Equal("Выход", ru.Exit);
        Assert.Equal("Exit", en.Exit);
        Assert.NotEqual(ru.Style, en.Style);
        Assert.NotEqual(ru.Shape, en.Shape);
        Assert.NotEqual(ru.Size, en.Size);
        Assert.NotEqual(ru.Autostart, en.Autostart);
    }
}
