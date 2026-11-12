using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LayoutFlag;

public static class BadgeRenderer
{
    public static Bitmap Render(FlagId flag, BadgeMode mode, BadgeShape shape, int width, int height)
    {
        const int scale = 4;
        int sw = width * scale;
        int sh = height * scale;

        using var hiRes = new Bitmap(sw, sh);
        using (var g = Graphics.FromImage(hiRes))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            if (mode == BadgeMode.Color)
            {
                DrawSolidColor(g, flag, sw, sh);
            }
            else
            {
                DrawFlag(g, flag, sw, sh);
            }

            // Тонкая окантовка по краю формы, чтобы плашка читалась на любом фоне (и светлом, и тёмном)
            using var borderPath = BadgeGeometry.CreateShapePath(shape, sw, sh, inset: scale * 0.6f);
            using var borderPen = new Pen(Color.FromArgb(95, 0, 0, 0), scale * 1.2f);
            g.DrawPath(borderPen, borderPath);
        }

        var output = new Bitmap(width, height);
        using (var gOut = Graphics.FromImage(output))
        {
            gOut.InterpolationMode = InterpolationMode.HighQualityBicubic;
            gOut.PixelOffsetMode = PixelOffsetMode.HighQuality;
            gOut.DrawImage(hiRes, 0, 0, width, height);
        }

        return output;
    }

    private static void DrawSolidColor(Graphics g, FlagId flag, int w, int h)
    {
        var (topColor, bottomColor) = flag switch
        {
            FlagId.En => (Color.FromArgb(59, 130, 246), Color.FromArgb(29, 78, 216)),  // синий #2563EB
            FlagId.Ru => (Color.FromArgb(239, 68, 68), Color.FromArgb(185, 28, 28)),   // красный #DC2626
            _ => (Color.FromArgb(120, 126, 138), Color.FromArgb(88, 94, 105))          // серый
        };

        using var brush = new LinearGradientBrush(new Rectangle(0, 0, w, h), topColor, bottomColor, 90f);
        g.FillRectangle(brush, 0, 0, w, h);
    }

    private static void DrawFlag(Graphics g, FlagId flag, int w, int h)
    {
        switch (flag)
        {
            case FlagId.Ru:
                DrawRussiaFlag(g, w, h);
                break;
            case FlagId.En:
                DrawUsaFlag(g, w, h);
                break;
            default:
                DrawUnknownGlobe(g, w, h);
                break;
        }
    }

    private static void DrawRussiaFlag(Graphics g, int w, int h)
    {
        float third = h / 3f;
        g.Clear(Color.White);
        using var blue = new SolidBrush(Color.FromArgb(0, 51, 160));
        using var red = new SolidBrush(Color.FromArgb(213, 43, 30));
        g.FillRectangle(blue, 0, third, w, third + 1f);
        g.FillRectangle(red, 0, 2f * third, w, h - 2f * third);
    }

    private static void DrawUsaFlag(Graphics g, int w, int h)
    {
        g.Clear(Color.White);
        float stripe = h / 13f;
        using var red = new SolidBrush(Color.FromArgb(179, 25, 27));
        for (int i = 0; i < 13; i += 2)
        {
            g.FillRectangle(red, 0, i * stripe, w, stripe + 0.6f);
        }

        float cantonW = w * 0.44f;
        float cantonH = 7f * stripe;
        using var navy = new SolidBrush(Color.FromArgb(10, 49, 82));
        g.FillRectangle(navy, 0, 0, cantonW, cantonH);

        using var starBrush = new SolidBrush(Color.White);
        float cellW = cantonW / 12f;
        float cellH = cantonH / 10f;
        float starR = Math.Max(1.6f, Math.Min(cellW, cellH) * 0.65f);

        for (int row = 0; row < 9; row++)
        {
            int cols = row % 2 == 0 ? 6 : 5;
            float offsetX = row % 2 == 0 ? 1f : 2f;
            float cy = cellH * (1f + row);
            for (int col = 0; col < cols; col++)
            {
                float cx = cellW * (offsetX + 2f * col);
                var pts = new PointF[10];
                for (int k = 0; k < 10; k++)
                {
                    double ang = Math.PI * (-90 + 36 * k) / 180.0;
                    float rad = k % 2 == 0 ? starR : starR * 0.42f;
                    pts[k] = new PointF(
                        (float)(cx + rad * Math.Cos(ang)),
                        (float)(cy + rad * Math.Sin(ang)));
                }
                g.FillPolygon(starBrush, pts);
            }
        }
    }

    private static void DrawUnknownGlobe(Graphics g, int w, int h)
    {
        using var bg = new LinearGradientBrush(
            new Rectangle(0, 0, w, h),
            Color.FromArgb(74, 76, 86),
            Color.FromArgb(48, 50, 58), 90f);
        g.FillRectangle(bg, 0, 0, w, h);

        float r = Math.Min(w, h) * 0.24f;
        float cx = w / 2f;
        float cy = h / 2f;
        using var pen = new Pen(Color.FromArgb(165, 175, 185), Math.Max(2f, r * 0.14f));
        g.DrawEllipse(pen, cx - r, cy - r, r * 2f, r * 2f);
        g.DrawEllipse(pen, cx - r * 0.42f, cy - r, r * 0.84f, r * 2f);
        g.DrawLine(pen, cx - r, cy, cx + r, cy);
    }
}
