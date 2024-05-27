using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace _92CloudWallpaper
{
    public partial class WallpaperControlWindow : Window
    {
        public Main MainForm { get; set; }
        private readonly MenuHandler menuHandler;
        private readonly WeatherService weatherService;

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
            LoadWeatherPage();

        }

        private  void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 读取上次窗口位置
            if (!double.IsNaN(Properties.Settings.Default.LastWindowLeft) && !double.IsNaN(Properties.Settings.Default.LastWindowTop))
            {
                this.Left = Properties.Settings.Default.LastWindowLeft;
                this.Top = Properties.Settings.Default.LastWindowTop;
            }
            else
            {
                // 如果没有保存的位置，则设置为右上角
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var windowWidth = this.Width;
                this.Left = screenWidth - windowWidth - 30;
                this.Top = 30;
            }
            //MainForm.savedInterval
            ShowInTaskbar = false;
            //weatherService = new WeatherService();
            


        }
        private async void LoadWeatherPage()
        {
            var weatherResult = await weatherService.GetWeatherAsync();
            if (weatherResult.Status == 1)
            {
                CityLabel.Text = weatherResult.City;
                WeatherText.Text = weatherResult.WeatherDescription + "\n"
                    + weatherResult.Temperature + "(" + weatherResult.FeelsLikeTemperature + ")\n"
                    + weatherResult.WindDirection + " " + weatherResult.WindSpeed + "\n"
                    + weatherResult.Visibility + "\n";
                    //+ "UV:" + weatherResult.UVIndex;
                WeatherIcon.Text = weatherResult.WeatherIcon;
            }
            else
            {
                CityLabel.Text = weatherResult.StatusDesc;
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
        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            MainForm.ShowPrevImage();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            MainForm.ShowNextImage();
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
