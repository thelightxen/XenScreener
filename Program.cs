using System.Diagnostics;
using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace XenScreener;

class XmlConfig
{
    string iniPath;

    public bool Notify { get; set; } = true;
    public bool AutoStart { get; set; } = false;

    public XmlConfig(string path)
    {
        iniPath = path;
        Load();
    }

    public void Load()
    {
        if (!File.Exists(iniPath))
        {
            Save();
            return;
        }

        try
        {
            var doc = XDocument.Load(iniPath);
            var root = doc.Root;

            if (root == null)
                return;

            if (bool.TryParse(root.Element("Notify")?.Value, out bool notifyVal))
                Notify = notifyVal;

            if (bool.TryParse(root.Element("AutoStart")?.Value, out bool autoStartVal))
                AutoStart = autoStartVal;
        }
        catch
        {
            Notify = true;
        }
    }

    public void Save()
    {
        var doc = new XDocument(
            new XElement("Config",
                new XElement("Notify", Notify.ToString()),
                new XElement("AutoStart", AutoStart.ToString())
            )
        );
        doc.Save(iniPath);
    }
}

class Program : ApplicationContext
{
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out Point lpPoint);
    [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(Point pt, uint dwFlags);
    [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    public XmlConfig Config;
    public bool bNotify = true;
    const int HOTKEY_ID = 9000;
    const uint MOD_NONE = 0x0000;
    const uint VK_SNAPSHOT = 0x2C; // PrtScr
    
    const string RUN_REGISTRY_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
    string appName = Assembly.GetExecutingAssembly().GetName().Name ?? "XenScreener";
    string appPath = Application.ExecutablePath;

    NotifyIcon trayIcon = null!;
    ToolStripMenuItem autoStartMenuItem = null!;
    ToolStripMenuItem notifyMenuItem = null!;
    ToolStripMenuItem monitorsMenu = null!;
    
    public Program()
    {
        Config = new XmlConfig("Config.xml");
        
        bNotify = Config.Notify;
        
        using Stream? xenIcon = Assembly.GetExecutingAssembly().GetManifestResourceStream("XenScreener.Resources.favicon.ico");
        using Stream? closeIcon = Assembly.GetExecutingAssembly().GetManifestResourceStream("XenScreener.Resources.close.ico");
        if (xenIcon != null && closeIcon != null)
        {
            trayIcon = new NotifyIcon()
            {
                Icon = new Icon(xenIcon),
                Visible = true,
                Text = appName,
            };

            trayIcon.ContextMenuStrip = new ContextMenuStrip();
            
            // Monitors
            monitorsMenu = new ToolStripMenuItem("Monitors");
            trayIcon.ContextMenuStrip.Items.Add(monitorsMenu);
            trayIcon.ContextMenuStrip.Opening += OnMonitorsOpen;

            // Autostart checkbox
            autoStartMenuItem = new ToolStripMenuItem("Autostart");
            autoStartMenuItem.CheckOnClick = true;
            autoStartMenuItem.Checked = IsAutoStartEnabled();
            autoStartMenuItem.Click += (s, e) => ToggleAutoStart(autoStartMenuItem.Checked);
            trayIcon.ContextMenuStrip.Items.Add(autoStartMenuItem);
            
            // Show notifications
            notifyMenuItem = new ToolStripMenuItem("Notify");
            notifyMenuItem.ToolTipText = "Show notification after screenshot";
            notifyMenuItem.Checked = bNotify;
            notifyMenuItem.CheckOnClick = true;
            notifyMenuItem.Click += (s, e) =>
            {
                bNotify = notifyMenuItem.Checked;
                Config.Notify = bNotify;
                Config.Save();
            };
            trayIcon.ContextMenuStrip.Items.Add(notifyMenuItem);

            // Separator
            trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());

            // Exit
            trayIcon.ContextMenuStrip.Items.Add("Exit", new Icon(closeIcon).ToBitmap(), (_, __) => ExitThread());
        }

        RegisterHotKey(IntPtr.Zero, HOTKEY_ID, MOD_NONE, VK_SNAPSHOT);

        Application.AddMessageFilter(new HotKeyMessageFilter(HandleHotkey));
    }
    
    private void OnMonitorsOpen(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        monitorsMenu.DropDownItems.Clear();

        var monitors = Screen.AllScreens;
        for (int i = 0; i < monitors.Length; i++)
        {
            var s = monitors[i];
            string itemText = $"Monitor {i + 1}: {s.Bounds.Width}x{s.Bounds.Height} {(s.Primary ? "(Primary)" : "")}";
            var monitorItem = new ToolStripMenuItem(itemText) { Enabled = false };
            monitorsMenu.DropDownItems.Add(monitorItem);
        }
    }

    bool IsAutoStartEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_KEY, false);
        if (key == null)
            return false;

        var value = key.GetValue(appName) as string;
        return value != null && value == $"\"{appPath}\"";
    }

    void ToggleAutoStart(bool enabled)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_KEY, true);
        if (key == null)
            return;

        if (enabled)
            key.SetValue(appName, $"\"{appPath}\"");
        else
            key.DeleteValue(appName, false);
        
        Config.AutoStart = enabled;
        Config.Save();
    }
    
    private void HandleHotkey()
    {
        try
        {
            GetCursorPos(out Point cursor);
            IntPtr monitor = MonitorFromPoint(cursor, 2);

            MONITORINFO mi = new MONITORINFO();
            mi.cbSize = Marshal.SizeOf(mi);
            GetMonitorInfo(monitor, ref mi);

            Rectangle bounds = new Rectangle(
                mi.rcMonitor.Left,
                mi.rcMonitor.Top,
                mi.rcMonitor.Right - mi.rcMonitor.Left,
                mi.rcMonitor.Bottom - mi.rcMonitor.Top
            );

            using Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);
            using Graphics gpu = Graphics.FromImage(bmp);
            gpu.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            Clipboard.SetImage(bmp);

            if (bNotify)
                ShowNotify("Hello World!", "Screenshot was copied to clipboard");
        }
        catch (Exception ex)
        {
            ShowNotify("Error", ex.Message);
        }
    }

    private void ShowNotify(string title, string message)
    {
        trayIcon.BalloonTipTitle = title;
        trayIcon.BalloonTipText = message;
        trayIcon.ShowBalloonTip(1000);
    }

    protected override void ExitThreadCore()
    {
        UnregisterHotKey(IntPtr.Zero, HOTKEY_ID);
        trayIcon.Visible = false;
        trayIcon.Dispose();
        base.ExitThreadCore();
    }
    
    class HotKeyMessageFilter : IMessageFilter
    {
        private readonly Action callback;
        public HotKeyMessageFilter(Action callback) => this.callback = callback;

        public bool PreFilterMessage(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                callback();
                return true;
            }
            return false;
        }
    }
    
    [STAThread]
    static void Main()
    {
        var currentProcess = Process.GetCurrentProcess();
        var processes = Process.GetProcessesByName(currentProcess.ProcessName);
        if (processes.Length > 1)
        {
            return;
        }
        
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Program());
    }
}