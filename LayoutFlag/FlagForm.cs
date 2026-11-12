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

    public FlagForm()
    {
        _flagRu = LoadFlag("russia");
        _flagEn = LoadFlag("usa");
        _flagUnknown = LoadFlag("unknown");

        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(56, 42);
        BackColor = Color.FromArgb(30, 30, 30);

        _box.Dock = DockStyle.Fill;
        _box.SizeMode = PictureBoxSizeMode.Zoom;
        _box.Margin = new Padding(3);
        _box.Image = _flagUnknown;
        Controls.Add(_box);

        _timer.Interval = 100;
        _timer.Tick += (s, e) => PollLayout();
        _timer.Start();
    }

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

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            ShowContextMenu();
        }
        base.OnMouseClick(e);
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
        var menu = new ContextMenuStrip();
        var exit = new ToolStripMenuItem("Выход");
        exit.Click += (s, e) => Close();
        menu.Items.Add(exit);
        menu.Show(Cursor.Position);
    }

    private static Image LoadFlag(string name)
    {
        var asm = typeof(FlagForm).Assembly;
        using var stream = asm.GetManifestResourceStream($"LayoutFlag.Flags.{name}.png")
            ?? throw new InvalidOperationException($"Embedded resource {name}.png not found");
        return new Bitmap(stream);
    }
}
