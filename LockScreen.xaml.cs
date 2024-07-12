using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace _92CloudWallpaper
{
    public partial class LockScreen : Window
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
        private static List<Window> allWindows = new List<Window>();
        private ContextMenu timeSettingsMenu;
        private Main mainInstance;

        public LockScreen(Main main)
        {
            InitializeComponent();
            Loaded += Wallpaper_Loaded;
            allWindows.Add(this);
            InitializeTimeSettingsMenu();
            mainInstance = main;
        }

        private void Wallpaper_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Window Loaded");
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
                            webView.CoreWebView2.ExecuteScriptAsync("document.body.style.overflowX = 'hidden'; document.body.style.overflowY = 'hidden';");

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

        public void ChangeUrl(string url)
        {
            try
            {
                if (webView != null && webView.CoreWebView2 != null && isInitialized)
                {
                    webView.Source = new Uri(url);
                    Console.WriteLine($"URL changed to: {url}");
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
                                    Console.WriteLine($"URL changed to: {url}");
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Close button clicked");
            CloseAllWallpapers();
            Main.lockScreenInitialized = false; // 重置空闲计时器标志
            //mainInstance.idleTimer.Start();
            //mainInstance.InitializeIdleTimer(mainInstance.idleThreshold); // 重新启动IdleTimer
        }

        public static void CloseAllWallpapers()
        {
            foreach (Window window in allWindows)
            {
                window.Close();
            }
            allWindows.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public static void InitializeAllWallpapers(string url, Main main)
        {
            IntPtr workerw = IntPtr.Zero;
            //main.idleTimer.Stop();
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                var wallpaperWindow = new LockScreen(main);
                wallpaperWindow.Left = screen.Bounds.Left;
                wallpaperWindow.Top = screen.Bounds.Top;
                wallpaperWindow.Width = screen.Bounds.Width;
                wallpaperWindow.Height = screen.Bounds.Height;
                wallpaperWindow.WindowStyle = WindowStyle.None; // 窗口无边框
                wallpaperWindow.WindowState = WindowState.Normal; // 窗口最大化
                wallpaperWindow.Topmost = true; // 窗口置顶
                Console.WriteLine($"{screen.Bounds.Width} x {screen.Bounds.Height} | {screen.Bounds.Left}, {screen.Bounds.Top}");
                wallpaperWindow.Show();
                //wallpaperWindow.SetAsDesktopWallpaper();

                IntPtr hWnd = new WindowInteropHelper(wallpaperWindow).Handle;

                Console.WriteLine($"SetParent to WorkerW, handle: {hWnd}");
                SetWindowLong(hWnd, GWL_EXSTYLE, GetWindowLong(hWnd, GWL_EXSTYLE) | WS_EX_LAYERED);
                SetWindowPos(hWnd, HWND_BOTTOM, screen.Bounds.Left, screen.Bounds.Top, screen.Bounds.Width, screen.Bounds.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
                wallpaperWindow.ChangeUrl(url);
            }
        }

        public static void ChangeAllWallpapersUrl(string url)
        {
            foreach (Window window in allWindows)
            {
                if (window is LockScreen wallpaperWindow)
                {
                    Console.WriteLine(url);
                    wallpaperWindow.ChangeUrl(url);
                }
            }
        }

        private void InitializeTimeSettingsMenu()
        {
            timeSettingsMenu = new ContextMenu();

            MenuItem tenSecondsItem = new MenuItem { Header = "10秒" };
            tenSecondsItem.Click += (sender, e) => SetIdleThreshold(10000, tenSecondsItem);

            MenuItem oneMinuteItem = new MenuItem { Header = "1分钟" };
            oneMinuteItem.Click += (sender, e) => SetIdleThreshold(60000, oneMinuteItem);

            MenuItem fiveMinutesItem = new MenuItem { Header = "5分钟" };
            fiveMinutesItem.Click += (sender, e) => SetIdleThreshold(300000, fiveMinutesItem);

            timeSettingsMenu.Items.Add(tenSecondsItem);
            timeSettingsMenu.Items.Add(oneMinuteItem);
            timeSettingsMenu.Items.Add(fiveMinutesItem);

            // 设置初始选中的项
            SetInitialSelectedItem();
        }

        private void SetInitialSelectedItem()
        {
            int currentThreshold = Properties.Settings.Default.IdleThreshold;
            foreach (MenuItem item in timeSettingsMenu.Items)
            {
                item.FontWeight = FontWeights.Normal;
            }

            switch (currentThreshold)
            {
                case 10000:
                    ((MenuItem)timeSettingsMenu.Items[0]).FontWeight = FontWeights.Bold;
                    break;
                case 60000:
                    ((MenuItem)timeSettingsMenu.Items[1]).FontWeight = FontWeights.Bold;
                    break;
                case 300000:
                    ((MenuItem)timeSettingsMenu.Items[2]).FontWeight = FontWeights.Bold;
                    break;
            }
        }

        private void TimeSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            timeSettingsMenu.PlacementTarget = TimeSettingsButton;
            timeSettingsMenu.IsOpen = true;
        }

        private void SetIdleThreshold(int threshold, MenuItem selectedItem)
        {
            //mainInstance.SetIdleThreshold(threshold);

            foreach (MenuItem item in timeSettingsMenu.Items)
            {
                item.FontWeight = FontWeights.Normal;
            }
            selectedItem.FontWeight = FontWeights.Bold;
        }
    }
}
