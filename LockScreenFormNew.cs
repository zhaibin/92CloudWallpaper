using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace _92CloudWallpaper
{
    public partial class LockScreenFormNew : Form
    {
        private WebView2 webView;

        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WS_EX_NOACTIVATE = 0x8000000;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        public LockScreenFormNew(Screen screen)
        {
            InitializeForm(screen);
        }

        private void InitializeForm(Screen screen)
        {
            Console.WriteLine($"Initializing form on screen: {screen.DeviceName}");
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = screen.Bounds.Location;
            this.Size = screen.Bounds.Size;
            this.Visible = false; // 初始化时不可见

            // 启用双缓冲减少闪烁
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(webView);

            this.Load += async (s, e) =>
            {
                Console.WriteLine("Form loaded, starting WebView initialization...");
                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.Navigate("https://www.example.com");

                // 设置窗口样式以使其成为工具窗口和不激活窗口
                int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
                Console.WriteLine($"Window long style set: {GetWindowLong(this.Handle, GWL_EXSTYLE)}");

                // 设置窗口透明
                this.BackColor = System.Drawing.Color.Wheat;
                this.TransparencyKey = System.Drawing.Color.Wheat;

                // 设置窗口为桌面的子窗口
                IntPtr hWnd = this.Handle;
                IntPtr hWndDesktop = GetDesktopWindow();
                SetParent(hWnd, hWndDesktop);
                Console.WriteLine($"Window parent set to desktop: {hWnd}");

                // 确保窗体在第二块屏幕上全屏显示
                this.Location = screen.Bounds.Location;
                this.Size = screen.Bounds.Size;
                this.WindowState = FormWindowState.Maximized;
                Console.WriteLine($"Final form location: {this.Location}, Size: {this.Size}");
            };
        }

        public void ChangeUrl(string url)
        {
            if (webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.Navigate(url);
                Console.WriteLine($"WebView URL changed to: {url}");
            }
        }

        public void ShowForm()
        {
            if (!this.Visible)
            {
                this.Visible = true;
                this.BringToFront();
                Console.WriteLine("Form is now visible.");
            }
        }
    }
}
