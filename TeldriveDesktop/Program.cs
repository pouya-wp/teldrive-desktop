using Microsoft.Web.WebView2.WinForms;
using TeldriveDesktop.Services;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace TeldriveDesktop;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var settings = AppSettings.Load();
        var backend = new BackendManager(settings);
        using var context = new DesktopContext(settings, backend, args.Contains("--autostart", StringComparer.OrdinalIgnoreCase));
        Application.Run(context);
    }
}

internal sealed class DesktopContext : ApplicationContext
{
    private readonly MainForm _form;
    private readonly NotifyIcon _tray;
    private readonly BackendManager _backend;
    private readonly AppSettings _settings;

    public DesktopContext(AppSettings settings, BackendManager backend, bool autostart)
    {
        _settings = settings; _backend = backend;
        _form = new MainForm(backend, settings);
        _tray = new NotifyIcon { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application, Text = "Teldrive Desktop", Visible = true, ContextMenuStrip = MakeMenu() };
        _tray.DoubleClick += (_, _) => ShowWindow();
        _form.FormClosing += OnFormClosing;
        if (!autostart) _form.Show();
        _ = backend.StartAsync();
    }
    private ContextMenuStrip MakeMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Teldrive", null, (_, _) => ShowWindow());
        menu.Items.Add("Restart Teldrive", null, async (_, _) => await _backend.RestartAsync());
        menu.Items.Add("Open Web UI in Browser", null, (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(BackendManager.ServerUrl) { UseShellExecute = true }));
        menu.Items.Add("Open Config Folder", null, (_, _) => System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(_backend.ConfigPath)!));
        var startup = new ToolStripMenuItem("Start with Windows") { Checked = _settings.StartWithWindows, CheckOnClick = true };
        startup.CheckedChanged += (_, _) => { _settings.StartWithWindows = startup.Checked; _settings.Save(); AutoStart.Set(startup.Checked); };
        menu.Items.Add(startup);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => ExitThread());
        return menu;
    }
    private void ShowWindow() { _form.ShowWithAnimation(); }
    private void OnFormClosing(object? sender, FormClosingEventArgs e) { if (_settings.CloseToTray) { e.Cancel = true; _form.HideWithAnimation(); } }
    protected override void ExitThreadCore() { _tray.Visible = false; _backend.Stop(); _backend.Dispose(); _form.FormClosing -= OnFormClosing; _form.Close(); base.ExitThreadCore(); }
}

