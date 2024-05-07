using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using static _92CloudWallpaper.ApiRequestHandler;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Security.Policy;


namespace _92CloudWallpaper
{
    public partial class Form1 : Form
    {
        private NotifyIcon trayIcon;
        private Timer timer;
        //private string imageUrl = "https://source.unsplash.com/user/erondu/1600x900"; // 替换为你的图片URL
        private int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        private int screenHeight = Screen.PrimaryScreen.Bounds.Height;
        private string appImageUrl = "http://cnapi.levect.com/v1/photoFrame/imageList";
        private ToolStripMenuItem loginMenuItem;

        public Form1()
        {
            InitializeComponent();
            InitializeTrayIcon();
            InitializeTimer(3600000);
            //MenuTimer();
            _ = DownloadAndSetWallpaper();
        }
        private void InitializeTrayIcon()
        {

            // 创建托盘菜单项和子菜单
            ContextMenuStrip trayMenu = new ContextMenuStrip();
            ToolStripMenuItem changeWallpaperMenu = new ToolStripMenuItem("更换壁纸");
            
            // 更换壁纸时间选项
            string[] intervals = { "暂停", "10 秒","1 分钟", "1 小时", "1 天", "立即更换" };
            int[] times = { 0, 10000, 60000, 3600000, 86400000 };

            for (int i = 0; i < intervals.Length; i++)
            {
                var menuItem = new ToolStripMenuItem(intervals[i], null, ChangeWallpaperEvent);
                if (i < times.Length) menuItem.Tag = times[i];
                if (i == 3) menuItem.Checked = true;  // 默认选中每小时
                changeWallpaperMenu.DropDownItems.Add(menuItem);
            }
            if(Properties.Settings.Default.UserId == 0)
            {
                loginMenuItem = new ToolStripMenuItem("登录", null, Login);
            }
            else
            {
                loginMenuItem = new ToolStripMenuItem("登出", null, Logout);
            }
            trayMenu.Items.Add(loginMenuItem);
            trayMenu.Items.Add(changeWallpaperMenu);
            trayMenu.Items.Add("退出程序", null, (sender, e) => Application.Exit());

            // 加载图标资源
            Icon trayIconImage = LoadIconFromResource("_92CloudWallpaper.7418_logo32.png");

            // 设置托盘图标
            trayIcon = new NotifyIcon()
            {
                Icon = trayIconImage,
                ContextMenuStrip = trayMenu,
                Visible = true,
                Text = "92云壁纸"
            };
            trayIcon.MouseClick += TrayIcon_MouseClick;
        }
        //左键单击托盘图标即可换图
        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            //MessageBox.Show("托盘图标被双击了!");
            _ = DownloadAndSetWallpaper();
        }
        private Icon LoadIconFromResource(string resourceName)
        {
            // 访问嵌入资源
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    MessageBox.Show("Error loading icon resource.");
                    return null;
                }
                // 读取图片并创建图标
                Image image = Image.FromStream(resourceStream);
                Bitmap bitmap = new Bitmap(image);
                return Icon.FromHandle(bitmap.GetHicon());
            }
        }
        private void ChangeWallpaperEvent(object sender, EventArgs e)
        {
            var clickedItem = sender as ToolStripMenuItem;

            // Manage check states
            foreach (ToolStripMenuItem item in clickedItem.GetCurrentParent().Items)
            {
                item.Checked = item == clickedItem;
            }

            if (clickedItem.Text == "立即更换")
            {
                _ = DownloadAndSetWallpaper();
                return;
            }

            int interval = (int)clickedItem.Tag;
            if (interval > 0)
            {
                timer.Interval = interval;
                timer.Start();
                //DownloadAndSetWallpaper(imageUrl);
                _ = DownloadAndSetWallpaper();
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
            timer.Tick += (sender, e) => DownloadAndSetWallpaper();
            timer.Start();
        }
        
        private void Logout(object sender, EventArgs e)
        {
            // 执行登出前的确认
            if (MessageBox.Show("您确定要登出吗？", "确认登出", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                AttemptLogout(); // 调用登出方法

                // 更新菜单项为“登录”
                loginMenuItem.Text = "登录";
                loginMenuItem.Click -= Logout;
                loginMenuItem.Click += Login;

                

                // 显示登出成功的信息
                //MessageBox.Show("您已成功登出。", "登出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AttemptLogout()
        {
            // 添加登出逻辑
            // 例如清除用户会话、删除安全令牌、清理用户相关的临时数据等
            Properties.Settings.Default.UserId = 0;
            Properties.Settings.Default.Save();
            GlobalData.UserId = 0;
            GlobalData.LoginFlag = 0;
        }
        private void Login(object sender, EventArgs e)
        {
            ShowLoginForm();
        }
        private void LoginSuccess()
        {
            loginMenuItem.Text = "登出";
            loginMenuItem.Click -= Login;
            loginMenuItem.Click += Logout;
            _ = DownloadAndSetWallpaper();

        }
        private void ShowLoginForm()
        {
            using (LoginForm loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    // 更新主窗口的菜单项等
                    //MessageBox.Show("Logged in successfully.");
                    LoginSuccess();
                }
            }
        }
        
        
        private async Task DownloadAndSetWallpaper()
        {
            string localPath = Path.Combine(Path.GetTempPath(), "currentWallpaper.jpg");

            var apiHandler = new ApiRequestHandler();
            var body = new
            {
                height = screenHeight,
                pageIndex = GlobalData.PageIndex,
                pageSize = 1,
                userId = GlobalData.UserId,
                //userId = 23,
                width = screenWidth,
            };
            //lx userid 11581088,23
            var response = await apiHandler.SendApiRequestAsync(appImageUrl, body);
            Console.WriteLine(response);
            JObject json = JObject.Parse(response);
            string url;
            try
            {
                 url = (string)json["body"]["list"][0]["url"];
            }
            catch (Exception)
            {
                url = "";
            }
            Console.WriteLine(url);
            if ( url != "")
            {
                GlobalData.PageIndex++;
                Console.WriteLine(GlobalData.PageIndex);
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        client.DownloadFile(url, localPath);
                        SetWallpaper(localPath);
                       // await SetLockScreen.SetImageAsync(localPath);
                        //return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred: {ex.Message}");
                        //return false;
                    }
                }
            }
            else
            {
                GlobalData.PageIndex = 1;
                _ = DownloadAndSetWallpaper();
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
