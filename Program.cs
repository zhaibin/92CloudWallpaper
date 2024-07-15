using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
        private static string pipeName = "92CloudWallpaperPipe";

        [STAThread]
        static void Main(string[] args)
        {
            bool createdNew;
            mutex = new Mutex(true, mutexName, out createdNew);

            if (!createdNew)
            {
                // 如果Mutex已经被创建，说明已有实例在运行，通过命名管道发送命令
                SendCommand("ShowPreloadPage");
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

            // 启动管道服务器
            StartPipeServer();

            Application.Run(mainForm);
        }

        private static void SendCommand(string command)
        {
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
                {
                    pipeClient.Connect(1000); // 尝试连接到服务器
                    using (StreamWriter writer = new StreamWriter(pipeClient))
                    {
                        writer.AutoFlush = true;
                        writer.WriteLine(command);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send command: {ex.Message}");
            }
        }

        private static void StartPipeServer()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        try
                        {
                            pipeServer.WaitForConnection();

                            using (StreamReader reader = new StreamReader(pipeServer))
                            {
                                string command = reader.ReadLine();
                                if (command != null)
                                {
                                    mainForm?.HandleCommand(command);
                                }
                            }
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine($"Pipe server error: {ex.Message}");
                        }
                    }
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
