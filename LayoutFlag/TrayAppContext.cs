using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LayoutFlag;

public class TrayAppContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly System.Windows.Forms.Timer _pollTimer = new();
    private readonly System.Windows.Forms.Timer _saveDebounceTimer = new();
    private readonly ContextMenuStrip _menu;
    private readonly NotifyIcon _tray;
    private readonly Dictionary<FlagId, (Icon Icon, IntPtr Handle)> _trayIcons = new();
    private readonly List<BadgeWindow> _windows = new();

    private FlagId _current = FlagId.Unknown;
    private bool _disposed;

    private ToolStripMenuItem _count1Item = null!;
    private ToolStripMenuItem _count2Item = null!;
    private ToolStripMenuItem _count3Item = null!;
    private ToolStripMenuItem _modeFlagsItem = null!;
    private ToolStripMenuItem _modeColorItem = null!;
    private ToolStripMenuItem _shapeRoundedItem = null!;
    private ToolStripMenuItem _shapeRectItem = null!;
    private ToolStripMenuItem _shapeCircleItem = null!;
    private ToolStripMenuItem _sizeSmallItem = null!;
    private ToolStripMenuItem _sizeMediumItem = null!;
    private ToolStripMenuItem _sizeLargeItem = null!;
    private ToolStripMenuItem _autostartItem = null!;

    public TrayAppContext()
    {
        _settings = AppSettings.Load();

        _menu = BuildContextMenu();
        _menu.Opening += (s, e) => SyncMenuChecks();

        _tray = new NotifyIcon
        {
            Text = "LayoutFlag",
            Visible = true,
            ContextMenuStrip = _menu
        };
        RebuildTrayIcons();
        _tray.DoubleClick += (s, e) => Restart();

        _saveDebounceTimer.Interval = 500;
        _saveDebounceTimer.Tick += (s, e) =>
        {
            _saveDebounceTimer.Stop();
            _settings.Save();
        };

        SyncWindowCount();
        PollLayout();

        _pollTimer.Interval = 100;
        _pollTimer.Tick += (s, e) => PollLayout();
        _pollTimer.Start();
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
            foreach (var win in _windows)
            {
                win.SetLayout(_current);
            }
            UpdateTrayIcon();
        }
    }

    private void SyncWindowCount()
    {
        int target = Math.Clamp(_settings.BadgeCount, 1, AppSettings.MaxBadges);
        var defaultSize = BadgeGeometry.GetPixelSize(_settings.Shape, _settings.BadgeSize);
        var workingAreas = Screen.AllScreens.Select(s => s.WorkingArea).ToArray();

        while (_windows.Count < target)
        {
            int idx = _windows.Count;
            var placedSoFar = _windows.Select(w => w.Location).ToList();

            Point loc;
            if (idx < _settings.Locations.Count && IsOnScreen(_settings.Locations[idx], defaultSize))
            {
                loc = _settings.Locations[idx];
            }
            else
            {
                loc = BadgePlacement.GetDefaultLocation(idx, defaultSize, workingAreas, placedSoFar);
                if (idx < _settings.Locations.Count)
                {
                    _settings.Locations[idx] = loc;
                }
                else
                {
                    _settings.Locations.Add(loc);
                }
            }

            var win = new BadgeWindow(idx, loc, _current, _settings.Mode, _settings.Shape, _settings.BadgeSize);
            win.Moved += OnWindowMoved;
            win.ContextMenuRequested += (w, pt) =>
            {
                SyncMenuChecks();
                _menu.Show(pt);
            };
            _windows.Add(win);
            win.Show();
        }

        while (_windows.Count > target)
        {
            int last = _windows.Count - 1;
            var win = _windows[last];
            _windows.RemoveAt(last);
            win.Moved -= OnWindowMoved;
            win.Close();
            win.Dispose();
        }
    }

    private static bool IsOnScreen(Point loc, Size size) =>
        Screen.AllScreens.Any(s => s.Bounds.IntersectsWith(new Rectangle(loc, size)));

    private void OnWindowMoved(BadgeWindow win)
    {
        while (_settings.Locations.Count <= win.Index)
        {
            _settings.Locations.Add(win.Location);
        }
        _settings.Locations[win.Index] = win.Location;

        // Отложенное сохранение, чтобы не дёргать диск на каждый пиксель перетаскивания
        _saveDebounceTimer.Stop();
        _saveDebounceTimer.Start();
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

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var countMenu = new ToolStripMenuItem("Плашек");
        _count1Item = new ToolStripMenuItem("1", null, (s, e) => SetBadgeCount(1));
        _count2Item = new ToolStripMenuItem("2", null, (s, e) => SetBadgeCount(2));
        _count3Item = new ToolStripMenuItem("3", null, (s, e) => SetBadgeCount(3));
        countMenu.DropDownItems.AddRange(new ToolStripItem[] { _count1Item, _count2Item, _count3Item });

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
        exit.Click += (s, e) => ExitApp();

        menu.Items.AddRange(new ToolStripItem[]
        {
            countMenu,
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
        _count1Item.Checked = _settings.BadgeCount == 1;
        _count2Item.Checked = _settings.BadgeCount == 2;
        _count3Item.Checked = _settings.BadgeCount == 3;

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

    private void SetBadgeCount(int count)
    {
        _settings.BadgeCount = Math.Clamp(count, 1, AppSettings.MaxBadges);
        SyncWindowCount();
        _settings.Save();
    }

    private void SetMode(BadgeMode mode)
    {
        _settings.Mode = mode;
        _settings.Save();
        ApplyAppearanceToAll();
        RebuildTrayIcons();
    }

    private void SetShape(BadgeShape shape)
    {
        _settings.Shape = shape;
        _settings.Save();
        ApplyAppearanceToAll();
        RebuildTrayIcons();
    }

    private void SetSize(BadgeSize size)
    {
        _settings.BadgeSize = size;
        _settings.Save();
        ApplyAppearanceToAll();
    }

    private void ApplyAppearanceToAll()
    {
        foreach (var win in _windows)
        {
            win.SetAppearance(_settings.Mode, _settings.Shape, _settings.BadgeSize);
        }
    }

    private void Restart()
    {
        CleanupBeforeExit();
        Application.Restart();
    }

    private void ExitApp()
    {
        CleanupBeforeExit();
        ExitThread();
    }

    private void CleanupBeforeExit()
    {
        if (_disposed) return;
        _disposed = true;

        _pollTimer.Stop();
        _pollTimer.Dispose();
        _saveDebounceTimer.Stop();
        _saveDebounceTimer.Dispose();

        for (int i = 0; i < _windows.Count; i++)
        {
            if (i < _settings.Locations.Count)
                _settings.Locations[i] = _windows[i].Location;
            else
                _settings.Locations.Add(_windows[i].Location);
        }
        _settings.Save();

        foreach (var win in _windows)
        {
            win.Moved -= OnWindowMoved;
            win.Close();
            win.Dispose();
        }
        _windows.Clear();

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
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CleanupBeforeExit();
        }
        base.Dispose(disposing);
    }
}
