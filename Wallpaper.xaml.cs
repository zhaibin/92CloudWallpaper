using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace _92CloudWallpaper
{
    public partial class Wallpaper : Window
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
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOZORDER = 0x0004;

        private bool isInitialized = false;

        public Wallpaper()
        {
            InitializeComponent();
            Loaded += Wallpaper_Loaded;
        }

        private void Wallpaper_Loaded(object sender, RoutedEventArgs e)
        {
            //SetAsDesktopWallpaper();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                webView.CoreWebView2InitializationCompleted += (sender, e) =>
                {
                    try
                    {
                        if (webView.CoreWebView2 != null)
                        {
                            Console.WriteLine("WebView2 Initialization completed");
                            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                            webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
                            webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;

                            // 禁用滚动条
                            webView.CoreWebView2.ExecuteScriptAsync("document.body.style.overflow ='hidden'");

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

                await webView.EnsureCoreWebView2Async(null);
                Console.WriteLine("WebView2 control created");
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
                    SetParent(new WindowInteropHelper(this).Handle, workerw);
                    Console.WriteLine($"SetParent to WorkerW, handle: {new WindowInteropHelper(this).Handle}");

                    int exStyle = GetWindowLong(new WindowInteropHelper(this).Handle, GWL_EXSTYLE);
                    exStyle &= ~WS_EX_TRANSPARENT; // 移除透明样式以允许点击
                    SetWindowLong(new WindowInteropHelper(this).Handle, GWL_EXSTYLE, exStyle);
                    Console.WriteLine($"Set window style to accept mouse events, exStyle: {exStyle}");

                    bool setResult = SetWindowPos(new WindowInteropHelper(this).Handle, HWND_BOTTOM, 0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOZORDER);
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
                if (webView != null && webView.CoreWebView2 != null && isInitialized)
                {
                    webView.Source = new Uri(url);
                }
                else
                {
                    Console.WriteLine("WebView2 not initialized or already disposed");
                    if (webView != null)
                    {
                        webView.CoreWebView2InitializationCompleted += (sender, e) =>
                        {
                            try
                            {
                                if (webView.CoreWebView2 != null && isInitialized)
                                {
                                    webView.Source = new Uri(url);
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

        public class MouseEventHandler
        {
            public void OnMouseClick()
            {
                Console.WriteLine("Mouse click detected on WebView2");
            }
        }

        public static void InitializeAllWallpapers()
        {
            IntPtr workerw = IntPtr.Zero;
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                var wallpaperWindow = new Wallpaper();
                wallpaperWindow.Left = screen.Bounds.Left;
                wallpaperWindow.Top = screen.Bounds.Top;
                wallpaperWindow.Width = screen.Bounds.Width;
                wallpaperWindow.Height = screen.Bounds.Height;
                wallpaperWindow.WindowStyle = WindowStyle.None; // 窗口无边框
                wallpaperWindow.WindowState = WindowState.Normal; // 窗口正常状态
                Console.WriteLine($"{screen.Bounds.Width} x {screen.Bounds.Height} | {screen.Bounds.Left}, {screen.Bounds.Top}");
                wallpaperWindow.Show();
                //wallpaperWindow.SetAsDesktopWallpaper();
                
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
                /*
                SetParent(new WindowInteropHelper(this).Handle, workerw);
                Console.WriteLine($"SetParent to WorkerW, handle: {new WindowInteropHelper(this).Handle}");

                int exStyle = GetWindowLong(new WindowInteropHelper(this).Handle, GWL_EXSTYLE);
                exStyle &= ~WS_EX_TRANSPARENT; // 移除透明样式以允许点击
                SetWindowLong(new WindowInteropHelper(this).Handle, GWL_EXSTYLE, exStyle);
                Console.WriteLine($"Set window style to accept mouse events, exStyle: {exStyle}");

                bool setResult = SetWindowPos(new WindowInteropHelper(this).Handle, HWND_BOTTOM, 0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOZORDER);
                Console.WriteLine($"SetWindowPos result: {setResult}");
*/

                IntPtr hWnd = new WindowInteropHelper(wallpaperWindow).Handle;
                SetParent(hWnd, workerw);
                Console.WriteLine($"SetParent to WorkerW, handle: {hWnd}");
                SetWindowLong(hWnd, GWL_EXSTYLE, GetWindowLong(hWnd, GWL_EXSTYLE) | WS_EX_LAYERED );
                SetWindowPos(hWnd, HWND_BOTTOM, screen.Bounds.Left, screen.Bounds.Top, screen.Bounds.Width, screen.Bounds.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
                wallpaperWindow.ChangeUrl("https://www.google.com/");
                
            }
        }


        public static void ChangeAllWallpapersUrl(string url)
        {
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window is Wallpaper wallpaperWindow)
                {
                    wallpaperWindow.ChangeUrl(url);
                }
            }
        }
    }
}
