using System;
using System.Collections.Generic;
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
        private static string mutexName = "UniqueAppNameMutex";
        private static Mutex mutex;

        [STAThread]
        static void Main()
        {
            bool createdNew;
            mutex = new Mutex(true, mutexName, out createdNew);

            if (!createdNew)
            {
                // 如果Mutex已经被创建，说明已有实例在运行，显示提示并退出
                MessageBox.Show("应用程序已在运行中。在右下角托盘可以找到我。");
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
            // 释放Mutex
            mutex.ReleaseMutex();
        }
    }
}
