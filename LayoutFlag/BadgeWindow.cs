using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LayoutFlag;

public class BadgeWindow : Form
{
    private FlagId _flag;
    private BadgeMode _mode;
    private BadgeShape _shape;
    private BadgeSize _badgeSize;
    private bool _restoringPosition;

    public int Index { get; }

    public event Action<BadgeWindow>? Moved;
    public event Action<BadgeWindow, Point>? ContextMenuRequested;

    public BadgeWindow(
        int index,
        Point initialLocation,
        FlagId initialFlag,
        BadgeMode mode,
        BadgeShape shape,
        BadgeSize badgeSize)
    {
        Index = index;
        _flag = initialFlag;
        _mode = mode;
        _shape = shape;
        _badgeSize = badgeSize;

        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        Size = GetScaledBadgeSize();
        Location = initialLocation;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_LAYERED
            cp.ExStyle |= 0x8000000 | 0x80 | 0x80000;
            return cp;
        }
    }

    public void SetLayout(FlagId flag)
    {
        if (_flag == flag) return;
        _flag = flag;
        RenderToLayeredWindow();
    }

    public void SetAppearance(BadgeMode mode, BadgeShape shape, BadgeSize badgeSize)
    {
        _mode = mode;
        _shape = shape;
        _badgeSize = badgeSize;
        ApplyAppearance();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyAppearance();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        ApplyAppearance();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, (IntPtr)NativeMethods.HTCAPTION, IntPtr.Zero);
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            ContextMenuRequested?.Invoke(this, Cursor.Position);
        }
        base.OnMouseClick(e);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        // Системный цикл перемещения (WM_NCLBUTTONDOWN + HTCAPTION) при отпускании вставляет окно
        // над окном под курсором — флажок уходит из topmost-полосы и остаётся за окнами.
        if (m.Msg == NativeMethods.WM_EXITSIZEMOVE)
        {
            EnsureTopMost();
        }
    }

    /// <summary>Возвращает окно в topmost-полосу, не трогая размер, позицию и активацию.</summary>
    public void EnsureTopMost()
    {
        if (!IsHandleCreated) return;

        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HWND_TOPMOST,
            0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOOWNERZORDER);
    }

    /// <summary>
    /// Страховка по таймеру: поднимает окно только если выше него оказалось видимое обычное окно,
    /// чтобы не лезть поверх меню Пуск, Alt-Tab и чужих always-on-top окон без нужды.
    /// </summary>
    public void KeepOnTop()
    {
        if (!IsHandleCreated || !IsBuried()) return;
        EnsureTopMost();
    }

    private bool IsBuried() => TopMostPolicy.IsBuried(EnumerateWindowsAbove());

    private List<ZOrderEntry> EnumerateWindowsAbove()
    {
        var windows = new List<ZOrderEntry>(TopMostPolicy.MaxScanDepth);
        IntPtr current = NativeMethods.GetWindow(Handle, NativeMethods.GW_HWNDPREV);

        while (current != IntPtr.Zero && windows.Count < TopMostPolicy.MaxScanDepth)
        {
            bool visible = NativeMethods.IsWindowVisible(current);
            bool topMost =
                (NativeMethods.GetWindowLongPtr(current, NativeMethods.GWL_EXSTYLE).ToInt64() &
                 NativeMethods.WS_EX_TOPMOST) != 0;
            windows.Add(new ZOrderEntry(visible, topMost));

            // Первое видимое topmost-окно уже ответило на вопрос — глубже смотреть незачем
            if (visible && topMost) break;

            current = NativeMethods.GetWindow(current, NativeMethods.GW_HWNDPREV);
        }

        return windows;
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        if (_restoringPosition) return;
        _restoringPosition = true;
        try
        {
            ClampLocation();
        }
        finally
        {
            _restoringPosition = false;
        }
        Moved?.Invoke(this);
    }

    private Size GetScaledBadgeSize()
    {
        var logical = BadgeGeometry.GetPixelSize(_shape, _badgeSize);
        float dpiScale = DeviceDpi / 96f;
        return new Size(
            Math.Max(1, (int)Math.Round(logical.Width * dpiScale)),
            Math.Max(1, (int)Math.Round(logical.Height * dpiScale)));
    }

    private void ApplyAppearance()
    {
        _restoringPosition = true;
        try
        {
            Size = GetScaledBadgeSize();
            ClampLocation();
        }
        finally
        {
            _restoringPosition = false;
        }
        RenderToLayeredWindow();
        EnsureTopMost();
    }

    private void ClampLocation()
    {
        var loc = Location;
        var area = Screen.FromPoint(loc).Bounds;
        int marginX = Math.Min(Width, 20);
        int marginY = Math.Min(Height, 20);
        int x = Math.Clamp(loc.X, area.Left - Width + marginX, area.Right - marginX);
        int y = Math.Clamp(loc.Y, area.Top - Height + marginY, area.Bottom - marginY);
        if (x != loc.X || y != loc.Y)
        {
            Location = new Point(x, y);
        }
    }

    private void RenderToLayeredWindow()
    {
        if (!IsHandleCreated) return;

        using var bmp = BadgeRenderer.Render(_flag, _mode, _shape, Width, Height, clipCorners: true);
        IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
        IntPtr memDc = NativeMethods.CreateCompatibleDC(screenDc);
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;

        try
        {
            hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
            oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

            var size = new NativeMethods.SIZE(bmp.Width, bmp.Height);
            var pointSource = new NativeMethods.POINT(0, 0);
            var topPos = new NativeMethods.POINT(Left, Top);
            var blend = new NativeMethods.BLENDFUNCTION
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };

            NativeMethods.UpdateLayeredWindow(
                Handle, screenDc, ref topPos, ref size,
                memDc, ref pointSource, 0, ref blend, NativeMethods.ULW_ALPHA);
        }
        finally
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
            if (hBitmap != IntPtr.Zero)
            {
                NativeMethods.SelectObject(memDc, oldBitmap);
                NativeMethods.DeleteObject(hBitmap);
            }
            NativeMethods.DeleteDC(memDc);
        }
    }
}
