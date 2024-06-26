using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace _92CloudWallpaper
{
    public partial class MainWebView : Form
    {
        private static MainWebView instance;
        private WebView2 webView;
        //private HttpListener listener;
        //private int port;
        private Main mainForm;
        private Dictionary<string, bool> preloadedPages;

        private MainWebView(Main mainForm)
        {
            InitializeComponent();
            this.mainForm = mainForm;
            InitializeWebView();
            preloadedPages = new Dictionary<string, bool>();
        }

        public static MainWebView Instance(Main mainForm)
        {
            if (instance == null || instance.IsDisposed)
            {
                instance = new MainWebView(mainForm);
            }
            return instance;
        }

        private async void InitializeWebView()
        {
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(webView);
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            InjectDisableSelectionScript();
            InjectDisableRightClickScript();
            InjectDisableDebuggingScript();
            webView.CoreWebView2.AddHostObjectToScript("external", new ScriptCallback(this));
        }

        public async void ClearCookies()
        {
            if (webView != null && webView.CoreWebView2 != null)
            {
                webView.CoreWebView2.CookieManager.DeleteAllCookies();
                await webView.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.Cookies);
            }
        }

        private async void InjectDisableSelectionScript()
        {
            if (webView.CoreWebView2 != null)
            {
                string script = @"
                    document.addEventListener('DOMContentLoaded', (event) => {
                        const style = document.createElement('style');
                        style.innerHTML = '* { user-select: none; }';
                        document.head.appendChild(style);
                    });
                ";
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
            }
        }

        private async void InjectDisableRightClickScript()
        {
            if (webView.CoreWebView2 != null)
            {
                string script = @"
                    document.addEventListener('DOMContentLoaded', (event) => {
                        document.addEventListener('contextmenu', (e) => {
                            e.preventDefault();
                        });
                    });
                ";
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
            }
        }

        private async void InjectDisableDebuggingScript()
        {
            if (webView.CoreWebView2 != null)
            {
                string script = @"
                    document.addEventListener('DOMContentLoaded', (event) => {
                        document.addEventListener('keydown', (e) => {
                            if (e.key === 'F12' || (e.ctrlKey && e.shiftKey && e.key === 'I')) {
                                e.preventDefault();
                            }
                        });
                    });
                ";
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
            }
        }

        public async void PreloadPage(string url)
        {
            if (!preloadedPages.ContainsKey(url))
            {
                await EnsureWebViewInitialized();
                webView.CoreWebView2.Navigate(url);
                preloadedPages[url] = true;
            }
        }

        public void ShowPreloadedPage(string url)
        {
            if (preloadedPages.ContainsKey(url))
            {
                webView.CoreWebView2.Navigate(url);
                this.Show();
            }
            else
            {
                PreloadPage(url);
                this.Show();
            }
        }

        private async Task EnsureWebViewInitialized()
        {
            if (webView.CoreWebView2 == null)
            {
                await webView.EnsureCoreWebView2Async(null);
            }
        }

        public static void PreloadAndShow(string url, Main mainForm)
        {
            MainWebView mainWebView = MainWebView.Instance(mainForm);
            mainWebView.PreloadPage(url);
        }

        private void MainWebView_Load(object sender, EventArgs e)
        {
            // SetFormIcon();
        }

        public void ReceiveUserData(int userId, string token)
        {
            Properties.Settings.Default.UserId = userId;
            Properties.Settings.Default.Token = token;
            Properties.Settings.Default.Save();
            GlobalData.UserId = userId;
            GlobalData.Token = token;
            Console.WriteLine($"Received UserId: {userId}, Token: {token}");
            mainForm.LoginSuccess();
            // 处理接收到的用户数据，如导航到指定页面等
        }

        public void WebLogout()
        {
            mainForm.AttemptLogout();
        }
        public async void ImagesAsync()
        {
            await Task.Run(() => mainForm.ImagesAsync());
        }
    }

    [ComVisible(true)]
    public class ScriptCallback
    {
        private MainWebView mainWebView;

        public ScriptCallback(MainWebView mainWebView)
        {
            this.mainWebView = mainWebView;
        }

        public void SetUserData(int userId, string token)
        {
            mainWebView.ReceiveUserData(userId, token);
        }

        public void Logout()
        {
            mainWebView.WebLogout();
        }

        public async void ImagesAsync()
        {
            await Task.Run(() => mainWebView.ImagesAsync());
        }
    }
}
