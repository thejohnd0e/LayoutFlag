using LayoutFlag;
using System.Collections.Generic;
using System.Drawing;

public class BadgePlacementTests
{
    private static readonly Size Badge = new(56, 38);

    [Fact]
    public void GetDefaultLocation_FirstBadge_PlacedBottomRightOfPrimaryMonitor()
    {
        var screens = new[] { new Rectangle(0, 0, 1920, 1040) };
        var pt = BadgePlacement.GetDefaultLocation(0, Badge, screens, new List<Point>());
        Assert.Equal(new Point(1920 - 56 - 16, 1040 - 38 - 16), pt);
    }

    [Fact]
    public void GetDefaultLocation_TwoMonitors_PlacesSecondBadgeOnEmptyMonitor()
    {
        var screens = new[]
        {
            new Rectangle(0, 0, 1920, 1040),
            new Rectangle(1920, 0, 1920, 1040)
        };
        var existing = new List<Point> { new(100, 100) }; // на первом мониторе

        var pt = BadgePlacement.GetDefaultLocation(1, Badge, screens, existing);

        Assert.Equal(new Point(3840 - 56 - 16, 1040 - 38 - 16), pt);
    }

    [Fact]
    public void GetDefaultLocation_TwoMonitorsFirstOnSecondMonitor_PlacesNewBadgeOnFirstMonitor()
    {
        var screens = new[]
        {
            new Rectangle(0, 0, 1920, 1040),
            new Rectangle(1920, 0, 1920, 1040)
        };
        var existing = new List<Point> { new(2100, 300) }; // перетащена на второй монитор

        var pt = BadgePlacement.GetDefaultLocation(1, Badge, screens, existing);

        Assert.Equal(new Point(1920 - 56 - 16, 1040 - 38 - 16), pt);
    }

    [Fact]
    public void GetDefaultLocation_SingleMonitorMultipleBadges_OffsetsToAvoidOverlap()
    {
        var screens = new[] { new Rectangle(0, 0, 1920, 1040) };
        var first = BadgePlacement.GetDefaultLocation(0, Badge, screens, new List<Point>());
        var second = BadgePlacement.GetDefaultLocation(1, Badge, screens, new List<Point> { first });

        Assert.NotEqual(first, second);
        Assert.True(screens[0].Contains(new Rectangle(second, Badge)));
    }
}
