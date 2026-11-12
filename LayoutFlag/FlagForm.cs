using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LayoutFlag;

public class FlagForm : Form
{
    private readonly System.Windows.Forms.Timer _timer = new();
    private FlagId _current = FlagId.Unknown;
    private readonly AppSettings _settings;
    private bool _restoringPosition;
    private Bitmap? _badgeBitmap;
    private readonly ContextMenuStrip _menu;
    private readonly NotifyIcon _tray;
    private readonly Dictionary<FlagId, (Icon Icon, IntPtr Handle)> _trayIcons = new();

    private ToolStripMenuItem _modeFlagsItem = null!;
    private ToolStripMenuItem _modeColorItem = null!;
    private ToolStripMenuItem _shapeRoundedItem = null!;
    private ToolStripMenuItem _shapeRectItem = null!;
    private ToolStripMenuItem _shapeCircleItem = null!;
    private ToolStripMenuItem _sizeSmallItem = null!;
    private ToolStripMenuItem _sizeMediumItem = null!;
    private ToolStripMenuItem _sizeLargeItem = null!;
    private ToolStripMenuItem _autostartItem = null!;

    public FlagForm()
    {
        _settings = AppSettings.Load();

        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;

        // Сначала задаём размер по настройкам, чтобы DefaultOrSavedLocation верно посчитал угол
        Size = BadgeGeometry.GetPixelSize(_settings.Shape, _settings.BadgeSize);
        Location = DefaultOrSavedLocation();
        ApplyAppearance();

        _menu = BuildContextMenu();
        _menu.Opening += (s, e) => SyncMenuChecks();

        _timer.Interval = 100;
        _timer.Tick += (s, e) => PollLayout();
        _timer.Start();

        _tray = new NotifyIcon
        {
            Text = "LayoutFlag",
            Visible = true,
            ContextMenuStrip = _menu
        };
        RebuildTrayIcons();
        UpdateTrayIcon();
        _tray.DoubleClick += (s, e) => Restart();

        PollLayout();
    }

