using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.Net.Http;

namespace _92CloudWallpaper
{
    public class SoftwareUpdater
    {
        private readonly Main mainForm;
        private readonly string aliyunSoftwareVersion = "https://hk-content.oss-cn-hangzhou.aliyuncs.com/92CloudWallpaperVersion/version.txt";
        private const string flagServer = "aliyun";
        private Timer versionCheckTimer;
        private string downloadUrl = "";
        private const int MaxRetries = 3;

        public SoftwareUpdater(Main mainForm)
        {
            this.mainForm = mainForm;
            InitializeVersionCheckTimer();
        }

        private void InitializeVersionCheckTimer()
        {
            versionCheckTimer = new Timer
            {
                Interval = 86400000 // 每24小时检查一次
            };
            versionCheckTimer.Tick += async (sender, e) => await CheckForUpdateAsync();
            versionCheckTimer.Start();
        }

        public async Task CheckForUpdateAsync(bool notifyMessageBox = true)
        {
            if (!await IsNetworkAvailableAsync())
            {
                Console.WriteLine("No network connection available. 软件更新不可用。");
                if (notifyMessageBox)
                {
                    MessageBox.Show("更新服务不可用，请检查网络并稍后再试。", "更新提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            string versionUrl = flagServer == "aliyun" ? aliyunSoftwareVersion : ""; // 添加其他服务器 URL

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "request");
                    var response = await client.GetStringAsync(versionUrl);

                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var latestVersion = doc.RootElement.GetProperty("tag_name").GetString();
                        downloadUrl = doc.RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();

                        if (latestVersion != InfoHelper.SoftwareInfo.CurrentVersion)
                        {
                            ShowUpdateDialog(notifyMessageBox);
                        }
                        else if (notifyMessageBox)
                        {
                            ShowNoUpdateDialog();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for update: {ex.Message}");
                if (notifyMessageBox)
                {
                    MessageBox.Show("检查更新时发生错误，请稍后再试。", "更新提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowUpdateDialog(bool notifyMessageBox = true)
        {
            if (notifyMessageBox)
            {
                var result = MessageBox.Show("发现新版本，是否立即更新？", "更新提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    DownloadLatestVersion(notifyMessageBox);
                }
            }
            else
            {
                DownloadLatestVersion(notifyMessageBox);
            }
        }

        private void ShowNoUpdateDialog()
        {
            MessageBox.Show("未发现新版本。", "更新提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void DownloadLatestVersion(bool notifyMessageBox = true)
        {
            if (!string.IsNullOrEmpty(downloadUrl))
            {
                string installerPath = Path.Combine(Path.GetTempPath(), "92CloudWallpaper.exe");
                bool success = false;
                int attempt = 0;

                while (!success && attempt < MaxRetries)
                {
                    attempt++;
                    success = await TryDownloadFileAsync(downloadUrl, installerPath, attempt, notifyMessageBox);
                    if (!success && notifyMessageBox)
                    {
                        MessageBox.Show($"下载失败，正在尝试重试（{attempt}/{MaxRetries}）。", "下载错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                if (success)
                {
                    var result = MessageBox.Show("新版本已准备好，即将安装。", $"{InfoHelper.SoftwareInfo.NameCN}", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (result == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start(installerPath);
                    }
                }
                else if (notifyMessageBox)
                {
                    MessageBox.Show("多次重试后下载失败，请稍后再试。", "下载错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (notifyMessageBox)
            {
                MessageBox.Show("下载文件出现异常。", "下载错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<bool> TryDownloadFileAsync(string url, string destinationPath, int attempt, bool notifyMessageBox = true)
        {
            try
            {
                using (var client = new WebClient())
                {
                    var progressDialog = new ProgressDialog
                    {
                        InfoLabel = { Text = "正在下载最新版本..." }
                    };

                    if (notifyMessageBox)
                    {
                        progressDialog.Show();
                    }

                    client.DownloadProgressChanged += (s, ev) =>
                    {
                        progressDialog.ProgressBar.Value = ev.ProgressPercentage;
                        progressDialog.InfoLabel.Text = $"已下载 {ev.BytesReceived / 1024} KB / {ev.TotalBytesToReceive / 1024} KB";
                    };

                    client.DownloadFileCompleted += (s, ev) =>
                    {
                        progressDialog.Close();
                        if (ev.Error != null)
                        {
                            throw ev.Error;
                        }
                    };

                    if (File.Exists(destinationPath) && attempt > 1)
                    {
                        client.Headers.Add(HttpRequestHeader.Range, $"bytes={new FileInfo(destinationPath).Length}-");
                    }

                    await client.DownloadFileTaskAsync(new Uri(url), destinationPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"下载尝试 {attempt} 失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> IsNetworkAvailableAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", InfoHelper.SoftwareInfo.NameEN);
                    var response = await client.GetAsync(aliyunSoftwareVersion);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Network check failed: {ex.Message}");
                return false;
            }
        }
    }
}

