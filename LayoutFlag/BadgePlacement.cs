using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace LayoutFlag;

public static class BadgePlacement
{
    public static Point GetDefaultLocation(
        int index,
        Size badgeSize,
        IReadOnlyList<Rectangle> workingAreas,
        IReadOnlyList<Point> existingLocations)
    {
        if (workingAreas.Count == 0)
        {
            return new Point(100 + index * (badgeSize.Width + 12), 100);
        }

        // Ищем монитор, на котором ещё нет плашки
        foreach (var area in workingAreas)
        {
            bool occupied = existingLocations.Any(pt => area.IntersectsWith(new Rectangle(pt, badgeSize)));
            if (!occupied)
            {
                return BottomRight(area, badgeSize);
            }
        }

        // Все мониторы уже заняты — ставим со смещением влево от правого нижнего угла
        var target = workingAreas[index % workingAreas.Count];
        var basePt = BottomRight(target, badgeSize);
        int x = Math.Max(target.Left + 16, basePt.X - index * (badgeSize.Width + 12));
        return new Point(x, basePt.Y);
    }

    private static Point BottomRight(Rectangle area, Size badgeSize) =>
        new(
            Math.Max(area.Left, area.Right - badgeSize.Width - 16),
            Math.Max(area.Top, area.Bottom - badgeSize.Height - 16));
}
