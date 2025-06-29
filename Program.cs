using System.Diagnostics;
using System.Drawing.Imaging;
using Microsoft.Win32;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace XenScreener
{
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

    class FlashOverlayForm : Form
    {
        public FlashOverlayForm(Rectangle bounds)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            BackColor = Color.Black;
            Opacity = 0.5;
            ShowInTaskbar = false;
            TopMost = true;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }
    }
    
    class Program : ApplicationContext
    {
        private static Program? instance;
        [DllImport("user32.dll")] static extern bool GetCursorPos(out Point lpPoint);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(Point pt, uint dwFlags);
        [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
        [DllImport("user32.dll")] static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")] static extern bool EmptyClipboard();
        [DllImport("user32.dll")] static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);
        [DllImport("user32.dll")] static extern bool CloseClipboard();
        
        [DllImport("Shcore.dll")]
        static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
        
        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(int dpiFlag);

        private const int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

        const int MDT_EFFECTIVE_DPI = 0;

        private const uint SRCCOPY = 0x00CC0020;
        private const uint CF_BITMAP = 2;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc _proc = null!;

        private static IntPtr hookId = IntPtr.Zero;
        private static HashSet<Keys> pressedKeys = new();

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
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        const string RUN_REGISTRY_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        string appName = Assembly.GetExecutingAssembly().GetName().Name ?? "XenScreener";
        string appPath = Application.ExecutablePath;

        NotifyIcon trayIcon = null!;
        ToolStripMenuItem autoStartMenuItem = null!;
        ToolStripMenuItem notifyMenuItem = null!;
        ToolStripMenuItem monitorsMenu = null!;

        private void ShowFlashEffect(Rectangle bounds)
        {
            var flash = new FlashOverlayForm(bounds);
            flash.Show();
            Task.Delay(120).ContinueWith(_ =>
            {
                try { flash.Invoke(flash.Close); }
                catch { }
            });
        }
        
        public Program()
        {
            instance = this;
            
            string localPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? "";

            Config = new XmlConfig(Path.Combine(localPath, "Config.xml"));

            bNotify = Config.Notify;

            using Stream? xenIcon = Assembly.GetExecutingAssembly().GetManifestResourceStream("XenScreener.Resources.favicon.ico");
            using Stream? closeIcon = Assembly.GetExecutingAssembly().GetManifestResourceStream("XenScreener.Resources.close.ico");
            if (xenIcon != null && closeIcon != null)
            {
                var iconXen = new Icon(xenIcon);
                var iconClose = new Icon(closeIcon);

                trayIcon = new NotifyIcon()
                {
                    Icon = iconXen,
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
                
                trayIcon.ContextMenuStrip.Items.Add("About", SystemIcons.Information.ToBitmap(), (_, __) => About());
                
                // Separator
                trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());

                // Exit
                trayIcon.ContextMenuStrip.Items.Add("Exit", iconClose.ToBitmap(), (_, __) => ExitThread());
            }
            
            SetHook();
        }

        private void About()
        {
            using (var about = new About())
            {
                about.ShowDialog();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
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
            using var key = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
                .OpenSubKey(RUN_REGISTRY_KEY, true);
            if (key == null)
                return;

            if (enabled)
                key.SetValue(appName, $"\"{appPath}\"");
            else
                key.DeleteValue(appName, false);

            Config.AutoStart = enabled;
            Config.Save();
        }

        private bool bWasPNG = false; 
        private void XenScreenshot()
        {
            try
            {
                GetCursorPos(out Point cursor);
                IntPtr monitor = MonitorFromPoint(cursor, 2);

                MONITORINFO mi = new MONITORINFO();
                mi.cbSize = Marshal.SizeOf(mi);
                GetMonitorInfo(monitor, ref mi);
                
                uint dpiX, dpiY;
                int ret = GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
                if (ret != 0)
                    dpiX = 96; dpiY = 96;
                
                float scaleX = dpiX / 96f;
                float scaleY = dpiY / 96f;

                int width = mi.rcMonitor.Right - mi.rcMonitor.Left;
                int height = mi.rcMonitor.Bottom - mi.rcMonitor.Top;
                int left = (int)(mi.rcMonitor.Left * scaleX);
                int top = (int)(mi.rcMonitor.Top * scaleY);

                Rectangle bounds = new Rectangle(left, top, width, height);
                
                IntPtr hdcScreen = IntPtr.Zero;
                IntPtr hdcMem = IntPtr.Zero;
                IntPtr hBitmap = IntPtr.Zero;
                IntPtr hOld = IntPtr.Zero;
                
                Exception threadEx = null!;

                Thread thread = new Thread(() =>
                {
                    try
                    {
                        hdcScreen = GetDC(IntPtr.Zero);
                        hdcMem = CreateCompatibleDC(hdcScreen);
                        hBitmap = CreateCompatibleBitmap(hdcScreen, bounds.Width, bounds.Height);
                        hOld = SelectObject(hdcMem, hBitmap);

                        if (!BitBlt(hdcMem, 0, 0, bounds.Width, bounds.Height, hdcScreen, bounds.Left, bounds.Top, SRCCOPY))
                            throw new System.ComponentModel.Win32Exception();

                        OpenClipboard(IntPtr.Zero);

                        try
                        {
                            EmptyClipboard();
                            SetClipboardData(CF_BITMAP, hBitmap);
                            
                            hBitmap = IntPtr.Zero;
                        }
                        finally
                        {
                            CloseClipboard();
                        }
                    }
                    catch (Exception ex)
                    {
                        threadEx = ex;
                    }
                    finally
                    {
                        if (hOld != IntPtr.Zero)
                            SelectObject(hdcMem, hOld);

                        if (hBitmap != IntPtr.Zero)
                            DeleteObject(hBitmap);

                        if (hdcMem != IntPtr.Zero)
                            DeleteDC(hdcMem);

                        if (hdcScreen != IntPtr.Zero)
                            ReleaseDC(IntPtr.Zero, hdcScreen);
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                if (threadEx != null)
                    throw threadEx;
                
                if (bWasPNG)
                {
                    using Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);
                    using (Graphics gpu = Graphics.FromImage(bmp))
                        gpu.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    
                    string userPictures = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                        "Screenshots"
                    );
                    Directory.CreateDirectory(userPictures);

                    string fileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_XFF.png";
                    string fullPath = Path.Combine(userPictures, fileName);

                    bmp.Save(fullPath, ImageFormat.Png);
                    ShowFlashEffect(bounds);
                }

                if (bNotify)
                {
                    bool wasPngCopy = bWasPNG;
                    Task.Delay(200).ContinueWith(_ =>
                    {
                        try { ShowNotify("Screenshot", wasPngCopy ? "Screenshot was copied and saved" : "Screenshot was copied to clipboard"); }
                        catch { /* Ignored */ }
                    });
                }

                bWasPNG = false;
            }
            catch (Exception ex)
            {
                ShowNotify("Error", ex.Message);
            }
        }
        
        private bool _handledPress;
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = (Keys)vkCode;

                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    pressedKeys.Add(key);

                    // Alt + PrtScr skip
                    if (pressedKeys.Contains(Keys.Menu) || 
                        pressedKeys.Contains(Keys.LMenu) || 
                        pressedKeys.Contains(Keys.RMenu))
                    {
                        return CallNextHookEx(hookId, nCode, wParam, lParam);
                    }
                    
                    if (key == Keys.PrintScreen)
                    {
                        if (!_handledPress)
                        {
                            _handledPress = true;
                            
                            bWasPNG = pressedKeys.Contains(Keys.ControlKey) || pressedKeys.Contains(Keys.LControlKey) || pressedKeys.Contains(Keys.RControlKey);
                            XenScreenshot();
                            
                            return (IntPtr)1;
                        }
                        return (IntPtr)1;
                    }

                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    pressedKeys.Remove(key);

                    if (key == Keys.PrintScreen)
                    {
                        _handledPress = false;
                    }
                }
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }
        
        private static IntPtr StaticHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (instance != null)
                return instance.HookCallback(nCode, wParam, lParam);

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        public static void SetHook()
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            _proc = StaticHookCallback;
            if (curModule?.ModuleName != null)
                hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        public static void Unhook()
        {
            UnhookWindowsHookEx(hookId);
        }

        private void ShowNotify(string title, string message)
        {
            trayIcon.BalloonTipTitle = title;
            trayIcon.BalloonTipText = message;
            trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            
            trayIcon.ShowBalloonTip(1000);
        }

        protected override void ExitThreadCore()
        {
            Unhook();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            base.ExitThreadCore();
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

            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Program());
        }
    }
}
