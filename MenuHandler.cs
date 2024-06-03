using Microsoft.Win32;
using System.Drawing;
using System.Windows.Forms;
using System;
using System.Diagnostics;
using System.Reflection;


namespace _92CloudWallpaper
{
    public class MenuHandler
    {
        private readonly Main mainForm;
        private readonly NotifyIcon trayIcon;
        private readonly Timer timer;
        private WallpaperControlWindow floatWindow;

        private ToolStripMenuItem loginMenuItem;
        private ToolStripMenuItem autoStartMenuItem;
        private ToolStripMenuItem floatWindowMenuItem;
        private ToolStripMenuItem changeWallpaperMenu;
        private ToolStripMenuItem versionMenuItem;
        private ToolStripMenuItem settingsMenu;
        private ToolStripMenuItem clearCacheMenuItem;
        private ToolStripMenuItem manageWallpapersMenuItem = null;
        //public readonly string currentVersion; // 当前版本号
        private readonly int[] times = { 0, 60000, 600000, 1800000, 3600000, 86400000 };
        private readonly string[] intervals = { "暂停", "1 分钟", "10 分钟", "半小时", "1 小时", "1 天" };
        private int previousInterval;

        public MenuHandler(Main mainForm, NotifyIcon trayIcon, Timer timer)
        {
            var funcMessage = "菜单初始化";
            Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");
            this.mainForm = mainForm;
            this.trayIcon = trayIcon;
            this.timer = timer;
            floatWindow = new WallpaperControlWindow(mainForm, this);

            this.previousInterval = mainForm.savedInterval;
            InitializeTrayIcon();

            if (Properties.Settings.Default.IsFloatWindowVisible)
            {
                ShowFloatWindow();
            }
            Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
        }

        private void InitializeTrayIcon()
        {
            ContextMenuStrip trayMenu = new ContextMenuStrip()
            {
                Font = SystemFonts.MenuFont
            };

            changeWallpaperMenu = new ToolStripMenuItem("更换壁纸");
            for (int i = 0; i < intervals.Length; i++)
            {
                var menuItem = new ToolStripMenuItem(intervals[i], null, ChangeWallpaperEvent);
                if (i < times.Length) menuItem.Tag = times[i];
                menuItem.Checked = i == Array.IndexOf(times, mainForm.savedInterval);
                changeWallpaperMenu.DropDownItems.Add(menuItem);
            }

            if (Properties.Settings.Default.UserId == 0)
            {
                loginMenuItem = new ToolStripMenuItem("登录", null, mainForm.Login);
                trayMenu.Items.Add(loginMenuItem);
            }
            else
            {
                loginMenuItem = new ToolStripMenuItem("登出", null, mainForm.Logout);
                trayMenu.Items.Add(loginMenuItem);
                manageWallpapersMenuItem = new ToolStripMenuItem("管理壁纸");
                manageWallpapersMenuItem.DropDownItems.Add(new ToolStripMenuItem("发布壁纸", null, (sender, e) => Process.Start(new ProcessStartInfo("https://creators-pc-cn.levect.com/Content/publishworks") { UseShellExecute = true })));
                manageWallpapersMenuItem.DropDownItems.Add(new ToolStripMenuItem("管理壁纸", null, (sender, e) => Process.Start(new ProcessStartInfo("https://creators-pc-cn.levect.com/Content/workmanagement") { UseShellExecute = true })));
                trayMenu.Items.Add(manageWallpapersMenuItem);
            }
            

            settingsMenu = new ToolStripMenuItem("软件设置");

            autoStartMenuItem = new ToolStripMenuItem("开机启动", null, ToggleAutoStart)
            {
                CheckOnClick = true,
                Checked = IsApplicationAutoStarting()
            };

            floatWindowMenuItem = new ToolStripMenuItem("悬浮窗展示", null, ToggleFloatWindow)
            {
                CheckOnClick = true,
                Checked = Properties.Settings.Default.IsFloatWindowVisible
            };

            clearCacheMenuItem = new ToolStripMenuItem("清理缓存", null, ClearCache);

            settingsMenu.DropDownItems.Add(autoStartMenuItem);
            settingsMenu.DropDownItems.Add(floatWindowMenuItem);
            settingsMenu.DropDownItems.Add(clearCacheMenuItem);

            trayMenu.Items.Add(settingsMenu);
            trayMenu.Items.Add(changeWallpaperMenu);
            versionMenuItem = new ToolStripMenuItem($"版本 {mainForm.GetCurrentVersion()}");
            versionMenuItem.Click += mainForm.CheckForUpdate;

            trayMenu.Items.Add(versionMenuItem);
            trayMenu.Items.Add("退出软件", null, (sender, e) => ApplicationExit());

            Icon trayIconImage = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            trayIcon.Icon = trayIconImage;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.Text = "92云壁纸";
            trayIcon.MouseClick += TrayIcon_MouseClick;
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mainForm.ShowNextImage();
            }
        }

