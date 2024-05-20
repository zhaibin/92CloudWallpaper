using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace _92CloudWallpaper
{
    public class Downloader : IDisposable
    {
        private static readonly HttpClient _client = new HttpClient();
        private List<string> _filePaths = new List<string>();
        private bool _disposed = false;

        public async Task DownloadFilesAsync(List<string> urlList, Action<List<string>> onCompleted)
        {
            List<Task> downloadTasks = new List<Task>();
            foreach (var url in urlList)
            {
                if (!string.IsNullOrEmpty(url))
                {
                    downloadTasks.Add(DownloadFileAsync(url));
                }
            }

            await Task.WhenAll(downloadTasks);
            onCompleted?.Invoke(_filePaths);
        }

        private async Task DownloadFileAsync(string url, int maxRetries = 3)
        {
            // 解析 URL 以获取文件名
            Uri uri = new Uri(url);
            string fileName = Path.GetFileName(uri.LocalPath);  // 获取原始文件名
            string tempPath = Path.GetTempPath();  // 获取系统临时文件夹路径
            string folderName = "CloudWallpaper";
            string folderPath = Path.Combine(tempPath, folderName, "Temp");
            string localPath = Path.Combine(folderPath, fileName);
            // 检查目标文件夹是否存在，不存在则创建
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            if (File.Exists(localPath))
            {
                long fileSize = new FileInfo(localPath).Length;
                if (fileSize > 0)
                {
                    _filePaths.Add(localPath);
                    return; // 如果文件大小为0，则跳过下载
                }
            }

            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    //Console.WriteLine("downloading...");
                    using (var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await contentStream.CopyToAsync(fileStream);
                            _filePaths.Add(localPath);  // 将路径添加到列表
                        }
                        return; // 成功下载后返回
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {retryCount + 1} failed to download {url}: {ex.Message}");
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        Console.WriteLine("Max retry attempts reached, download failed.");
                        throw; // 可选地，可以在这里处理更多的异常逻辑或记录
                    }
                }
            }
        }

        public List<string> GetFilePaths()
        {
            return _filePaths;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here, if any
                }
                _disposed = true;
            }
        }

        ~Downloader()
        {
            Dispose(false);
        }
    }
}
