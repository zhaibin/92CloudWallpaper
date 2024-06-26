using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace _92CloudWallpaper
{
    public partial class WallpaperFormNew : Form
    {
        private WebView2 webView;

        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WS_EX_NOACTIVATE = 0x8000000;
        private const int GWL_EXSTYLE = -20;
        private const int HWND_BOTTOM = 1;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        public WallpaperFormNew(Screen screen)
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
            Console.WriteLine($"Form location: {this.Location}, Size: {this.Size}");

            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(webView);

            this.Load += async (s, e) =>
            {
                // Hide the form initially
                this.Opacity = 0;
                Console.WriteLine("Form loaded, starting WebView initialization...");

                await webView.EnsureCoreWebView2Async(null);
                webView.CoreWebView2.Navigate("https://www.example.com");

                webView.NavigationCompleted += (sender, args) =>
                {
                    // Show the form after the navigation is completed
                    this.Opacity = 1;
                    Console.WriteLine("WebView navigation completed.");
                };

                // Set the window style to make it a tool window and no activate
                int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
                Console.WriteLine($"Window long style set: {GetWindowLong(this.Handle, GWL_EXSTYLE)}");

                // Make the window transparent
                this.BackColor = System.Drawing.Color.Wheat;
                this.TransparencyKey = System.Drawing.Color.Wheat;

                // Get the Progman window
                IntPtr progman = FindWindow("Progman", null);
                Console.WriteLine($"Progman window handle: {progman}");

                // Send a message to Progman to spawn a WorkerW
                IntPtr result = IntPtr.Zero;
                SendMessageTimeout(progman, 0x052C, new IntPtr(0), IntPtr.Zero, SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out result);
                Console.WriteLine("Message sent to Progman to spawn WorkerW");

                // Get the WorkerW window
                IntPtr workerw = IntPtr.Zero;
                do
                {
                    workerw = FindWindowEx(IntPtr.Zero, workerw, "WorkerW", null);
                } while (workerw == IntPtr.Zero);

                ShowWindow(workerw, SW_HIDE);
                Console.WriteLine($"WorkerW window hidden: {workerw}");

                // Set the window to be a child of the Progman window (desktop icons window)
                IntPtr hWnd = this.Handle;
                SetParent(hWnd, progman);
                Console.WriteLine($"Window parent set to Progman: {hWnd}");

                // Set the window position to the bottom
                SetWindowPos(hWnd, (IntPtr)HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
                Console.WriteLine("Window position set to bottom");

                // Ensure the form's location and size are correct after setting parent
                this.Location = screen.Bounds.Location;
                this.Size = screen.Bounds.Size;
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

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [Flags]
        public enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,
            SMTO_BLOCK = 0x1,
            SMTO_ABORTIFHUNG = 0x2,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8,
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out IntPtr lpdwResult);
    }
}
