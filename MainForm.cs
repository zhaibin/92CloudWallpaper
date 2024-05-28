using System;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using MyAppNamespace;

namespace MyAppNamespace
{
    public partial class MainForm : Form
    {
        private WpfWindow wpfWindow;
        private System.Windows.Forms.Timer mainFormTimer;

        public MainForm()
        {
            InitializeComponent();

            // 创建并显示WPF窗口
            wpfWindow = new WpfWindow();
            ElementHost.EnableModelessKeyboardInterop(wpfWindow); // 允许WPF窗口和WinForms进行键盘交互
            wpfWindow.Show();

            // 初始化MainForm定时器
            mainFormTimer = new System.Windows.Forms.Timer();
            mainFormTimer.Interval = 2000; // 每2秒触发一次
            mainFormTimer.Tick += MainFormTimer_Tick;
            mainFormTimer.Start();
        }

        private void MainFormTimer_Tick(object sender, EventArgs e)
        {
            // 从WinForms更新WPF窗口的UI，显示当前时间
            string updateTime = DateTime.Now.ToString("HH:mm:ss");
            wpfWindow.UpdateMainFormLabel($"Updated from MainForm: {updateTime}");
            Console.WriteLine("MainForm Timer Thread ID: " + Thread.CurrentThread.ManagedThreadId);
        }
    }
}