    private Point DefaultOrSavedLocation()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromPoint(Cursor.Position).WorkingArea;
        var loc = _settings.Location;
        if (loc is null || !IsOnScreen(loc.Value))
        {
            loc = new Point(area.Right - Width - 16, area.Bottom - Height - 16);
        }
        return loc.Value;
    }

    private bool IsOnScreen(Point loc) =>
        Screen.AllScreens.Any(s => s.Bounds.IntersectsWith(new Rectangle(loc, Size)));

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW — не крадём фокус, не в taskbar/Alt+Tab
            cp.ExStyle |= 0x8000000 | 0x80;
            return cp;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_badgeBitmap is not null)
        {
            e.Graphics.DrawImageUnscaled(_badgeBitmap, 0, 0);
        }
        base.OnPaint(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // drag без активации: стандартный move через non-client area
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, (IntPtr)NativeMethods.HTCAPTION, IntPtr.Zero);
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            SyncMenuChecks();
            _menu.Show(Cursor.Position);
        }
        base.OnMouseClick(e);
    }

    private void PollLayout()
    {
        IntPtr hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return; // нет активного окна — оставляем прежний флаг
        }

        uint tid = NativeMethods.GetWindowThreadProcessId(hwnd, out _);
        IntPtr hkl = NativeMethods.GetKeyboardLayout(tid);
        var flag = LayoutDetector.GetFlagForHkl(hkl);
        if (flag != _current)
        {
            _current = flag;
            RegenerateBadge();
            UpdateTrayIcon();
        }
    }

    private void ApplyAppearance()
    {
        _restoringPosition = true;
        try
        {
            Size = BadgeGeometry.GetPixelSize(_settings.Shape, _settings.BadgeSize);

            using var path = BadgeGeometry.CreateShapePath(_settings.Shape, Width, Height);
            var oldRegion = Region;
            Region = new Region(path);
            oldRegion?.Dispose();

            ClampLocation();
        }
        finally
        {
            _restoringPosition = false;
        }

        _settings.Location = Location;
        RegenerateBadge();
    }

    private void RegenerateBadge()
    {
        var next = BadgeRenderer.Render(_current, _settings.Mode, _settings.Shape, Width, Height);
        var prev = _badgeBitmap;
        _badgeBitmap = next;
        prev?.Dispose();
        Invalidate();
    }

    private void RebuildTrayIcons()
    {
        var old = _trayIcons.Values.ToList();
        _trayIcons.Clear();

        int iconSize = Math.Max(16, Math.Max(SystemInformation.SmallIconSize.Width, SystemInformation.SmallIconSize.Height));
        foreach (FlagId flag in Enum.GetValues<FlagId>())
        {
            using var bmp = BadgeRenderer.Render(flag, _settings.Mode, _settings.Shape, iconSize, iconSize, clipCorners: true);
            IntPtr hIcon = bmp.GetHicon();
            _trayIcons[flag] = (Icon.FromHandle(hIcon), hIcon);
        }

        UpdateTrayIcon();

        foreach (var (icon, handle) in old)
        {
            icon.Dispose();
            NativeMethods.DestroyIcon(handle);
        }
    }

    private void UpdateTrayIcon()
    {
        if (_trayIcons.TryGetValue(_current, out var entry))
        {
            _tray.Icon = entry.Icon;
        }
        _tray.Text = _current switch
        {
            FlagId.Ru => "LayoutFlag — RU",
            FlagId.En => "LayoutFlag — EN",
            _ => "LayoutFlag"
        };
    }

    private void Restart()
    {
        _settings.Save();
        Application.Restart();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var modeMenu = new ToolStripMenuItem("Вид");
        _modeFlagsItem = new ToolStripMenuItem("Флаги", null, (s, e) => SetMode(BadgeMode.Flags));
        _modeColorItem = new ToolStripMenuItem("Цвет", null, (s, e) => SetMode(BadgeMode.Color));
        modeMenu.DropDownItems.AddRange(new ToolStripItem[] { _modeFlagsItem, _modeColorItem });

        var shapeMenu = new ToolStripMenuItem("Форма");
        _shapeRoundedItem = new ToolStripMenuItem("Скруглённая", null, (s, e) => SetShape(BadgeShape.Rounded));
        _shapeRectItem = new ToolStripMenuItem("Прямоугольник", null, (s, e) => SetShape(BadgeShape.Rectangle));
        _shapeCircleItem = new ToolStripMenuItem("Круг", null, (s, e) => SetShape(BadgeShape.Circle));
        shapeMenu.DropDownItems.AddRange(new ToolStripItem[] { _shapeRoundedItem, _shapeRectItem, _shapeCircleItem });

        var sizeMenu = new ToolStripMenuItem("Размер");
        _sizeSmallItem = new ToolStripMenuItem("Маленький", null, (s, e) => SetSize(BadgeSize.Small));
        _sizeMediumItem = new ToolStripMenuItem("Средний", null, (s, e) => SetSize(BadgeSize.Medium));
        _sizeLargeItem = new ToolStripMenuItem("Крупный", null, (s, e) => SetSize(BadgeSize.Large));
        sizeMenu.DropDownItems.AddRange(new ToolStripItem[] { _sizeSmallItem, _sizeMediumItem, _sizeLargeItem });

        var restart = new ToolStripMenuItem("Перезапустить");
        restart.Click += (s, e) => Restart();

        _autostartItem = new ToolStripMenuItem("Автозапуск")
        {
            Checked = Shortcut.AutostartEnabled(),
            CheckOnClick = true
        };
        _autostartItem.Click += (s, e) => Shortcut.ToggleAutostart(_autostartItem.Checked);

        var exit = new ToolStripMenuItem("Выход");
        exit.Click += (s, e) => Close();

        menu.Items.AddRange(new ToolStripItem[]
        {
            modeMenu,
            shapeMenu,
            sizeMenu,
            new ToolStripSeparator(),
            restart,
            _autostartItem,
            new ToolStripSeparator(),
            exit
        });
        return menu;
    }

    private void SyncMenuChecks()
    {
        _modeFlagsItem.Checked = _settings.Mode == BadgeMode.Flags;
        _modeColorItem.Checked = _settings.Mode == BadgeMode.Color;

        _shapeRoundedItem.Checked = _settings.Shape == BadgeShape.Rounded;
        _shapeRectItem.Checked = _settings.Shape == BadgeShape.Rectangle;
        _shapeCircleItem.Checked = _settings.Shape == BadgeShape.Circle;

        _sizeSmallItem.Checked = _settings.BadgeSize == BadgeSize.Small;
        _sizeMediumItem.Checked = _settings.BadgeSize == BadgeSize.Medium;
        _sizeLargeItem.Checked = _settings.BadgeSize == BadgeSize.Large;

        _autostartItem.Checked = Shortcut.AutostartEnabled();
    }

    private void SetMode(BadgeMode mode)
    {
        _settings.Mode = mode;
        _settings.Save();
        RegenerateBadge();
        RebuildTrayIcons();
    }

    private void SetShape(BadgeShape shape)
    {
        _settings.Shape = shape;
        _settings.Save();
        ApplyAppearance();
        RebuildTrayIcons();
    }

    private void SetSize(BadgeSize size)
    {
        _settings.BadgeSize = size;
        _settings.Save();
        ApplyAppearance();
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
        _settings.Location = Location;
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

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _tray.Visible = false;
        _tray.Icon = null;
        _tray.Dispose();
        foreach (var (icon, handle) in _trayIcons.Values)
        {
            icon.Dispose();
            NativeMethods.DestroyIcon(handle);
        }
        _trayIcons.Clear();
        _menu.Dispose();
        _badgeBitmap?.Dispose();
        _settings.Save();
        base.OnFormClosing(e);
    }
}
