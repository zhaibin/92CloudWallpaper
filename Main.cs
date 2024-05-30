using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _92CloudWallpaper
{
    public partial class Main : Form
    {
        private NotifyIcon trayIcon;
        private Timer timer;
        public int defaultInterval = 600000;
        public int savedInterval { get; set; } = 600000; // 默认值为10分钟
        public string currentVersion ; // 当前版本号
        //private bool isPaused = false;
        private MenuHandler menuHandler;
        private SoftwareUpdater softwareUpdater;
        private ImageCacheManager cacheManager;
        private WallpaperControlWindow wallpaperControlWindow;


        public ImageCacheManager.ImageInfo currentImageInfo { get; set; } // 定义当前图片信息的成员变量
        public int currentWallpaperIndex { get; set; }
        public int wallpaperCount { get; set; }
        public Main()
        {
            try
            {
                cacheManager = new ImageCacheManager();
                savedInterval = Properties.Settings.Default.SelectedInterval;
                trayIcon = new NotifyIcon();
                timer = new Timer();
                menuHandler = new MenuHandler(this, trayIcon, timer); // 先初始化
                softwareUpdater = new SoftwareUpdater(this);
                wallpaperControlWindow = new WallpaperControlWindow(this, menuHandler);
                

                InitializeTimer(savedInterval);

                Task.Run(() => InitializeCarouselAsync(cacheManager));

                // 检查更新
                Task.Run(async () => await softwareUpdater.CheckForUpdateAsync());
            }
            catch (Exception ex)
            {
                Logger.LogError("Error during initialization", ex);
                cacheManager.DeleteCacheDirectory();
            }
        }
        public string GetCurrentVersion()
        {
            // 获取当前程序集
            //Assembly assembly = Assembly.GetExecutingAssembly();
            // 获取程序集版本号
            //Version version = assembly.GetName().Version;
            //currentVersion = "v"+version.ToString();
            currentVersion = "v0.3.3.0";
            return currentVersion;
        }
        public async Task InitializeCarouselAsync(ImageCacheManager cacheManager)
        {
            await cacheManager.LoadImagesAsync();
            if (cacheManager.ImageInfos.Count > 0 && cacheManager.ImageCache.ContainsKey(cacheManager.ImageInfos[cacheManager.CurrentIndex].Url))
            {
                UpdateImageDisplayAsync(cacheManager.ImageCache[cacheManager.ImageInfos[cacheManager.CurrentIndex].Url]);
                timer.Start();
            }
        }

        private void UpdateImageDisplayAsync(ImageCacheManager.ImageCacheItem cacheItem)
        {
            try
            {
                currentImageInfo = cacheItem.Info;
                SetWallpaper(cacheItem.FilePath);
                cacheManager.SaveCurrentPosition(cacheManager.CurrentIndex);
                currentWallpaperIndex = cacheManager.CurrentIndex;
                wallpaperCount = cacheManager.ImageInfos.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load image from {cacheItem.FilePath}. Exception: {ex.Message}");
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
            if (cacheManager.ImageInfos.Count > 0)
            {
                cacheManager.CurrentIndex = (cacheManager.CurrentIndex + 1) % cacheManager.ImageInfos.Count;
                if (cacheManager.ImageCache.ContainsKey(cacheManager.ImageInfos[cacheManager.CurrentIndex].Url))
                {
                    UpdateImageDisplayAsync(cacheManager.ImageCache[cacheManager.ImageInfos[cacheManager.CurrentIndex].Url]);
                }

                if (cacheManager.CurrentIndex == cacheManager.ImageInfos.Count - 1)
                {
                    await cacheManager.LoadImagesAsync();
                }
            }
        }

        public void ShowPrevImage()
        {
            if (cacheManager.ImageInfos.Count > 0)
            {
                cacheManager.CurrentIndex = (cacheManager.CurrentIndex - 1 + cacheManager.ImageInfos.Count) % cacheManager.ImageInfos.Count;
                if (cacheManager.ImageCache.ContainsKey(cacheManager.ImageInfos[cacheManager.CurrentIndex].Url))
                {
                    UpdateImageDisplayAsync(cacheManager.ImageCache[cacheManager.ImageInfos[cacheManager.CurrentIndex].Url]);
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public void PauseWallpaperChange()
        {
            Properties.Settings.Default.SelectedInterval = 0;
            Properties.Settings.Default.Save();
            timer.Stop();
            menuHandler.UpdateFloatWindowButtons(false);
        }

        public void ResumeWallpaperChange()
        {
            int savedInterval = Properties.Settings.Default.SelectedInterval;
            if (savedInterval > 0)
            {
                Properties.Settings.Default.SelectedInterval = savedInterval;
                Properties.Settings.Default.Save();
            }
            timer.Start();
            menuHandler.UpdateFloatWindowButtons(true);
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

        public void Logout(object sender, EventArgs e)
        {
            if (MessageBox.Show("您确定要登出吗？", "确认登出", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                AttemptLogout();
                //UpdateLoginMenuItem("登录", Login);
                menuHandler.UpdateLoginMenuItem("登录", Login);

            }
        }

        private void AttemptLogout()
        {
            Properties.Settings.Default.UserId = 0;
            Properties.Settings.Default.Save();

            GlobalData.UserId = 0;
            //GlobalData.LoginFlag = 0;
            var cacheManagerNew = new ImageCacheManager();
            cacheManagerNew.SaveVersionInfo(GetCurrentVersion(), GlobalData.UserId);
            
            Task.Run(() => InitializeCarouselAsync(cacheManagerNew));
            cacheManager = cacheManagerNew;
            menuHandler.RemoveManageWallpapersMenuItem();
        }

        public void Login(object sender, EventArgs e)
        {
            ShowLoginForm();
        }

        private async void LoginSuccess()
        {
            //UpdateLoginMenuItem("登出", Logout);
            menuHandler.UpdateLoginMenuItem("登出", Logout);
            menuHandler.AddManageWallpapersMenuItem();
            var cacheManagerNew = new ImageCacheManager();
            cacheManagerNew.SaveVersionInfo(GetCurrentVersion(), GlobalData.UserId);
            cacheManager = cacheManagerNew;
            await Task.Run(() => InitializeCarouselAsync(cacheManagerNew));
            
            
            //ShowNextImage();


        }

        private void ShowLoginForm()
        {
            using (LoginForm loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    LoginSuccess();
                }
            }
        }

        private void UpdateLoginMenuItem(string text, EventHandler clickEvent)
        {
            menuHandler.UpdateLoginMenuItem(text, clickEvent);
        }

        private void SetWallpaper(string path)
        {
            const int SPI_SETDESKWALLPAPER = 20;
            const int SPIF_UPDATEINIFILE = 0x01;
            const int SPIF_SENDWININICHANGE = 0x02;
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
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

        private void CleanupBeforeExit()
        {
            timer.Stop();
            wallpaperControlWindow?.Close();
            cacheManager.SaveCurrentPosition(cacheManager.CurrentIndex);
            cacheManager.SaveVersionInfo(GetCurrentVersion(), GlobalData.UserId);
            trayIcon.Dispose();
        }
    }
}