        public void ChangeWallpaperEvent(object sender, EventArgs e)
        {
            var clickedItem = sender as ToolStripMenuItem;
            if (clickedItem == null) return;

            int selectedIndex = changeWallpaperMenu.DropDownItems.IndexOf(clickedItem);
            if (selectedIndex < times.Length)
            {
                Properties.Settings.Default.SelectedInterval = times[selectedIndex];
                Properties.Settings.Default.Save();
            }
            foreach (ToolStripMenuItem item in clickedItem.GetCurrentParent().Items)
            {
                item.Checked = item == clickedItem;
            }

            int interval = (int)clickedItem.Tag;
            if (interval > 0)
            {
                previousInterval = interval;
                timer.Interval = interval;
                timer.Start();
                mainForm.ShowNextImage();
                mainForm.SetWallpaperChangeInterval(interval);
                UpdateFloatWindowButtons(true);
            }
            else
            {
                timer.Stop();
                mainForm.SetWallpaperChangeInterval(interval);
                UpdateFloatWindowButtons(false);
            }
        }

        private void ToggleAutoStart(object sender, EventArgs e)
        {
            SetApplicationAutoStart(autoStartMenuItem.Checked);
        }

        private void SetApplicationAutoStart(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key == null) return;

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
                return key?.GetValue(Application.ProductName) != null;
            }
        }

        private void ToggleFloatWindow(object sender, EventArgs e)
        {
            if (floatWindowMenuItem.Checked)
            {
                ShowFloatWindow();
            }
            else
            {
                HideFloatWindow();
            }
            Properties.Settings.Default.IsFloatWindowVisible = floatWindowMenuItem.Checked;
            Properties.Settings.Default.Save();
        }

        private void ShowFloatWindow()
        {
            floatWindow = new WallpaperControlWindow(mainForm, this);
            floatWindow.Show();
            floatWindowMenuItem.Checked = true;
        }

        private void HideFloatWindow()
        {
            floatWindow.Close();
            floatWindowMenuItem.Checked = false;
        }

        public void UpdateLoginMenuItem(string text, EventHandler clickEvent)
        {
            loginMenuItem.Text = text;
            loginMenuItem.Click -= mainForm.Login;
            loginMenuItem.Click -= mainForm.Logout;
            loginMenuItem.Click += clickEvent;
        }

        public void UpdateVersionMenuItemText(string text)
        {
            versionMenuItem.Text = text;
        }

        public void SetVersionMenuItemClickEvent(EventHandler eventHandler)
        {
            versionMenuItem.Click -= mainForm.CheckForUpdate;
            versionMenuItem.Click += eventHandler;
        }

        public void UpdateFloatWindowButtons(bool isPlaying)
        {
            floatWindow?.UpdatePlayPauseButtons(isPlaying);
        }

        public void ResumeWallpaperChange()
        {
            if (previousInterval > 0)
            {
                timer.Interval = previousInterval;
                timer.Start();
                mainForm.ShowNextImage();
                UpdateMenuItemsForInterval(previousInterval);
            }
            else
            {
                timer.Interval = mainForm.defaultInterval;
                timer.Start();
                mainForm.ShowNextImage();
                UpdateMenuItemsForInterval(mainForm.defaultInterval);
            }
        }

        public void PauseWallpaperChange()
        {
            timer.Stop();
            UpdateMenuItemsForInterval(0);
        }

        private void UpdateMenuItemsForInterval(int interval)
        {
            foreach (ToolStripMenuItem item in changeWallpaperMenu.DropDownItems)
            {
                item.Checked = (int)item.Tag == interval;
            }
        }

        private void ClearCache(object sender, EventArgs e)
        {
            var result = MessageBox.Show("确定要清理缓存吗？\n清理完成后软件会重新启动。", "确认清理缓存", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                ImageCacheManager cacheManager = new ImageCacheManager();
                timer.Stop();
                cacheManager.DeleteCacheDirectory();
                Program.Restart();
            }
        }

    public void AddManageWallpapersMenuItem()
    {
        if (manageWallpapersMenuItem == null)
        {
            manageWallpapersMenuItem = new ToolStripMenuItem("管理壁纸");
            manageWallpapersMenuItem.DropDownItems.Add(new ToolStripMenuItem("发布壁纸", null, (sender, e) => Process.Start(new ProcessStartInfo("https://creators-pc-cn.levect.com/Content/publishworks") { UseShellExecute = true })));
            manageWallpapersMenuItem.DropDownItems.Add(new ToolStripMenuItem("管理壁纸", null, (sender, e) => Process.Start(new ProcessStartInfo("https://creators-pc-cn.levect.com/Content/workmanagement") { UseShellExecute = true })));
            trayIcon.ContextMenuStrip.Items.Insert(1, manageWallpapersMenuItem);
        }
    }

    public void RemoveManageWallpapersMenuItem()
    {
        if (manageWallpapersMenuItem != null)
        {
            trayIcon.ContextMenuStrip.Items.Remove(manageWallpapersMenuItem);
            manageWallpapersMenuItem = null;
        }
    }

        private void ApplicationExit()
        {
            trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
