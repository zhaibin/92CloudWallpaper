using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

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
        private readonly int[] times = { 0, 60000, 600000, 1800000, 3600000, 86400000 }; // 修改1分钟的时间为60000
        private readonly string[] intervals = { "暂停", "1 分钟", "10 分钟", "半小时", "1 小时", "1 天" };
        private int previousInterval;

        public MenuHandler(Main mainForm, NotifyIcon trayIcon, Timer timer)
        {
            this.mainForm = mainForm;
            this.trayIcon = trayIcon;
            this.timer = timer;
            // 初始化 WallpaperControlWindow
            floatWindow = new WallpaperControlWindow(mainForm, this);

            this.previousInterval = mainForm.savedInterval;
            InitializeTrayIcon();

            // 根据保存的状态初始化悬浮窗
            if (Properties.Settings.Default.IsFloatWindowVisible)
            {
                ShowFloatWindow();
            }
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
                menuItem.Checked = i == Array.IndexOf(times, mainForm.savedInterval); // 根据保存的索引设置选中状态
                changeWallpaperMenu.DropDownItems.Add(menuItem);
            }

            if (Properties.Settings.Default.UserId == 0)
            {
                loginMenuItem = new ToolStripMenuItem("登录", null, mainForm.Login);
            }
            else
            {
                loginMenuItem = new ToolStripMenuItem("登出", null, mainForm.Logout);
            }
            trayMenu.Items.Add(loginMenuItem);

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

            trayMenu.Items.Add(autoStartMenuItem);
            trayMenu.Items.Add(floatWindowMenuItem);
            trayMenu.Items.Add(changeWallpaperMenu);
            versionMenuItem = new ToolStripMenuItem($"版本 {Main.CurrentVersion}");
            versionMenuItem.Click += mainForm.CheckForUpdate;

            trayMenu.Items.Add(versionMenuItem);
            trayMenu.Items.Add("退出程序", null, (sender, e) => ApplicationExit());

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
            //if (floatWindow == null || !floatWindow.IsVisible)
            {
                floatWindow = new WallpaperControlWindow(mainForm, this);
                
                floatWindow.Show();
                floatWindowMenuItem.Checked = true;
            }
        }

        private void HideFloatWindow()
        {
            //if (floatWindow != null && floatWindow.IsVisible)
            {
                floatWindow.Close();
                floatWindowMenuItem.Checked = false;
            }
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

        private void ApplicationExit()
        {
            floatWindow?.Close();
            Application.Exit();
        }
    }
}
