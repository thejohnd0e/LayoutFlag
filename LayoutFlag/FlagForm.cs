using System;
using System.Drawing;
using System.Windows.Forms;

namespace LayoutFlag;

public class FlagForm : Form
{
    private readonly PictureBox _box = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly Image _flagRu;
    private readonly Image _flagEn;
    private readonly Image _flagUnknown;
    private FlagId _current = FlagId.Unknown;
    private readonly AppSettings _settings;
    private bool _restoringPosition;
    private ContextMenuStrip? _menu;
    private readonly NotifyIcon _tray;

    public FlagForm()
    {
        _settings = AppSettings.Load();

        _flagRu = LoadFlag("russia");
        _flagEn = LoadFlag("usa");
        _flagUnknown = LoadFlag("unknown");

        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(56, 42);
        BackColor = Color.FromArgb(30, 30, 30);
        Location = DefaultOrSavedLocation();

        _box.Dock = DockStyle.Fill;
        _box.SizeMode = PictureBoxSizeMode.Zoom;
        _box.Margin = new Padding(3);
        _box.Image = _flagUnknown;
        Controls.Add(_box);

        // PictureBox перекрывает всю форму — мышиные события приходят ему, а не форме
        _box.MouseDown += (s, e) => BeginDrag(e.Button);
        _box.MouseClick += (s, e) => { if (e.Button == MouseButtons.Right) ShowContextMenu(); };

        _timer.Interval = 100;
        _timer.Tick += (s, e) => PollLayout();
        _timer.Start();

        _tray = new NotifyIcon
        {
            Icon = MakeTrayIcon(),
            Text = "LayoutFlag — индикатор раскладки",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };
        _tray.DoubleClick += (s, e) => Restart();
    }

    private static Icon MakeTrayIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(30, 30, 30));
        int third = 16 / 3;
        using (var brush = new SolidBrush(Color.RoyalBlue)) g.FillRectangle(brush, 0, third, 16, third);
        using (var brush = new SolidBrush(Color.Red)) g.FillRectangle(brush, 0, 2 * third, 16, 16 - 2 * third);
        return Icon.FromHandle(bmp.GetHicon());
    }

    private void Restart()
    {
        _settings.Save();
        Application.Restart(); // закрывает форму и запускает новый экземпляр
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
        Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(new Rectangle(loc, Size)));

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

    private void BeginDrag(MouseButtons button)
    {
        if (button != MouseButtons.Left)
        {
            return;
        }
        // drag без активации: стандартный move через non-client area
        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, (IntPtr)NativeMethods.HTCAPTION, IntPtr.Zero);
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
            _box.Image = flag switch
            {
                FlagId.Ru => _flagRu,
                FlagId.En => _flagEn,
                _ => _flagUnknown
            };
        }
    }

    private void ShowContextMenu()
    {
        _menu ??= BuildContextMenu();
        // синхронизируем галочку с реальным наличием ярлыка
        ((ToolStripMenuItem)_menu.Items[0]!).Checked = Shortcut.AutostartEnabled();
        _menu.Show(Cursor.Position);
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        var restart = new ToolStripMenuItem("Перезапустить");
        restart.Click += (s, e) => Restart();
        var auto = new ToolStripMenuItem("Автозапуск")
        {
            Checked = Shortcut.AutostartEnabled(),
            CheckOnClick = true
        };
        auto.Click += (s, e) => Shortcut.ToggleAutostart(auto.Checked);
        var exit = new ToolStripMenuItem("Выход");
        exit.Click += (s, e) => Close();
        menu.Items.Add(restart);
        menu.Items.Add(auto);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exit);
        return menu;
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        if (_restoringPosition) return;
        // клэмп в видимую область: плашку нельзя утащить за экран
        var loc = Location;
        var area = Screen.FromPoint(loc).WorkingArea;
        int x = Math.Clamp(loc.X, area.Left - Width + 40, area.Right - 40);
        int y = Math.Clamp(loc.Y, area.Top - Height + 20, area.Bottom - 20);
        if (x != loc.X || y != loc.Y)
        {
            _restoringPosition = true;
            Location = new Point(x, y);
            _restoringPosition = false;
        }
        _settings.Location = Location;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _tray.Visible = false;
        _tray.Dispose();
        _settings.Save();
        base.OnFormClosing(e);
    }

    private static Image LoadFlag(string name)
    {
        var asm = typeof(FlagForm).Assembly;
        using var stream = asm.GetManifestResourceStream($"LayoutFlag.Flags.{name}.png")
            ?? throw new InvalidOperationException($"Embedded resource {name}.png not found");
        return new Bitmap(stream);
    }
}
