using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;

namespace _92CloudWallpaper
{
    public class SoftwareUpdater
    {
        private readonly Main mainForm;
        private readonly string githubReleasesUrl = "https://api.github.com/repos/zhaibin/92CloudWallpaper/releases/latest";
        private Timer versionCheckTimer;
        private string downloadUrl = "";

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

        public async Task CheckForUpdateAsync()
        {
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "request");
                client.Encoding = System.Text.Encoding.UTF8;  // 确保处理 UTF-8 编码
                var response = await client.DownloadStringTaskAsync(githubReleasesUrl);
                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    var latestVersion = doc.RootElement.GetProperty("tag_name").GetString();
                    try
                    {
                        downloadUrl = doc.RootElement.GetProperty("assets")[0].GetProperty("browser_download_url").GetString();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred: {ex.Message}.URL{downloadUrl}");
                    }

                    if (latestVersion != Main.CurrentVersion)
                    {
                        ShowUpdateDialog();
                    }
                    else
                    {
                        ShowNoUpdateDialog();
                    }
                }
            }
        }

        private void ShowUpdateDialog()
        {
            var result = MessageBox.Show("发现新版本，是否立即更新？", "更新提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
            {
                DownloadLatestVersion();
            }
        }

        private void ShowNoUpdateDialog()
        {
            var result = MessageBox.Show("未发现新版本。", "更新提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (result == DialogResult.OK)
            {
                // Do nothing, just close the dialog
            }
        }

        private async void DownloadLatestVersion()
        {
            if (downloadUrl != "")
            {
                string installerPath = Path.Combine(Path.GetTempPath(), "92CloudWallpaper.exe");

                using (var client = new WebClient())
                {
                    var progressDialog = new ProgressDialog
                    {
                        InfoLabel = { Text = "正在下载最新版本..." }
                    };
                    progressDialog.Show();

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
                            MessageBox.Show("下载失败：" + ev.Error.Message, "下载错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            MessageBox.Show("下载完成，即将安装。", "下载完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            System.Diagnostics.Process.Start(installerPath);
                        }
                    };

                    await client.DownloadFileTaskAsync(new Uri(downloadUrl), installerPath);
                }
            }
            else
            {
                MessageBox.Show("下载文件出现异常。", "下载错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
