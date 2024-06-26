using Microsoft.Web.WebView2.WinForms;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _92CloudWallpaper
{
    public partial class Wallpaper : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOZORDER = 0x0004;

        private WebView2 webView;
        private bool isInitialized = false;

        public Wallpaper()
        {
            InitializeComponent();
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = false;
            this.ShowInTaskbar = false;
            SetAsDesktopWallpaper();

            // 延迟初始化 WebView2 控件，等待窗体完全加载并设置为父窗口之后
            await Task.Delay(1000);
            InitializeWebView();
        }

        private void InitializeWebView()
        {
            try
            {
                webView = new WebView2
                {
                    Dock = DockStyle.Fill,
                    Source = new Uri("https://www.bing.com")
                };
                Controls.Add(webView);
                Console.WriteLine("WebView2 control created");

                webView.CoreWebView2InitializationCompleted += (sender, e) =>
                {
                    try
                    {
                        if (!webView.IsDisposed && webView.CoreWebView2 != null)
                        {
                            Console.WriteLine("WebView2 Initialization completed");
                            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                            webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
                            webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                            webView.Reload();
                            isInitialized = true;
                        }
                        else
                        {
                            Console.WriteLine("WebView2 was disposed before initialization could complete");
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"Exception during WebView2 initialization: {ex.Message}");
                    }
                };

                // 初始化 WebView2 控件
                webView.EnsureCoreWebView2Async(null).ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        Console.WriteLine($"Exception during WebView2 EnsureCoreWebView2Async: {task.Exception.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during WebView2 control creation: {ex.Message}");
            }
        }

        private void SetAsDesktopWallpaper()
        {
            IntPtr progman = FindWindow("Progman", null);
            IntPtr workerw = IntPtr.Zero;

            Console.WriteLine($"Progman handle: {progman}");

            if (progman != IntPtr.Zero)
            {
                IntPtr result = IntPtr.Zero;
                SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out result);
                Console.WriteLine($"SendMessageTimeout result: {result}");

                EnumWindows((hwnd, lParam) =>
                {
                    IntPtr shellViewWin = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (shellViewWin != IntPtr.Zero)
                    {
                        workerw = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                        Console.WriteLine($"Found SHELLDLL_DefView, WorkerW handle: {workerw}");
                    }
                    return true;
                }, IntPtr.Zero);

                if (workerw == IntPtr.Zero)
                {
                    Console.WriteLine("Trying alternative method to find WorkerW");
                    EnumWindows((hwnd, lParam) =>
                    {
                        IntPtr found = FindWindowEx(hwnd, IntPtr.Zero, "WorkerW", null);
                        if (found != IntPtr.Zero)
                        {
                            workerw = found;
                            Console.WriteLine($"Alternative method found WorkerW handle: {workerw}");
                        }
                        return true;
                    }, IntPtr.Zero);
                }

                if (workerw != IntPtr.Zero)
                {
                    SetParent(Handle, workerw);
                    Console.WriteLine($"SetParent to WorkerW, handle: {Handle}");

                    int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
                    exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
                    SetWindowLong(Handle, GWL_EXSTYLE, exStyle);
                    Console.WriteLine($"Set window style to transparent and layered, exStyle: {exStyle}");

                    bool setResult = SetWindowPos(Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOZORDER);
                    Console.WriteLine($"SetWindowPos result: {setResult}");
                }
                else
                {
                    Console.WriteLine("WorkerW not found");
                }
            }
            else
            {
                Console.WriteLine("Progman not found");
            }
        }

        public void ChangeUrl(string url)
        {
            try
            {
                if (webView != null && !webView.IsDisposed && webView.CoreWebView2 != null && isInitialized)
                {
                    webView.Source = new Uri(url);
                    webView.Reload();
                }
                else
                {
                    Console.WriteLine("WebView2 not initialized or already disposed");
                    if (webView != null && !webView.IsDisposed)
                    {
                        webView.CoreWebView2InitializationCompleted += (sender, e) =>
                        {
                            try
                            {
                                if (!webView.IsDisposed && webView.CoreWebView2 != null && isInitialized)
                                {
                                    webView.Source = new Uri(url);
                                    webView.Reload();
                                }
                            }
                            catch (InvalidOperationException ex)
                            {
                                Console.WriteLine($"Exception during URL change in event handler: {ex.Message}");
                            }
                        };
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Exception during URL change: {ex.Message}");
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"NullReferenceException during URL change: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected exception during URL change: {ex.Message}");
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out IntPtr lpdwResult);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [Flags]
        private enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,
            SMTO_BLOCK = 0x1,
            SMTO_ABORTIFHUNG = 0x2,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8
        }

        public static void SetupWallpapersForAllScreens()
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                Wallpaper wallpaper = new Wallpaper
                {
                    StartPosition = FormStartPosition.Manual,
                    Location = screen.Bounds.Location,
                    Size = screen.Bounds.Size,
                    FormBorderStyle = FormBorderStyle.None,
                    WindowState = FormWindowState.Maximized,
                    ShowInTaskbar = false
                };
                wallpaper.Show();
            }
        }
    }
}
