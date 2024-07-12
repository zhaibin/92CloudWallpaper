using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


namespace _92CloudWallpaper
{
    public partial class DesktopWindow : Window
    {
        public Main MainForm { get; set; }
        //private readonly MenuHandler menuHandler;
        //private DispatcherTimer imageInfoUpdateTimer;
        private BitmapImage currentBitmap;
        public DesktopWindow(Main mainForm)
        {
            var funcMessage = "浮窗初始化";
            Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");
            InitializeComponent();
            
            MainForm = mainForm;

            //this.menuHandler = menuHandler;
            this.Loaded += Window_Loaded;
            this.Closing += Window_Closing;
            
            //weatherService = new WeatherService();
            //InitializeWeatherUpdateTimer();
            //InitializeImageInfoUpdateTimer();
            Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var funcMessage = "浮窗加载";
            Console.WriteLine($"{funcMessage}开始：{DateTime.Now}");
            // 读取上次窗口位置
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
            //DisplayImageInfo();
            Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");

        }

        public void DisplayImageInfo()
        {
            var funcMessage = "浮窗信息显示";
            Console.WriteLine($"{funcMessage}开始：{MainForm.wallpaperCount} {DateTime.Now}");
            if (this.Dispatcher.CheckAccess())
            {
                //Console.WriteLine($"InfoWindow Thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                var time = DateTime.Now;

                if (MainForm.wallpaperCount > 0)
                {
                    //CurrentWallpaper_Label.Text = "当前壁纸";
                    //CurrentWallpaper.Text = $"第 {(MainForm.currentWallpaperIndex + 1)} 张";
                    //WallpaperCount_Label.Text = "共有壁纸";
#if DEBUG
                    //WallpaperCount.Text = $"{(MainForm.currentWallpaperIndex + 1)} / {MainForm.wallpaperCount}";
#endif
                }
                var imageInfo = MainForm.currentImageInfo; // 获取当前图片信息
                if (imageInfo != null) 
                {
                    Console.WriteLine($"作者 {imageInfo.AlbumName} || 介绍 {imageInfo.Description}");
                    // 更新界面上的标签或其他控件以显示图片信息
                    //imageInfo.ShootTime = "2020-04-05 12:20:20";
                    //imageInfo.ShootAddr = "北京 西城";
                    //imageInfo.Description = "北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城北京 西城";
                    /*
                    if (imageInfo.ShootTime != "" && imageInfo.ShootAddr != null) 
                    { 
                        DateTime shootTime;
                        if (DateTime.TryParse(imageInfo.ShootTime, out shootTime))
                        {
                            ShootTime.Text = shootTime.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            ShootTime.Text = imageInfo.ShootTime;
                        }
                        ShootTime_Label.Text = "拍摄日期";
                    }
                    else
                    {
                        ShootTime_Label.Text = "";
                        ShootTime.Text = "";
                    }
                    if (imageInfo.ShootAddr != "" && imageInfo.ShootAddr != null)
                    {
                        ShootAddr_Label.Text = "拍摄地点";
                        ShootAddr.Text = imageInfo.ShootAddr;
                    }
                    else
                    {
                        ShootAddr_Label.Text = "";
                        ShootAddr.Text = "";
                    }
                    */
                    //AuthorUrl.Source = ImageCacheManager.GetImage(imageInfo.AuthorUrl);

                    //AuthorName.Text = imageInfo.AuthorName;
                    //SetBackgroundImage(MainForm.currentWallpaperFilePath);
                    //PicContent.Text = currTime.ToString();
                    //Console.WriteLine($"AuthorUrl: {imageInfo.AuthorUrl}");
                    if(imageInfo.Description != "")
                    {
                        PicContent.Text = imageInfo.Description;
                    }
                    else
                    {
                        PicContent.Text = $"「{imageInfo.AlbumName}」";
                    }
                    
                }
                else
                {
                    //ShootTime.Text = "";
                    //ShootAddr.Text = "";
                    //AuthorUrl.Source = null;
                    PicContent.Text = "在壁纸商店订阅壁纸或本地上传自己喜欢的壁纸，开启云壁纸";
                    //AuthorName.Text = "";
                    //Console.WriteLine($"InfoWindow 2: {time}");
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
                        //ShootTime.Text = imageInfo.ShootTime;
                        //ShootAddr.Text = imageInfo.ShootAddr;
                        //AuthorUrl.Source = new BitmapImage(new Uri(imageInfo.AuthorUrl));
                        PicContent.Text = imageInfo.Description;
                        //AuthorName.Text = imageInfo.AuthorName;

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
            Console.WriteLine($"{funcMessage}结束：{DateTime.Now}");
        }
        private void SetBackgroundImage(string imagePath)
        {
            // 检查图片文件是否存在
            if (File.Exists(imagePath))
            {
                // 释放之前的BitmapImage
                if (currentBitmap != null)
                {
                    currentBitmap = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers(); // 确保垃圾回收完成
                }

                // 创建新的BitmapImage
                currentBitmap = new BitmapImage();
                currentBitmap.BeginInit();
                currentBitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                currentBitmap.DecodePixelHeight = (int)desktopWindowGrid.ActualHeight;
                currentBitmap.DecodePixelWidth = (int)desktopWindowGrid.ActualWidth;
                currentBitmap.CacheOption = BitmapCacheOption.OnLoad; // 确保图片在加载时完成缓存
                currentBitmap.EndInit();

                // 将BitmapImage赋值给ImageBrush
                backgroundBrush.ImageSource = currentBitmap;
            }
            else
            {
                //MessageBox.Show("Image file not found: " + imagePath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        /*
        private async void ObsDateTime_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            await LoadWeatherPage();
        }
        */
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            MainForm.ShowPreloadPage(InfoHelper.Urls.Store);
        }
        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            MainForm.ShowPrevImage();
            //DisplayImageInfo();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            MainForm.ShowNextImage();
            //DisplayImageInfo();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            MainForm.PauseWallpaperChange();
            //menuHandler.PauseWallpaperChange();
            PauseButton.Visibility = Visibility.Collapsed;
            PlayButton.Visibility = Visibility.Visible;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            MainForm.ResumeWallpaperChange();
            //menuHandler.ResumeWallpaperChange();
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
        private void OpenCalculator(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("calc.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法打开计算器: " + ex.Message);
            }
        }

        private void OpenNotepad(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("notepad.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法打开记事本: " + ex.Message);
            }
        }

       

        
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
