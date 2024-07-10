using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _92CloudWallpaper
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        // 唯一的Mutex名称
        private static string mutexName = "92CloudWallpaper";
        private static Mutex mutex;
        private static Main mainForm;
       
        [STAThread]
        static void Main(string[] args)
        {

            bool createdNew;
            mutex = new Mutex(true, mutexName, out createdNew);

            if (!createdNew)
            {
                // 如果Mutex已经被创建，说明已有实例在运行，显示提示并退出
                if (args.Length == 0 || args[0] != "/restart")
                {
                    MessageBox.Show("应用程序已在运行中。在右下角托盘可以找到我。");
                    //mainForm.ShowMainPage();
                    return;
                }
            }

            Application.ApplicationExit += new EventHandler(OnApplicationExit);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Wallpaper.SetupWallpapersForAllScreens();

            // 检查命令行参数
            //if (args.Length > 0 && args[0] == "openStore")
            //{
            //    mainForm.ShowPreloadPage(InfoHelper.Urls.Store);
            //}
            if (args.Length > 0 && args[0] == "/hideMainPage")
            {
                mainForm = new Main(false);
                //mainForm.ShowPreloadPage(InfoHelper.Urls.Store);
            }
            else
            {
                mainForm = new Main(true);
            }
            


            Application.Run(mainForm);
            
            
            // 在此处不再调用 mutex.ReleaseMutex();
        }

        private static void OnApplicationExit(object sender, EventArgs e)
        {
            // 释放Mutex
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex = null;
            }
            //mainForm.CleanupBeforeExit();
        }

        public static void Restart()
        {
            // 释放当前的Mutex
            if (mutex != null)
            {
                mutex.ReleaseMutex();
                mutex = null;
            }

            // 启动新实例，传递重启参数
            Process.Start(Application.ExecutablePath, "/restart");

            // 退出当前实例
            Application.Exit();
        }

    }
}