internal sealed class MainForm : Form
{
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    [DllImport("user32.dll")] private static extern bool AnimateWindow(IntPtr hWnd, int time, int flags);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    private readonly BackendManager _backend;
    private readonly AppSettings _settings;
    private readonly Panel _startup = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(250, 250, 251) };
    private readonly Label _status = new() { AutoSize = true, Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(75, 75, 80) };
    private readonly Label _title = new() { AutoSize = true, Text = "Teldrive Desktop", Font = new Font("Segoe UI Semibold", 22) };
    private readonly Button _retry = new() { Text = "Retry", AutoSize = true, Visible = false };
    private readonly Panel _titleBar = new() { Dock = DockStyle.Top, Height = 42, BackColor = Color.FromArgb(246, 247, 249) };
    private WebView2? _web;
    public MainForm(BackendManager backend, AppSettings settings)
    {
        _backend = backend; _settings = settings;
        Text = "Teldrive Desktop"; FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.CenterScreen; MinimumSize = new Size(760, 560); Size = new Size(1240, 820); BackColor = _startup.BackColor; DoubleBuffered = true;
        Resize += (_, _) => ApplyRoundedRegion(); Shown += (_, _) => { ApplyRoundedRegion(); var pref = 2; DwmSetWindowAttribute(Handle, 33, ref pref, sizeof(int)); AnimateWindow(Handle, 160, 0x00080000 | 0x00020000); };
        BuildTitleBar();
        var content = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(44), Anchor = AnchorStyles.None };
        var mark = new Label { Text = "T", TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, BackColor = Color.FromArgb(35, 35, 38), Font = new Font("Segoe UI Semibold", 20), Size = new Size(52, 52), Margin = new Padding(0, 0, 0, 18) };
        content.Controls.Add(mark); content.Controls.Add(_title); content.Controls.Add(new Label { Text = "", Height = 12 }); content.Controls.Add(_status); content.Controls.Add(new Label { Text = "The existing Teldrive service is being started in the background.", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(0, 10, 0, 16) });
        _retry.Click += async (_, _) => await _backend.RestartAsync(); content.Controls.Add(_retry);
        _startup.Controls.Add(content); _startup.Resize += (_, _) => content.Location = new Point((_startup.ClientSize.Width - content.Width) / 2, (_startup.ClientSize.Height - content.Height) / 2);
        Controls.Add(_startup); Controls.Add(_titleBar); _titleBar.BringToFront(); _backend.StatusChanged += OnStatusChanged;
    }
    private void BuildTitleBar()
    {
        _titleBar.MouseDown += DragWindow;
        var caption = new Label { Text = "Teldrive Desktop", AutoSize = true, Font = new Font("Segoe UI Semibold", 9), ForeColor = Color.FromArgb(74, 74, 78), Location = new Point(86, 13) };
        caption.MouseDown += DragWindow;
        _titleBar.Controls.Add(caption);
        AddTrafficButton(Color.FromArgb(255, 95, 86), 16, (_, _) => HideWithAnimation());
        AddTrafficButton(Color.FromArgb(255, 189, 46), 40, (_, _) => WindowState = FormWindowState.Minimized);
        AddTrafficButton(Color.FromArgb(39, 201, 63), 64, (_, _) => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized);
    }
    private void AddTrafficButton(Color color, int x, EventHandler action)
    {
        var button = new TrafficLightButton(color) { Location = new Point(x, 13), Size = new Size(14, 14), Cursor = Cursors.Hand, TabStop = false };
        button.Click += action; _titleBar.Controls.Add(button);
    }
    private void ApplyRoundedRegion() { if (WindowState == FormWindowState.Maximized) { Region = null; return; } using var path = new GraphicsPath(); path.AddArc(0, 0, 32, 32, 180, 90); path.AddArc(Width - 32, 0, 32, 32, 270, 90); path.AddArc(Width - 32, Height - 32, 32, 32, 0, 90); path.AddArc(0, Height - 32, 32, 32, 90, 90); path.CloseFigure(); Region = new Region(path); }
    public void ShowWithAnimation() { if (!Visible) { Show(); AnimateWindow(Handle, 160, 0x00080000 | 0x00020000); } WindowState = FormWindowState.Normal; Activate(); }
    public void HideWithAnimation() { AnimateWindow(Handle, 130, 0x00080000 | 0x00010000); Hide(); }
    private void DragWindow(object? sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 2, 0); } }
    private void OnStatusChanged(object? sender, BackendStatus status)
    {
        if (IsDisposed) return; BeginInvoke(() =>
        {
            _status.Text = status.Detail; _retry.Visible = status.State is BackendState.Failed or BackendState.Unhealthy;
            if (status.State == BackendState.Running) ShowTeldrive();
        });
    }
    private async void ShowTeldrive()
    {
        if (_web is not null) return;
        _web = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.White };
        Controls.Add(_web); _web.BringToFront(); _titleBar.BringToFront();
        try { await _web.EnsureCoreWebView2Async(); _web.CoreWebView2.Navigate(BackendManager.ServerUrl); }
        catch { Controls.Remove(_web); _web.Dispose(); _web = null; _status.Text = "WebView2 Runtime is unavailable. Install Microsoft Edge WebView2 Runtime."; _retry.Visible = true; }
    }
}

internal sealed class TrafficLightButton(Color color) : Control
{
    private bool _hover;
    protected override void OnCreateControl() { base.OnCreateControl(); using var p = new GraphicsPath(); p.AddEllipse(ClientRectangle); Region = new Region(p); }
    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnPaint(PaintEventArgs e) { using var brush = new SolidBrush(color); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.FillEllipse(brush, 0, 0, Width - 1, Height - 1); if (_hover) { using var pen = new Pen(Color.FromArgb(110, 30, 30, 30), 1.3f); e.Graphics.DrawLine(pen, 4, 4, Width - 4, Height - 4); e.Graphics.DrawLine(pen, Width - 4, 4, 4, Height - 4); } }
}
