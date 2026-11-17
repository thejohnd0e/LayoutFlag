using LayoutFlag;
using System.Collections.Generic;

public class TopMostPolicyTests
{
    private static ZOrderEntry Visible(bool topMost) => new(IsVisible: true, IsTopMost: topMost);
    private static ZOrderEntry Hidden(bool topMost) => new(IsVisible: false, IsTopMost: topMost);

    [Fact]
    public void IsBuried_NoWindowsAbove_ReturnsFalse()
    {
        Assert.False(TopMostPolicy.IsBuried(new List<ZOrderEntry>()));
    }

    [Fact]
    public void IsBuried_OnlyHiddenWindowsAbove_ReturnsFalse()
    {
        var above = new List<ZOrderEntry> { Hidden(topMost: false), Hidden(topMost: false) };
        Assert.False(TopMostPolicy.IsBuried(above));
    }

    [Fact]
    public void IsBuried_VisibleTopMostWindowAbove_ReturnsFalse()
    {
        // Первое видимое окно — topmost (панель задач, чужое always-on-top): мы в полосе topmost.
        var above = new List<ZOrderEntry> { Hidden(false), Visible(topMost: true), Visible(topMost: false) };
        Assert.False(TopMostPolicy.IsBuried(above));
    }

    [Fact]
    public void IsBuried_VisibleNormalWindowAbove_ReturnsTrue()
    {
        // Так выглядит флажок после перетаскивания на обычное окно.
        var above = new List<ZOrderEntry> { Hidden(true), Visible(topMost: false) };
        Assert.True(TopMostPolicy.IsBuried(above));
    }

    [Fact]
    public void IsBuried_IgnoresEverythingBelowTheFirstVisibleTopMostWindow()
    {
        var above = new List<ZOrderEntry> { Visible(true), Visible(false), Visible(false) };
        Assert.False(TopMostPolicy.IsBuried(above));
    }
}
