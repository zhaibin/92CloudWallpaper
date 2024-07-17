using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _92CloudWallpaper
{
    internal static class Program
    {
        private static string mutexName = "92CloudWallpaper";
        private static Mutex mutex;
        private static Main mainForm;
        private static string signalFileName = "92CloudWallpaper.signal";

        [STAThread]
        static void Main(string[] args)
        {
            bool createdNew;
            mutex = new Mutex(true, mutexName, out createdNew);

            if (!createdNew)
            {
                // 如果Mutex已经被创建，说明已有实例在运行，通过文件信号发送命令
                SendCommand();
                return;
            }

            Application.ApplicationExit += new EventHandler(OnApplicationExit);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0 && args[0] == "/hideMainPage")
            {
                mainForm = new Main(false);
            }
            else
            {
                mainForm = new Main(true);
            }

            // 启动文件信号监控
            StartSignalMonitor();
            Application.Run(mainForm);
        }
        private static void SendCommand()
        {
            try
            {
                File.Create(signalFileName).Dispose(); // 创建一个空文件作为信号
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send command: {ex.Message}");
            }
        }


        private static void StartSignalMonitor()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (File.Exists(signalFileName))
                    {
                        try
                        {
                            File.Delete(signalFileName); // 删除信号文件以重置信号
                            mainForm?.HandleCommand("ShowPreloadPage");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Signal monitor error: {ex.Message}");
                        }
                    }
                    Thread.Sleep(1000); // 每秒检查一次信号文件
                }
            });
        }


        private static void OnApplicationExit(object sender, EventArgs e)
        {
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex = null;
            }
        }

        public static void Restart()
        {
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex = null;
            }

            Process.Start(Application.ExecutablePath, "/restart");

            Application.Exit();
        }
    }
}
