﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

namespace _92CloudWallpaper
{
    public partial class Main : Form
    {
        private NotifyIcon trayIcon;
        private Timer timer;
        
        private readonly int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        private readonly int screenHeight = Screen.PrimaryScreen.Bounds.Height;
        //private string imageUrl = "https://source.unsplash.com/random/"+screenWidth+"x"+ screenHeight; // 替换为你的图片URL
        private readonly string appImageUrl = "https://cnapi.levect.com/v1/photoFrame/imageList";
        private ToolStripMenuItem loginMenuItem;
        private ToolStripMenuItem autoStartMenuItem;
        private List<string> paths = new List<string>();
        private List<string> pathsTemp = new List<string>();
        private List<string> syncWallpaperURLs = new List<string>();
        //private int pathsCount = 0;
        private int cacheIndex = 0;
        private const int CacheExpirationDays = 7;

        public Main()
        {
            InitializeComponent();
            InitializeTrayIcon();
#if DEBUG
            InitializeTimer(5000);
#else
            InitializeTimer(600000);
#endif
            _ = InitializeAndSetWallpaperAsync();  // 初始化时进行缓存并设置壁纸
        }

        private void InitializeTrayIcon()
        {
            ContextMenuStrip trayMenu = new ContextMenuStrip();
            ToolStripMenuItem changeWallpaperMenu = new ToolStripMenuItem("更换壁纸");

            string[] intervals = { "暂停", "1 分钟", "10 分钟", "半小时", "1 小时", "1 天", "立即更换" };
            int[] times =
            {
                0, 60000, 600000, 1800000, 3600000, 86400000
            };

            for (int i = 0; i < intervals.Length; i++)
            {
                var menuItem = new ToolStripMenuItem(intervals[i], null, ChangeWallpaperEvent);
                if (i < times.Length) menuItem.Tag = times[i];
                if (i == 3) menuItem.Checked = true;  // 默认选中每小时
                changeWallpaperMenu.DropDownItems.Add(menuItem);
            }

            if (Properties.Settings.Default.UserId == 0)
            {
                loginMenuItem = new ToolStripMenuItem("登录", null, Login);
            }
            else
            {
                loginMenuItem = new ToolStripMenuItem("登出", null, Logout);
            }
            trayMenu.Items.Add(loginMenuItem);

            autoStartMenuItem = new ToolStripMenuItem("开机启动", null, ToggleAutoStart)
            {
                CheckOnClick = true,
                Checked = IsApplicationAutoStarting()
            };

            trayMenu.Items.Add(autoStartMenuItem);
            trayMenu.Items.Add(changeWallpaperMenu);
            trayMenu.Items.Add("退出程序", null, (sender, e) => Application.Exit());

            Icon trayIconImage = LoadIconFromResource("_92CloudWallpaper.7418_logo32.png");

            trayIcon = new NotifyIcon()
            {
                Icon = trayIconImage,
                ContextMenuStrip = trayMenu,
                Visible = true,
                Text = "92云壁纸"
            };
            trayIcon.MouseClick += TrayIcon_MouseClick;
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _ = SetWallpaperAsync();
            }
        }

