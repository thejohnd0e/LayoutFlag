namespace LayoutFlag;

/// <summary>Сведения об окне, находящемся выше нашего в z-порядке.</summary>
public readonly record struct ZOrderEntry(bool IsVisible, bool IsTopMost);

/// <summary>
/// Windows удерживает окно «поверх всех» не флагом WS_EX_TOPMOST, а отдельной полосой z-порядка.
/// Перетаскивание (системный цикл перемещения по WM_NCLBUTTONDOWN/HTCAPTION) вставляет окно над
/// окном под курсором — то есть в обычную полосу, и флажок остаётся за окнами до перезапуска.
/// Политика отвечает на вопрос, нужно ли вернуть окно в topmost-полосу.
/// </summary>
public static class TopMostPolicy
{
    /// <summary>Сколько окон выше просматривать, прежде чем считать состояние нормальным.</summary>
    public const int MaxScanDepth = 32;

    /// <summary>
    /// true, если выше окна есть видимое окно, не являющееся topmost: значит, наше окно
    /// оказалось вне topmost-полосы. Первое видимое topmost-окно (панель задач, чужое
    /// always-on-top) означает, что мы в полосе, — дальше смотреть не нужно.
    /// </summary>
    public static bool IsBuried(IEnumerable<ZOrderEntry> windowsAbove)
    {
        foreach (var window in windowsAbove)
        {
            if (!window.IsVisible) continue;
            if (window.IsTopMost) return false;
            return true;
        }

        return false;
    }
}
