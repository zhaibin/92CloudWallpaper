using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MyAppNamespace
{
    public partial class WpfWindow : Window
    {
        private DispatcherTimer wpfTimer;
        private static readonly HttpClient httpClient = new HttpClient();

        public WpfWindow()
        {
            InitializeComponent();

            // 初始化WPF定时器
            wpfTimer = new DispatcherTimer();
            wpfTimer.Interval = TimeSpan.FromSeconds(1); // 每秒触发一次
            wpfTimer.Tick += async (sender, e) => await WpfTimer_Tick();
            wpfTimer.Start();
        }

        private async Task WpfTimer_Tick()
        {
            try
            {
                string url = "http://ip-api.com/json/";
                string response = await httpClient.GetStringAsync(url);
                UpdateWeatherLabel(response);
            }
            catch (Exception ex)
            {
                UpdateWeatherLabel($"Error: {ex.Message}");
            }
            UpdateThreadIdLabel("WPF Timer Thread ID: " + Thread.CurrentThread.ManagedThreadId);
        }

        public void UpdateWeatherLabel(string text)
        {
            if (this.Dispatcher.CheckAccess())
            {
                weatherLabel.Content = text;
            }
            else
            {
                this.Dispatcher.Invoke(() => weatherLabel.Content = text);
            }
        }

        public void UpdateMainFormLabel(string text)
        {
            if (this.Dispatcher.CheckAccess())
            {
                mainFormLabel.Content = text;
            }
            else
            {
                this.Dispatcher.Invoke(() => mainFormLabel.Content = text);
            }
        }

        public void UpdateThreadIdLabel(string text)
        {
            if (this.Dispatcher.CheckAccess())
            {
                threadIdLabel.Content = text;
            }
            else
            {
                this.Dispatcher.Invoke(() => threadIdLabel.Content = text);
            }
        }
    }
}
