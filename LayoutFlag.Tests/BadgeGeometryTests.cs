using LayoutFlag;
using System.Drawing;

public class BadgeGeometryTests
{
    [Theory]
    [InlineData(BadgeShape.Rounded, BadgeSize.Small, 36, 24)]
    [InlineData(BadgeShape.Rounded, BadgeSize.Medium, 56, 38)]
    [InlineData(BadgeShape.Rounded, BadgeSize.Large, 80, 54)]
    [InlineData(BadgeShape.Rectangle, BadgeSize.Small, 36, 24)]
    [InlineData(BadgeShape.Rectangle, BadgeSize.Medium, 56, 38)]
    [InlineData(BadgeShape.Rectangle, BadgeSize.Large, 80, 54)]
    [InlineData(BadgeShape.Circle, BadgeSize.Small, 28, 28)]
    [InlineData(BadgeShape.Circle, BadgeSize.Medium, 40, 40)]
    [InlineData(BadgeShape.Circle, BadgeSize.Large, 56, 56)]
    public void GetPixelSize_ReturnsExpectedDimensions(BadgeShape shape, BadgeSize size, int w, int h)
    {
        Assert.Equal(new Size(w, h), BadgeGeometry.GetPixelSize(shape, size));
    }

    [Theory]
    [InlineData(BadgeShape.Rounded)]
    [InlineData(BadgeShape.Rectangle)]
    [InlineData(BadgeShape.Circle)]
    public void CreateShapePath_FitsWithinBounds(BadgeShape shape)
    {
        using var path = BadgeGeometry.CreateShapePath(shape, 56, 38);
        var bounds = path.GetBounds();
        Assert.True(bounds.Width > 0 && bounds.Height > 0);
        Assert.True(bounds.Right <= 56.5f && bounds.Bottom <= 38.5f);
    }

    [Fact]
    public void Render_AllCombinations_ProducesBitmapOfExactSize()
    {
        foreach (FlagId flag in Enum.GetValues<FlagId>())
        foreach (BadgeMode mode in Enum.GetValues<BadgeMode>())
        foreach (BadgeShape shape in Enum.GetValues<BadgeShape>())
        foreach (BadgeSize size in Enum.GetValues<BadgeSize>())
        {
            var px = BadgeGeometry.GetPixelSize(shape, size);
            using var bmp = BadgeRenderer.Render(flag, mode, shape, px.Width, px.Height);
            Assert.Equal(px.Width, bmp.Width);
            Assert.Equal(px.Height, bmp.Height);
        }
    }

    [Theory]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public void Render_TrayIconWithClippedCorners_HasTransparentCorner(int size)
    {
        using var bmp = BadgeRenderer.Render(FlagId.Ru, BadgeMode.Flags, BadgeShape.Rounded, size, size, clipCorners: true);
        Assert.Equal(size, bmp.Width);
        Assert.Equal(size, bmp.Height);
        Assert.Equal(0, bmp.GetPixel(0, 0).A);
    }
}
