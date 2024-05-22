using System;
using System.IO;


namespace _92CloudWallpaper
{
    public static class Logger
    {
        //private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
        private static readonly string logFilePath = Path.Combine(Path.GetTempPath(), "CloudWallpaper", "error.log");

        public static void LogError(string message, Exception ex)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERROR: {message}");
                    writer.WriteLine($"Exception: {ex.Message}");
                    writer.WriteLine($"Stack Trace: {ex.StackTrace}");
                    writer.WriteLine("---------------------------------------------------");
                }
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Failed to log error: {logEx.Message}");
            }
        }

        public static void LogInfo(string message)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - INFO: {message}");
                    writer.WriteLine("---------------------------------------------------");
                }
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Failed to log info: {logEx.Message}");
            }
        }
    }
}
