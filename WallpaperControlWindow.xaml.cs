using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace _92CloudWallpaper
{
    public partial class WallpaperControlWindow : Window
    {
        public Main MainForm { get; set; }
        private readonly MenuHandler menuHandler;
        private readonly WeatherService weatherService;
        private DispatcherTimer weatherUpdateTimer;
        private DispatcherTimer imageInfoUpdateTimer;

        public WallpaperControlWindow(Main mainForm, MenuHandler menuHandler)
        {
            InitializeComponent();
            MainForm = mainForm;

            this.menuHandler = menuHandler;
            this.Loaded += Window_Loaded;
            this.Closing += Window_Closing;
            if (MainForm.savedInterval == 0)
            {
                PauseButton.Visibility = Visibility.Collapsed;
                PlayButton.Visibility = Visibility.Visible;
            }
            else
            {
                PauseButton.Visibility = Visibility.Visible;
                PlayButton.Visibility = Visibility.Collapsed;
            }
            weatherService = new WeatherService();
            InitializeWeatherUpdateTimer();
            InitializeImageInfoUpdateTimer();
        }

        private void InitializeWeatherUpdateTimer()
        {
            weatherUpdateTimer = new DispatcherTimer();
            weatherUpdateTimer.Interval = TimeSpan.FromMinutes(30); // 设置为30分钟
            weatherUpdateTimer.Tick += async (sender, e) => await WeatherUpdateTimer_Tick(sender, e);
            weatherUpdateTimer.Start();
        }

        private void InitializeImageInfoUpdateTimer()
        {
            imageInfoUpdateTimer = new DispatcherTimer();
            imageInfoUpdateTimer.Interval = TimeSpan.FromSeconds(2); // 每2秒钟更新一次
            imageInfoUpdateTimer.Tick += (sender, e) => DisplayImageInfo();
            imageInfoUpdateTimer.Start();
        }

        private async Task WeatherUpdateTimer_Tick(object sender, EventArgs e)
        {
            await LoadWeatherPage();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 读取上次窗口位置
            if (!double.IsNaN(Properties.Settings.Default.LastWindowLeft) && !double.IsNaN(Properties.Settings.Default.LastWindowTop))
            {
                this.Left = Properties.Settings.Default.LastWindowLeft;
                this.Top = Properties.Settings.Default.LastWindowTop;
            }
            else
            {
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var windowWidth = this.Width;
                this.Left = screenWidth - windowWidth - 30;
                this.Top = 30;
            }
            ShowInTaskbar = false;

            // 加载天气信息
            await LoadWeatherPage();

            
        }

        private async Task LoadWeatherPage()
        {
            // 打印当前线程ID，用于调试
            Console.WriteLine($"Weather Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            var time = DateTime.Now;
            Console.WriteLine($"WeatherWindow : {time}");
            var weatherResult = await weatherService.GetWeatherAsync();
            if (weatherResult.Status == 1)
            {
                Dispatcher.Invoke(() => {
                    CityLabel.Text = weatherResult.City;
                    WeatherText.Text = weatherResult.WeatherDescription + "\n"
                        + weatherResult.Temperature + "(" + weatherResult.FeelsLikeTemperature + ")\n"
                        + weatherResult.WindDirection + " " + weatherResult.WindSpeed + "\n"
                        + weatherResult.Visibility + "\n";
                    WeatherIcon.Text = weatherResult.WeatherIcon;
                    // 格式化时间并添加 emoji 刷新文案
                    DateTime obsDateTime;
                    if (DateTime.TryParse(weatherResult.LocalObsDateTime, out obsDateTime))
                    {
                        ObsDateTime.Text = obsDateTime.ToString("MM-dd HH:mm");
                    }
                    else
                    {
                        ObsDateTime.Text = weatherResult.LocalObsDateTime;
                    }
                });
            }
            else
            {
                Dispatcher.Invoke(() => {
                    CityLabel.Text = weatherResult.StatusDesc;
                });
            }
        }

        public void DisplayImageInfo()
        {
            if (this.Dispatcher.CheckAccess())
            {
                Console.WriteLine($"InfoWindow Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                var time = DateTime.Now;
                Console.WriteLine($"InfoWindow : {time}");
                var imageInfo = MainForm.currentImageInfo; // 获取当前图片信息
                if(imageInfo != null) {
                    // 更新界面上的标签或其他控件以显示图片信息
                    if(imageInfo.ShootTime != "") { 
                        DateTime shootTime;
                        if (DateTime.TryParse(imageInfo.ShootTime, out shootTime))
                        {
                            ShootTime.Text = shootTime.ToString("YYYY-MM-dd");
                        }
                        else
                        {
                            ShootTime.Text = imageInfo.ShootTime;
                        }
                        ShootTime_Label.Text = "拍摄时间";
                    }
                    if (imageInfo.ShootAddr != "")
                    {
                        ShootAddr_Label.Text = "拍摄地点";
                        ShootAddr.Text = imageInfo.ShootAddr;
                    }
                    AuthorUrl.Source = new BitmapImage(new Uri(imageInfo.AuthorUrl));
                    PicContent.Text = imageInfo.Description;
                    AuthorName.Text = imageInfo.AuthorName;
                    //PicContent.Text = currTime.ToString();
                    //Console.WriteLine($"AuthorUrl: {imageInfo.AuthorUrl}");
                }
                else
                {
                    ShootTime.Text = "";
                    ShootAddr.Text = "";
                    AuthorUrl.Source = null;
                    PicContent.Text = "";
                    AuthorName.Text = "";
                    Console.WriteLine($"InfoWindow 2: {time}");
                }
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var imageInfo = MainForm.currentImageInfo; // 获取当前图片信息
                        // 更新界面上的标签或其他控件以显示图片信息
                        ShootTime.Text = imageInfo.ShootTime;
                        ShootAddr.Text = imageInfo.ShootAddr;
                        AuthorUrl.Source = new BitmapImage(new Uri(imageInfo.AuthorUrl));
                        PicContent.Text = imageInfo.Description;
                        AuthorName.Text = imageInfo.AuthorName;

                        // 日志输出以调试
                        Console.WriteLine($"ShootTime: {imageInfo.ShootTime}");
                        Console.WriteLine($"ShootAddr: {imageInfo.ShootAddr}");
                        Console.WriteLine($"AuthorUrl: {imageInfo.AuthorUrl}");
                        Console.WriteLine($"PicContent: {imageInfo.Description}");
                        Console.WriteLine($"AuthorName: {imageInfo.AuthorName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating UI: {ex.Message}");
                    }
                });
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 保存窗口位置
            Properties.Settings.Default.LastWindowLeft = this.Left;
            Properties.Settings.Default.LastWindowTop = this.Top;
            Properties.Settings.Default.Save();
            ShowInTaskbar = false; // 窗口关闭时隐藏任务栏图标
        }
        private async void ObsDateTime_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            await LoadWeatherPage();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            MainForm.ShowPrevImage();
            DisplayImageInfo();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            MainForm.ShowNextImage();
            DisplayImageInfo();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            MainForm.PauseWallpaperChange();
            menuHandler.PauseWallpaperChange();
            PauseButton.Visibility = Visibility.Collapsed;
            PlayButton.Visibility = Visibility.Visible;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            menuHandler.ResumeWallpaperChange();
            PauseButton.Visibility = Visibility.Visible;
            PlayButton.Visibility = Visibility.Collapsed;
        }

        public void UpdatePlayPauseButtons(bool isPlaying)
        {
            if (isPlaying)
            {
                PauseButton.Visibility = Visibility.Visible;
                PlayButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                PauseButton.Visibility = Visibility.Collapsed;
                PlayButton.Visibility = Visibility.Visible;
            }
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
