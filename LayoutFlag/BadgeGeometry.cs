using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LayoutFlag;

public static class BadgeGeometry
{
    public static Size GetPixelSize(BadgeShape shape, BadgeSize size)
    {
        if (shape == BadgeShape.Circle)
        {
            return size switch
            {
                BadgeSize.Small => new Size(28, 28),
                BadgeSize.Large => new Size(56, 56),
                _ => new Size(40, 40)
            };
        }

        return size switch
        {
            BadgeSize.Small => new Size(36, 24),
            BadgeSize.Large => new Size(80, 54),
            _ => new Size(56, 38)
        };
    }

    public static GraphicsPath CreateShapePath(BadgeShape shape, float width, float height, float inset = 0f)
    {
        var path = new GraphicsPath();
        float x = inset;
        float y = inset;
        float w = Math.Max(1f, width - inset * 2f);
        float h = Math.Max(1f, height - inset * 2f);

        switch (shape)
        {
            case BadgeShape.Circle:
                path.AddEllipse(x, y, w, h);
                break;

            case BadgeShape.Rectangle:
                path.AddRectangle(new RectangleF(x, y, w, h));
                break;

            default:
                float r = Math.Max(2f, Math.Min(w, h) * 0.22f);
                float d = r * 2f;
                path.AddArc(x, y, d, d, 180, 90);
                path.AddArc(x + w - d, y, d, d, 270, 90);
                path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
                path.AddArc(x, y + h - d, d, d, 90, 90);
                path.CloseFigure();
                break;
        }

        return path;
    }
}
