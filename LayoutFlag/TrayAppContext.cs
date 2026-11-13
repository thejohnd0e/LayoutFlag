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

    private ToolStripMenuItem _countMenu = null!;
    private ToolStripMenuItem _count1Item = null!;
    private ToolStripMenuItem _count2Item = null!;
    private ToolStripMenuItem _count3Item = null!;
    private ToolStripMenuItem _count4Item = null!;
    private ToolStripMenuItem _modeMenu = null!;
    private ToolStripMenuItem _modeFlagsItem = null!;
    private ToolStripMenuItem _modeColorItem = null!;
    private ToolStripMenuItem _shapeMenu = null!;
    private ToolStripMenuItem _shapeRoundedItem = null!;
    private ToolStripMenuItem _shapeRectItem = null!;
    private ToolStripMenuItem _shapeCircleItem = null!;
    private ToolStripMenuItem _sizeMenu = null!;
    private ToolStripMenuItem _sizeSmallItem = null!;
    private ToolStripMenuItem _sizeMediumItem = null!;
    private ToolStripMenuItem _sizeLargeItem = null!;
    private ToolStripMenuItem _langMenu = null!;
    private ToolStripMenuItem _langAutoItem = null!;
    private ToolStripMenuItem _langRuItem = null!;
    private ToolStripMenuItem _langEnItem = null!;
    private ToolStripMenuItem _restartItem = null!;
    private ToolStripMenuItem _autostartItem = null!;
    private ToolStripMenuItem _aboutItem = null!;
    private ToolStripMenuItem _exitItem = null!;

    public TrayAppContext(UiLanguage? initialLanguageHint = null)
    {
        bool isFirstRun = !AppSettings.DefaultFileExists();
        _settings = AppSettings.Load();
        if (isFirstRun && initialLanguageHint is not null)
        {
            _settings.Language = initialLanguageHint.Value;
            _settings.Save();
        }

        _menu = BuildContextMenu();
        ApplyMenuLanguage();
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

        _countMenu = new ToolStripMenuItem();
        _count1Item = new ToolStripMenuItem("1", null, (s, e) => SetBadgeCount(1));
        _count2Item = new ToolStripMenuItem("2", null, (s, e) => SetBadgeCount(2));
        _count3Item = new ToolStripMenuItem("3", null, (s, e) => SetBadgeCount(3));
        _count4Item = new ToolStripMenuItem("4", null, (s, e) => SetBadgeCount(4));
        _countMenu.DropDownItems.AddRange(new ToolStripItem[] { _count1Item, _count2Item, _count3Item, _count4Item });

        _modeMenu = new ToolStripMenuItem();
        _modeFlagsItem = new ToolStripMenuItem(string.Empty, null, (s, e) => SetMode(BadgeMode.Flags));
        _modeColorItem = new ToolStripMenuItem(string.Empty, null, (s, e) => SetMode(BadgeMode.Color));
        _modeMenu.DropDownItems.AddRange(new ToolStripItem[] { _modeFlagsItem, _modeColorItem });

        _shapeMenu = new ToolStripMenuItem();
        _shapeRoundedItem = new ToolStripMenuItem(string.Empty, null, (s, e) => SetShape(BadgeShape.Rounded));
        _shapeRectItem = new ToolStripMenuItem(string.Empty, null, (s, e) => SetShape(BadgeShape.Rectangle));
        _shapeCircleItem = new ToolStripMenuItem(string.Empty, null, (s, e) => SetShape(BadgeShape.Circle));
        _shapeMenu.DropDownItems.AddRange(new ToolStripItem[] { _shapeRoundedItem, _shapeRectItem, _shapeCircleItem });

        _sizeMenu = new ToolStripMenuItem();
        _sizeSmallItem = new ToolStripMenuItem(string.Empty, null, (s, e) => SetSize(BadgeSize.Small));
        _sizeMediumItem = new ToolStripMenuItem(string.Empty, null, (s, e) => SetSize(BadgeSize.Medium));
        _sizeLargeItem = new ToolStripMenuItem(string.Empty, null, (s, e) => SetSize(BadgeSize.Large));
        _sizeMenu.DropDownItems.AddRange(new ToolStripItem[] { _sizeSmallItem, _sizeMediumItem, _sizeLargeItem });

        _langMenu = new ToolStripMenuItem();
        _langAutoItem = new ToolStripMenuItem(string.Empty, null, (s, e) => SetLanguage(UiLanguage.Auto));
        _langRuItem = new ToolStripMenuItem(string.Empty, null, (s, e) => SetLanguage(UiLanguage.Ru));
        _langEnItem = new ToolStripMenuItem(string.Empty, null, (s, e) => SetLanguage(UiLanguage.En));
        _langMenu.DropDownItems.AddRange(new ToolStripItem[] { _langAutoItem, _langRuItem, _langEnItem });

        _restartItem = new ToolStripMenuItem();
        _restartItem.Click += (s, e) => Restart();

        _autostartItem = new ToolStripMenuItem
        {
            Checked = Shortcut.AutostartEnabled(),
            CheckOnClick = true
        };
        _autostartItem.Click += (s, e) => Shortcut.ToggleAutostart(_autostartItem.Checked);

        _exitItem = new ToolStripMenuItem();
        _exitItem.Click += (s, e) => ExitApp();

        _aboutItem = new ToolStripMenuItem();
        _aboutItem.Click += (s, e) => ShowAboutDialog();

        menu.Items.AddRange(new ToolStripItem[]
        {
            _countMenu,
            _modeMenu,
            _shapeMenu,
            _sizeMenu,
            _langMenu,
            new ToolStripSeparator(),
            _restartItem,
            _autostartItem,
            _aboutItem,
            new ToolStripSeparator(),
            _exitItem
        });
        return menu;
    }

    private void ApplyMenuLanguage()
    {
        var t = Strings.For(_settings.Language);
        _countMenu.Text = t.Badges;
        _modeMenu.Text = t.Style;
        _modeFlagsItem.Text = t.Flags;
        _modeColorItem.Text = t.Color;
        _shapeMenu.Text = t.Shape;
        _shapeRoundedItem.Text = t.Rounded;
        _shapeRectItem.Text = t.Rectangle;
        _shapeCircleItem.Text = t.Circle;
        _sizeMenu.Text = t.Size;
        _sizeSmallItem.Text = t.Small;
        _sizeMediumItem.Text = t.Medium;
        _sizeLargeItem.Text = t.Large;
        _langMenu.Text = t.Language;
        _langAutoItem.Text = t.LangAuto;
        _langRuItem.Text = t.LangRu;
        _langEnItem.Text = t.LangEn;
        _restartItem.Text = t.Restart;
        _autostartItem.Text = t.Autostart;
        _aboutItem.Text = t.About;
        _exitItem.Text = t.Exit;
    }

    private void SyncMenuChecks()
    {
        _count1Item.Checked = _settings.BadgeCount == 1;
        _count2Item.Checked = _settings.BadgeCount == 2;
        _count3Item.Checked = _settings.BadgeCount == 3;
        _count4Item.Checked = _settings.BadgeCount == 4;

        _modeFlagsItem.Checked = _settings.Mode == BadgeMode.Flags;
        _modeColorItem.Checked = _settings.Mode == BadgeMode.Color;

        _shapeRoundedItem.Checked = _settings.Shape == BadgeShape.Rounded;
        _shapeRectItem.Checked = _settings.Shape == BadgeShape.Rectangle;
        _shapeCircleItem.Checked = _settings.Shape == BadgeShape.Circle;

        _sizeSmallItem.Checked = _settings.BadgeSize == BadgeSize.Small;
        _sizeMediumItem.Checked = _settings.BadgeSize == BadgeSize.Medium;
        _sizeLargeItem.Checked = _settings.BadgeSize == BadgeSize.Large;

        _langAutoItem.Checked = _settings.Language == UiLanguage.Auto;
        _langRuItem.Checked = _settings.Language == UiLanguage.Ru;
        _langEnItem.Checked = _settings.Language == UiLanguage.En;

        _autostartItem.Checked = Shortcut.AutostartEnabled();
    }

    private void SetLanguage(UiLanguage language)
    {
        _settings.Language = language;
        _settings.Save();
        ApplyMenuLanguage();
    }

    private void ShowAboutDialog()
    {
        var t = Strings.For(_settings.Language);
        var result = MessageBox.Show(
            t.AboutText,
            t.AboutTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);

        if (result == DialogResult.Yes)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Strings.RepositoryUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                // молча игнорируем, если браузер не смог запуститься
            }
        }
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
