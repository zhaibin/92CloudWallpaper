using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace _92CloudWallpaper
{
    public class ApiRequestHandler
    {
        private readonly HttpClient _client;
        private readonly string _apiKey;
        private readonly string ts = GenerateFormattedTimestamp();
        public string userId = "0";
        public readonly string _uuid;

        public ApiRequestHandler()
        {
            _client = new HttpClient();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            _apiKey = configuration["ApiKey"];

            // 获取或生成 UUID
            _uuid = GetOrCreateUuid();
        }

        // 发送API请求的通用方法
        public async Task<string> SendApiRequestAsync(string url, object body)
        {
            var messageID = ts + GenerateRandomNumberString(10);
            var timeStamp = ts;
            var header = new Dictionary<string, object>
            {
                { "messageID", messageID },
                { "timeStamp", timeStamp },
                { "terminal", 10 },
                { "version", InfoHelper.SoftwareInfo.CurrentVersion },
                { "companyId", "10120" },
                { "countryCode", "+86" },
                { "did", _uuid }
            };
            var message = new Dictionary<string, object>
            {
                { "header" , header },
                { "body" , body },
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = false,  // 生成单行格式的 JSON
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // 减少字符转义
            };
            var jsonBody = JsonSerializer.Serialize(body, options);

            var sign = GenerateMd5Signature(messageID, timeStamp, _apiKey, jsonBody);
            header["sign"] = sign;
            message["header"] = header;

            var jsonNew = JsonSerializer.Serialize(message, options);
            var content = new StringContent(jsonNew, Encoding.UTF8, "application/json");
            Console.WriteLine($"req : {jsonNew}");
            var response = await _client.PostAsync(url, content);

            return await response.Content.ReadAsStringAsync();
        }

        // 生成当前的时间戳
        private static string GenerateFormattedTimestamp()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        private static string GenerateMd5Signature(string messageId, string timestamp, string secretKey, string messageBody)
        {
            var rawString = $"{messageId}{timestamp}{secretKey}{messageBody}";
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(rawString));
                var sb = new StringBuilder();
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // 获取或生成 UUID
        private static string GetOrCreateUuid()
        {
            var uuid = Properties.Settings.Default.Uuid;
            if (string.IsNullOrEmpty(uuid))
            {
                uuid = Guid.NewGuid().ToString();
                Properties.Settings.Default.Uuid = uuid;
                Properties.Settings.Default.Save();
            }
            return uuid;
        }

        private static string GenerateRandomNumberString(int length)
        {
            var random = new Random();
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(random.Next(0, 10));
            }
            return sb.ToString();
        }

    }

    
}
