using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Threading;
using System.Management;
using static ImageCacheManager;
using System.Collections.Generic;

namespace _92CloudWallpaper
{
    public partial class Main : Form
    {
        private NotifyIcon trayIcon;
        private Timer timer;
        public Timer periodUpdateCacheTimer;
        public static bool lockScreenInitialized = false;
        public int defaultInterval = 600000;
        public int savedInterval { get; set; } = 600000; // 默认值为10分钟
        //public string CurrentVersion; // 当前版本号
        public int idleThreshold;
        private MenuHandler menuHandler;
        private SoftwareUpdater softwareUpdater;
        private ImageCacheManager cacheManager;
        //private InfoHelper infoHelper;
        private MainWebView mainWebView; // 添加对 MainWebView 的引用
        public DesktopWindow desktopWindow;
        private readonly Stats stats;
        //public static String Uuid;
        public ImageCacheManager.ImageInfo currentImageInfo { get; set; } // 定义当前图片信息的成员变量
        public string currentWallpaperFilePath;
        public int currentWallpaperIndex { get; set; }
        public int wallpaperCount { get; set; }
        public static Main Instance { get; private set; }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public Main(bool isShowMain = true)
        {
            try
            {
                Instance = this;
                this.Text = InfoHelper.SoftwareInfo.NameCN;
                cacheManager = new ImageCacheManager();
                savedInterval = Properties.Settings.Default.SelectedInterval;
                idleThreshold = Properties.Settings.Default.IdleThreshold; // 从设置中读取idleThreshold
                trayIcon = new NotifyIcon();
                timer = new Timer();
                periodUpdateCacheTimer = new Timer();
                desktopWindow = new DesktopWindow(this);
                menuHandler = new MenuHandler(this, trayIcon, timer); // 先初始化
                softwareUpdater = new SoftwareUpdater(this);
                
                InitializeTimer(savedInterval);
                var Uuid = InfoHelper.GetOrCreateUuid();
                //Console.WriteLine($"Uuid {Uuid}");
                stats = new Stats();
                ShowMainPage(isShowMain);


                this.Load += Main_Load; // 在加载事件中调用异步方法

            }
            catch (Exception ex)
            {
                Logger.LogError("Error during initialization", ex);
            }
        }
        private async void Main_Load(object sender, EventArgs e)
        {
            await stats.ReportAsync(null, InfoHelper.StatsBehavior.StartApplication);

            wallpaperCount = cacheManager.ImageInfos.Count;
            if (wallpaperCount > 0)
            {
                currentWallpaperIndex = cacheManager.CurrentIndex;
                if (currentWallpaperIndex < wallpaperCount)
                {
                    currentImageInfo = cacheManager.ImageInfos[currentWallpaperIndex];
                }
                else
                {
                    currentImageInfo = null;
                }
            }
            else
            {
                await InitializeCarouselAsync(cacheManager);
            }
            ShowNextImage();

            await softwareUpdater.CheckForUpdateAsync(false);

            PeriodUpdateCache();
        }

        public void ShowMainPage(bool isShowMain = true)
        {
            if (isShowMain) 
            {
                ShowPreloadPage(InfoHelper.Urls.Store);
            }
        }

        public void ShowPreloadPage(string url)
        {
            
            // 显示预加载的其他页面
            mainWebView = MainWebView.Instance(this);
            mainWebView.ShowPreloadedPage(url);
        }

        public void ShowFloatWindow()
        {

            //desktopWindow.Show();
            desktopWindow.Visibility = Visibility.Visible;


        }

        public void HideFloatWindow()
        {
            //desktopWindow?.Close();
            desktopWindow.Visibility = Visibility.Hidden;
        }



        public async Task InitializeCarouselAsync(ImageCacheManager cacheManager)
        {
            await cacheManager.LoadImagesAsync(false, 5);
            Console.WriteLine($"imageinfo count {cacheManager.ImageInfos.Count}   cacheManager.CurrentIndex {cacheManager.CurrentIndex}");
            /*if (cacheManager.ImageInfos.Count > 0 && cacheManager.ImageCache.ContainsKey(cacheManager.ImageInfos[cacheManager.CurrentIndex].Url))
            {
                UpdateImageDisplayAsync(cacheManager.ImageCache[cacheManager.ImageInfos[cacheManager.CurrentIndex].Url]);
                
            }*/
            if(cacheManager.ImageInfos.Count > 0)
            {
                currentWallpaperIndex = 0;
                await UpdateImageDisplayAsync(cacheManager.ImageCache[cacheManager.ImageInfos[currentWallpaperIndex].Url]);
            }
            else
            {
                desktopWindow.Dispatcher.Invoke(() => {
                    desktopWindow.DisplayImageInfo();
                });
            }
            //timer.Start();
            //await cacheManager.LoadImagesAsync();

            
        }
        
