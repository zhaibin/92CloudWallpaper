using System;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Documents;

namespace _92CloudWallpaper
{
    public class Stats
    {
        private static readonly HttpClient client = new HttpClient();

        public async Task ReportAsync(ImageCacheManager.ImageInfo imageInfo, string behavior)
        {
            

            StringBuilder urlBuilder = new StringBuilder();
            urlBuilder.Append(InfoHelper.Urls.Stats);
            urlBuilder.Append("&uid=" + GlobalData.UserId);
            urlBuilder.Append("&did=" + GlobalData.Did);
            if (imageInfo != null) 
            {
                urlBuilder.Append("&groupid=" + imageInfo.GroupId);
                urlBuilder.Append("&albumid=" + imageInfo.AlbumId);
                urlBuilder.Append("&authorid=" + imageInfo.AuthorId);
            }
            else
            {
                urlBuilder.Append("&groupid=0");
                urlBuilder.Append("&albumid=0");
                urlBuilder.Append("&authorid=0");
            }
            urlBuilder.Append("&bhv=" + behavior);
            urlBuilder.Append("&appver=" + InfoHelper.SoftwareInfo.CurrentVersion);
            urlBuilder.Append("&dc=" + InfoHelper.DistributeChannel.Name);
            urlBuilder.Append("&ts=" + GetUnixTimeStamp());
            urlBuilder.Append("&" + GetStatsSystemParameters());
            

            string url = urlBuilder.ToString();
            try
            {
                Console.WriteLine($"Stats:{behavior},Url: {url}");
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Request error: " + e.Message);
            }
        }

        public string GetStatsSystemParameters()
        {
            StringBuilder queryStringBuilder = new StringBuilder();

            // 获取操作系统主要版本
            string osName = GetWmiPropertyValue("Win32_OperatingSystem", "Caption");
            queryStringBuilder.Append("platform=Windows");
            queryStringBuilder.Append("&os=" + UrlEncode(osName));

            // 获取操作系统版本
            queryStringBuilder.Append("&osver=" + UrlEncode(GetWmiPropertyValue("Win32_OperatingSystem", "Version")));

            // 获取系统内存
            string totalMemory = GetWmiPropertyValue("Win32_ComputerSystem", "TotalPhysicalMemory");
            queryStringBuilder.Append("&memory=" + UrlEncode(FormatMemorySize(totalMemory)));

            return queryStringBuilder.ToString();
        }

        static string GetWmiPropertyValue(string wmiClass, string wmiProperty)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT {wmiProperty} FROM {wmiClass}"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj[wmiProperty]?.ToString() ?? "unknown";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving {wmiClass}.{wmiProperty}: {ex.Message}");
            }

            return "unknown";
        }

        static string FormatMemorySize(string memorySizeInBytes)
        {
            if (long.TryParse(memorySizeInBytes, out long bytes))
            {
                double gigabytes = bytes / (1024 * 1024 * 1024.0);
                int roundedGigabytes = (int)Math.Round(gigabytes / 8.0) * 8; // Round to nearest 8GB
                return $"{roundedGigabytes}GB";
            }

            return "unknown";
        }

        static string UrlEncode(string value)
        {
            return HttpUtility.UrlEncode(value);
        }

        static long GetUnixTimeStamp()
        {
            DateTime now = DateTime.Now;

            // 转换为 Unix 时间戳
            return  ((DateTimeOffset)now).ToUnixTimeSeconds();
            
        }
    }
}