        private Icon LoadIconFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    MessageBox.Show("Error loading icon resource.");
                    return null;
                }
                Image image = Image.FromStream(resourceStream);
                Bitmap bitmap = new Bitmap(image);
                return Icon.FromHandle(bitmap.GetHicon());
            }
        }

        private void ChangeWallpaperEvent(object sender, EventArgs e)
        {
            var clickedItem = sender as ToolStripMenuItem;

            foreach (ToolStripMenuItem item in clickedItem.GetCurrentParent().Items)
            {
                item.Checked = item == clickedItem;
            }

            if (clickedItem.Text == "立即更换")
            {
                _ = SetWallpaperAsync();
                return;
            }

            int interval = (int)clickedItem.Tag;
            if (interval > 0)
            {
                timer.Interval = interval;
                timer.Start();
                _ = SetWallpaperAsync();
            }
            else
            {
                timer.Stop();
            }
        }

        private void InitializeTimer(int interval)
        {
            timer = new Timer
            {
                Interval = interval
            };
            timer.Tick += async (sender, e) => await SetWallpaperAsync();
            timer.Start();
        }

        private async void Logout(object sender, EventArgs e)
        {
            if (MessageBox.Show("您确定要登出吗？", "确认登出", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await AttemptLogoutAsync();
                loginMenuItem.Text = "登录";
                loginMenuItem.Click -= Logout;
                loginMenuItem.Click += Login;
            }
        }

        private async Task AttemptLogoutAsync()
        {
            Properties.Settings.Default.UserId = 0;
            Properties.Settings.Default.Save();
            
            ClearCache();
            GlobalData.UserId = 0;
            GlobalData.LoginFlag = 0;
            cacheIndex = 0;  // 重置cacheIndex
            await InitializeAndSetWallpaperAsync();
        }

        private void Login(object sender, EventArgs e)
        {
            ShowLoginForm();
        }

        private async void LoginSuccess()
        {
            loginMenuItem.Text = "登出";
            loginMenuItem.Click -= Login;
            loginMenuItem.Click += Logout;
            ClearCache();
            cacheIndex = 0;  // 重置cacheIndex
            await InitializeAndSetWallpaperAsync();
        }

        private void ShowLoginForm()
        {
            using (LoginForm loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    //GlobalData.UserId = loginForm.UserId; // 假设在登录表单中设置了UserId
                    LoginSuccess();
                }
            }
        }

        private async Task DownloadAsync()
        {
            int pageIndex = GlobalData.PageIndex;
            int pageSize = 4;
            var apiHandler = new ApiRequestHandler();

            var body = new SortedDictionary<string, object>
            {
                { "userId" , GlobalData.UserId },
                { "height" , screenHeight },
                { "pageIndex", pageIndex },
                { "pageSize" , pageSize },
                { "width" , screenWidth },
            };

            var response = await apiHandler.SendApiRequestAsync(appImageUrl, body).ConfigureAwait(false);
            Console.WriteLine(response);
            List<string> urlList = new List<string>();
            using (JsonDocument doc = JsonDocument.Parse(response))
            {
                JsonElement root = doc.RootElement;
                JsonElement bodyElement = root.GetProperty("body");
                JsonElement listElement = bodyElement.GetProperty("list");

                foreach (JsonElement l in listElement.EnumerateArray())
                {
                    String url = l.GetProperty("url").GetString();
                    urlList.Add(url);
                    syncWallpaperURLs.Add(url);
                }
            }
            //Console.WriteLine($"urlList1111 {syncWallpaperURLs.Count}");
            string cacheDir = GetCacheDir();
            if (urlList.Count == 0)
            {
                //Console.WriteLine($"urlList222 {urlList.Count}");
                

                // 确保缓存目录存在
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                // 删除接口中没有返回的图片，并保留一周内的图片
                var cachedFiles = Directory.GetFiles(cacheDir);
                
                var expiredFiles = cachedFiles
                    .Where(f => (DateTime.Now - File.GetCreationTime(f)).TotalDays > CacheExpirationDays)
                    .ToList();
                
                foreach (var cachedFile in cachedFiles) { 
                    Console.WriteLine(cachedFile);
                }
                List<string> newPaths = new List<string>();
                //Console.WriteLine($"urlList222 {syncWallpaperURLs.Count}");
                foreach (var syncWallpaperURL in syncWallpaperURLs){
                    //Console.WriteLine(syncWallpaperURL);
                    newPaths.Add(Path.Combine(cacheDir, Path.GetFileName(syncWallpaperURL)));
                }
                var filesToDelete = cachedFiles
                    .Except(newPaths.ToArray())
                    //.Concat(expiredFiles)
                    .ToList();

                foreach (var file in filesToDelete)
                {
                    //Console.WriteLine(file);
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }


                paths = newPaths;
                syncWallpaperURLs.Clear();
                pathsTemp.Clear();
                GlobalData.PageIndex = 1;
                //_ = SetWallpaperAsync();
                //return;
            }
            else{ 

                using (var downloader = new Downloader())
                {
                    await downloader.DownloadFilesAsync(urlList, filePaths =>
                    {
                        foreach (var path in downloader.GetFilePaths())
                        {
                            if (!pathsTemp.Contains(path))
                            {
                                pathsTemp.Add(path);
                            }
                        }
    /*                    if (downloader.GetFilePaths().Count == pageSize)
                        {
                            GlobalData.PageIndex++;
                        }
                        else
                        {
                            GlobalData.PageIndex = 1;
                        }*/
                    });
                }
                CacheImages(pathsTemp);
                GlobalData.PageIndex++;
            }
            
        }

        private void CacheImages(List<string> paths)
        {
            string cacheDir = GetCacheDir();
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
            foreach (string path in paths)
            {
                string fileName = Path.GetFileName(path);
                string destFile = Path.Combine(cacheDir, fileName);
                if (!File.Exists(destFile))
                {
                    try {
                        File.Copy(path, destFile);
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex.Message);
                    }
                    
                }
            }
        }

        private string GetCacheDir()
        {
            string userId = GlobalData.UserId.ToString();
            return Path.Combine(Path.GetTempPath(), "CloudWallpaper", userId);
        }

        private void ClearCache()
        {
            string cacheDir = GetCacheDir();
            if (Directory.Exists(cacheDir))
            {
                var files = Directory.GetFiles(cacheDir);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            string cacheTmpDir = Path.Combine(Path.GetTempPath(), "CloudWallpaper", "Temp");
            if (Directory.Exists(cacheTmpDir))
            {
                var files = Directory.GetFiles(cacheTmpDir);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
        }

        private async Task InitializeAndSetWallpaperAsync()
        {
            await DownloadAsync();
            await SetWallpaperAsync();
        }

        private async Task SetWallpaperAsync()
        {
            LoadCachedImages();
            Console.WriteLine($" paths.Count:{paths.Count} ||| cacheIndex: {cacheIndex}");
            if (paths != null && paths.Count > 0)
            {
                if (cacheIndex >= paths.Count)
                {
                    cacheIndex = 0;
                }

                string localPath = paths[cacheIndex];
                if (File.Exists(localPath))
                {
                    long fileSize = new FileInfo(localPath).Length;
                    if (fileSize > 0)
                    {
                        SetWallpaper(localPath);
                        cacheIndex++;
                        if (cacheIndex >= paths.Count)
                        {
                            cacheIndex = 0;
                            await DownloadAsync();
                        }
                    }
                }
            }
            else
            {
                await DownloadAsync();
            }
        }

        private void LoadCachedImages()
        {
            string cacheDir = GetCacheDir();
            if (Directory.Exists(cacheDir))
            {
                var cachedFiles = Directory.GetFiles(cacheDir).OrderBy(f => File.GetCreationTime(f)).ToList();
                foreach (var file in cachedFiles)
                {
                    if (!paths.Contains(file))
                    {
                        paths.Add(file);
                    }
                }
            }
        }

        private async Task DownloadAndSetWallpaper()
        {
            string localPath = Path.Combine(Path.GetTempPath(), "currentWallpaper.jpg");
            var apiHandler = new ApiRequestHandler();

            var body = new SortedDictionary<string, object>
            {
                { "userId" , GlobalData.UserId },
                { "height" , screenHeight },
                { "pageIndex", 1 },
                { "pageSize" , 1 },
                { "width" , screenWidth },
            };

            var response = await apiHandler.SendApiRequestAsync(appImageUrl, body);
            string url;

            using (JsonDocument doc = JsonDocument.Parse(response))
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("body", out JsonElement bodyElement) &&
                bodyElement.TryGetProperty("list", out JsonElement listElement) &&
                listElement.GetArrayLength() > 0)
                {
                    JsonElement l = listElement[0];
                    url = l.GetProperty("url").GetString();
                    using (WebClient client = new WebClient())
                    {
                        try
                        {
                            client.DownloadFile(url, localPath);
                            SetWallpaper(localPath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"An error occurred: {ex.Message}.URL{url}");
                        }
                    }
                }
                else
                {
                    GlobalData.PageIndex = 1;
                }
            }
        }

        private void DownloadAndSetWallpaper(string url)
        {
            string localPath = Path.Combine(Path.GetTempPath(), "currentWallpaper.jpg");
            using (WebClient client = new WebClient())
            {
                try
                {
                    client.DownloadFile(url, localPath);
                    SetWallpaper(localPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}");
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private void ToggleAutoStart(object sender, EventArgs e)
        {
            if (autoStartMenuItem.Checked)
            {
                SetApplicationAutoStart(true);
            }
            else
            {
                SetApplicationAutoStart(false);
            }
        }

        private void SetApplicationAutoStart(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (enable)
                {
                    key.SetValue(Application.ProductName, Application.ExecutablePath);
                }
                else
                {
                    key.DeleteValue(Application.ProductName, false);
                }
            }
        }

        private bool IsApplicationAutoStarting()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key.GetValue(Application.ProductName) != null;
            }
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

        public enum WallpaperStyle : int
        {
            Tiled,
            Centered,
            Stretched
        }
    }
}