        private async Task UpdateImageDisplayAsync(ImageCacheManager.ImageCacheItem cacheItem)
        {
            try
            {
                currentImageInfo = cacheItem.Info;
                currentWallpaperFilePath = cacheItem.FilePath;
                SetWallpaper(cacheItem.FilePath);
                cacheManager.SaveCurrentPosition(cacheManager.CurrentIndex);
                currentWallpaperIndex = cacheManager.CurrentIndex;
                wallpaperCount = cacheManager.ImageInfos.Count;

                
                desktopWindow.Dispatcher.Invoke(() => {
                    desktopWindow.DisplayImageInfo();
                });

                //await cacheManager.CacheImageSurround();

                await stats.ReportAsync(currentImageInfo, InfoHelper.StatsBehavior.SetWallpaper);


            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load image from {cacheItem.FilePath}",ex);
            }
        }


        public void SetWallpaperChangeInterval(int interval)
        {
            savedInterval = interval;
            Properties.Settings.Default.SelectedInterval = interval;
            Properties.Settings.Default.Save();
        }

        public async void CheckForUpdate(object sender, EventArgs e)
        {
            await softwareUpdater.CheckForUpdateAsync();
        }

        public void UpdateVersionMenuItemText(string text)
        {
            menuHandler.UpdateVersionMenuItemText(text);
        }

        public void SetVersionMenuItemClickEvent(EventHandler eventHandler)
        {
            menuHandler.SetVersionMenuItemClickEvent(eventHandler);
        }

        public async void ShowNextImage()
        {
            bool flag = false;
            if (cacheManager.ImageInfos.Count > 0)
            {
                cacheManager.CurrentIndex = (cacheManager.CurrentIndex + 1) % cacheManager.ImageInfos.Count;
                if (cacheManager.ImageCache.ContainsKey(cacheManager.ImageInfos[cacheManager.CurrentIndex].Url))
                {
                    await UpdateImageDisplayAsync(cacheManager.ImageCache[cacheManager.ImageInfos[cacheManager.CurrentIndex].Url]);
                }
                else
                {
                    Console.WriteLine("下一张时，更新缓存图片");
                    //await cacheManager.CacheImageSurround();

                }
                if(cacheManager.ImageInfos.Count > GlobalData.PageSize)
                {
                    Console.WriteLine($"CurrentIndex: {cacheManager.CurrentIndex} / ImageInfos.Count: {cacheManager.ImageInfos.Count}" );
                    if(cacheManager.CurrentIndex % GlobalData.PageSize == GlobalData.PageSize - 1)
                    {
                        flag = true;
                    }
                }
                else
                {
                    if (cacheManager.CurrentIndex == cacheManager.ImageInfos.Count - 1)
                    {
                        flag = true;
                    }
                }
                if (flag)
                {
                    try
                    {
                        await cacheManager.LoadImagesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception: {ex.Message}");
                    }
                }
               
            }
        }

