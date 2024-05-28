using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
//using SkiaSharp;

public class ImageCarousel : Form
{
    private PictureBox pictureBox;
    private Label label;
    private Button nextButton;
    private Button prevButton;
    //private Timer timer;
    private ImageCacheManager cacheManager;
    //private int currentIndex = 0;
    //private const string PositionFilePath = "currentPosition.txt";

    public ImageCarousel()
    {
        cacheManager = new ImageCacheManager();

        pictureBox = new PictureBox { Dock = DockStyle.Top, SizeMode = PictureBoxSizeMode.StretchImage, Height = 300 };
        label = new Label { Dock = DockStyle.Bottom, Height = 100, TextAlign = System.Drawing.ContentAlignment.MiddleCenter };

        nextButton = new Button { Text = "下一张", Dock = DockStyle.Right };
        nextButton.Click += NextButton_Click;

        prevButton = new Button { Text = "上一张", Dock = DockStyle.Left };
        prevButton.Click += PrevButton_Click;

        this.Controls.Add(pictureBox);
        this.Controls.Add(label);
        this.Controls.Add(nextButton);
        this.Controls.Add(prevButton);

        //timer = new Timer { Interval = 2000 };
        //timer.Tick += OnTimedEvent;

        //LoadCurrentPosition();
        Task.Run(() => InitializeCarouselAsync());
    }

    private async Task InitializeCarouselAsync()
    {
        await cacheManager.LoadImagesAsync();
        if (cacheManager.ImageInfos.Count > 0 && cacheManager.ImageCache.ContainsKey(cacheManager.ImageInfos[cacheManager.CurrentIndex].Url))
        {
            //UpdateImageDisplay(cacheManager.ImageCache[cacheManager.ImageInfos[cacheManager.CurrentIndex].Url]);
            //timer.Start();
        }
    }

    private void UpdateImageDisplay(ImageCacheManager.ImageCacheItem cacheItem)
    {
        try
        {
            //pictureBox.Image?.Dispose();
            //pictureBox.Image = LoadWebPImage(cacheItem.FilePath);
            SetWallpaper(cacheItem.FilePath);
            //label.Text = $"描述: {cacheItem.Info.Description}\n地址: {cacheItem.Info.Location}";
            //SaveCurrentPosition();
            cacheManager.SaveCurrentPosition(cacheManager.CurrentIndex);
        }
        catch (Exception ex)
        {
            // 图片文件可能损坏或格式不支持，处理异常并记录日志
            Console.WriteLine($"Failed to load image from {cacheItem.FilePath}. Exception: {ex.Message}");
        }
    }


    private void OnTimedEvent(object sender, EventArgs e)
    {
        ShowNextImage();
    }

    private void NextButton_Click(object sender, EventArgs e)
    {
        ShowNextImage();
    }

    private void PrevButton_Click(object sender, EventArgs e)
    {
        ShowPrevImage();
    }

    private void ShowNextImage()
    {
        if (cacheManager.ImageInfos.Count > 0)
        {
            cacheManager.CurrentIndex = (cacheManager.CurrentIndex + 1) % cacheManager.ImageInfos.Count;
            if (cacheManager.ImageCache.ContainsKey(cacheManager.ImageInfos[cacheManager.CurrentIndex].Url))
            {
                UpdateImageDisplay(cacheManager.ImageCache[cacheManager.ImageInfos[cacheManager.CurrentIndex].Url]);
            }

            // 仅在最后一张时调用同步方法
            if (cacheManager.CurrentIndex == cacheManager.ImageInfos.Count - 1)
            {
                Task.Run(() => cacheManager.LoadImagesAsync());
            }
        }
    }

    private void ShowPrevImage()
    {
        if (cacheManager.ImageInfos.Count > 0)
        {
            cacheManager.CurrentIndex = (cacheManager.CurrentIndex - 1 + cacheManager.ImageInfos.Count) % cacheManager.ImageInfos.Count;
            if (cacheManager.ImageCache.ContainsKey(cacheManager.ImageInfos[cacheManager.CurrentIndex].Url))
            {
                UpdateImageDisplay(cacheManager.ImageCache[cacheManager.ImageInfos[cacheManager.CurrentIndex].Url]);
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

}
