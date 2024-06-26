using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Forms;
using Transitionals.Controls;
using Transitionals.Transitions;
using Timer = System.Timers.Timer;
using MessageBox = System.Windows.Forms.MessageBox;

namespace _92CloudWallpaper
{
    public partial class LockScreenWPF : Window
    {
        private string[] imageFiles;
        //private int currentImageIndex = 0;
        private Timer slideshowTimer;
        private static List<LockScreenWPF> openWindows = new List<LockScreenWPF>();

        public LockScreenWPF()
        {
            InitializeComponent();
            //LoadImages();
            //StartSlideshow();
            Loaded += Wallpaper_Loaded;
            openWindows.Add(this);
        }

        private void LoadImages()
        {
            // 修改缓存目录为指定路径
            string cacheDirectory = @"C:\Users\xuant\Downloads";
            if (Directory.Exists(cacheDirectory))
            {
                imageFiles = Directory.GetFiles(cacheDirectory, "*.jpg");
                if (imageFiles.Length == 0)
                {
                    MessageBox.Show("No images found in the cache directory.");
                }
            }
            else
            {
                MessageBox.Show("Cache directory not found.");
            }
        }

        private void StartSlideshow()
        {
            slideshowTimer = new Timer(5000); // 5秒切换一次图片
            slideshowTimer.Elapsed += OnSlideshowTimerElapsed;
            slideshowTimer.Start();
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

                            //isInitialized = true;
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
        private void OnSlideshowTimerElapsed(object sender, ElapsedEventArgs e)
        {
            
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllWindows();
        }

        private void TimeSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // 时间设置按钮点击处理逻辑
        }

        public static void CloseAllWindows()
        {
            foreach (var window in openWindows.ToList())
            {
                window.Dispatcher.Invoke(() => window.Close());
            }
            openWindows.Clear();
        }

        public static void StartLockScreens()
        {
            var screens = Screen.AllScreens;
            foreach (var screen in screens)
            {
                var screenBounds = screen.Bounds;
                var thread = new System.Threading.Thread(() =>
                {
                    var wpfWindow = new LockScreenWPF
                    {
                        Left = screenBounds.Left,
                        Top = screenBounds.Top,
                        Width = screenBounds.Width,
                        Height = screenBounds.Height,
                        WindowStartupLocation = WindowStartupLocation.Manual
                    };

                    wpfWindow.Loaded += (sender, args) =>
                    {
                        // 设置窗口全屏
                        wpfWindow.WindowState = WindowState.Maximized;
                    };

                    wpfWindow.Show();
                    Dispatcher.Run();
                });

                // 设置线程为STA模式
                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                thread.Start();
            }
        }
    }
}