        public async void ShowPrevImage()
        {
            if (cacheManager.ImageInfos.Count > 0)
            {
                cacheManager.CurrentIndex = (cacheManager.CurrentIndex - 1 + cacheManager.ImageInfos.Count) % cacheManager.ImageInfos.Count;
                if (cacheManager.ImageCache.ContainsKey(cacheManager.ImageInfos[cacheManager.CurrentIndex].Url))
                {
                    await UpdateImageDisplayAsync(cacheManager.ImageCache[cacheManager.ImageInfos[cacheManager.CurrentIndex].Url]);
                }
                else
                {
                    Console.WriteLine("上一张时，更新缓存图片");
                    //await cacheManager.CacheImageSurround();
                    
                }
            }
            
        }
        public async Task ImagesAsync()
        {
            await cacheManager.LoadImagesAsync();
            
            ShowNextImage();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public void PauseWallpaperChange()
        {
            Properties.Settings.Default.SelectedInterval = 0;
            Properties.Settings.Default.Save();
            //timer.Stop();
            menuHandler.UpdateFloatWindowButtons(false);
            menuHandler.PauseWallpaperChange();


        }

        public void ResumeWallpaperChange()
        {
            int savedInterval = Properties.Settings.Default.SelectedInterval;
            if (savedInterval > 0)
            {
                Properties.Settings.Default.SelectedInterval = savedInterval;
                Properties.Settings.Default.Save();
            }
            //timer.Start();
            menuHandler.UpdateFloatWindowButtons(true);
            menuHandler.ResumeWallpaperChange();
        }

        private void InitializeTimer(int interval)
        {
            if (interval > 0)
            {
                timer.Interval = interval;
                timer.Tick += (sender, e) => ShowNextImage();
                timer.Start();
            }
        }

        private async void PeriodUpdateCache()
        {
            periodUpdateCacheTimer.Interval = 60*60*1000;
            periodUpdateCacheTimer.Tick += (sender, e) =>
            {
                Task.Run(async () =>
                {
                    Console.WriteLine($"定时更新 {DateTime.Now}");
                    await cacheManager.LoadImagesAsync();
                }).ConfigureAwait(false);
            };
            await Task.Delay(periodUpdateCacheTimer.Interval);
            periodUpdateCacheTimer.Start();
        }



        public void Logout(object sender, EventArgs e)
        {
            if (System.Windows.Forms.MessageBox.Show("您确定要登出吗？", "确认登出", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                AttemptLogout();
                //menuHandler.UpdateLoginMenuItem("登录", Login);
            }
        }

        public void AttemptLogout()
        {
            Properties.Settings.Default.UserId = 0;
            Properties.Settings.Default.Token = "";
            Properties.Settings.Default.Save();

            GlobalData.UserId = 0;
            GlobalData.Token = "";

            //mainWebView.ClearCookies();

            var cacheManagerNew = new ImageCacheManager();
            
            cacheManagerNew.SaveVersionInfo(InfoHelper.SoftwareInfo.CurrentVersion, GlobalData.UserId);

            Task.Run(() => InitializeCarouselAsync(cacheManagerNew));
            cacheManager = cacheManagerNew;
            //menuHandler.RemoveManageWallpapersMenuItem();
            //menuHandler.UpdateLoginMenuItem("登录", Login);
            HideFloatWindow();

        }

        public void Login(object sender, EventArgs e)
        {
            //ShowLoginForm();
        }

        public async void LoginSuccess()
        {
            //menuHandler.UpdateLoginMenuItem("登出", Logout);
            //menuHandler.AddManageWallpapersMenuItem();
            try
            {
                var cacheManagerNew = new ImageCacheManager();
                cacheManagerNew.SaveVersionInfo(InfoHelper.SoftwareInfo.CurrentVersion, GlobalData.UserId);
                cacheManager = cacheManagerNew;
                ShowFloatWindow();
                await Task.Run(() => InitializeCarouselAsync(cacheManagerNew));
            }
            catch (Exception ex) 
            {
                Logger.LogError("Error during LoginSuccess", ex);
            }

        }
        public void HandleCommand(string command)
        {
            if (command == "ShowPreloadPage")
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => ShowPreloadPage(InfoHelper.Urls.Store)));
                }
                else
                {
                    ShowPreloadPage(InfoHelper.Urls.Store);
                }
            }
        }


        private void SetWallpaper(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                Console.WriteLine($"Invalid path or file does not exist: {path}");
                return;
            }

            const int SPI_SETDESKWALLPAPER = 20;
            const int SPIF_UPDATEINIFILE = 0x01;
            const int SPIF_SENDWININICHANGE = 0x02;

            try
            {
                int result = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
                if (result == 0)
                {
                    Console.WriteLine("Failed to set wallpaper.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception while setting wallpaper",ex);
            }
        }



        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // 隐藏窗体
            ShowInTaskbar = false; // 移除任务栏图标
            base.OnLoad(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            CleanupBeforeExit();
        }

        public void CleanupBeforeExit()
        {
            try
            {
                var funcMessage = "关闭程序前 数据清理";
                Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");
                timer.Stop();
                //HideFloatWindow();
                if (desktopWindow != null)
                {
                    desktopWindow.Close();
                }
                //desktopWindow?.Close();
            
                cacheManager.SaveCurrentPosition(cacheManager.CurrentIndex);
                cacheManager.SaveVersionInfo(InfoHelper.SoftwareInfo.CurrentVersion, GlobalData.UserId);
                trayIcon.Dispose();
                Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
            }
            catch(Exception ex)
            {
                Logger.LogError("关闭程序前", ex);
            }
            
        }
        

        [StructLayout(LayoutKind.Sequential)]
        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        
    }
}
