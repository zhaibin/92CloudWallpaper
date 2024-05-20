using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;


namespace _92CloudWallpaper
{
    public class ApiRequestHandler
    {
        private readonly HttpClient _client;
        private const string _apiKey = "Z2oCDluRc0JlXUAmRO";
        private readonly string  ts = GenerateFormattedTimestamp();
        public string userId = "0";

        public ApiRequestHandler()
        {
            _client = new HttpClient();
        }
        // 发送API请求的通用方法
        public async Task<string> SendApiRequestAsync(string url, object body)
        {
            var messageID = ts + "0000000001";
            var timeStamp = ts;
            var header = new Dictionary<string, object>
            {
                { "messageID", messageID },
                { "timeStamp", timeStamp },
                { "terminal", 1 },
                { "version", "0.1" },
                { "companyId", "10120" },
                { "countryCode", "+86" },
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

            //Console.WriteLine(jsonBody);

            var sign = GenerateMd5Signature(messageID, timeStamp, _apiKey, jsonBody);
            //Console.WriteLine(sign);
            header["sign"] = sign;
            message["header"] = header;

            var jsonNew = JsonSerializer.Serialize(message, options);
            //Console.WriteLine(jsonNew);

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
            // Concatenate all parts to form the base string for hashing
            var rawString = $"{messageId}{timestamp}{secretKey}{messageBody}";
            //Console.WriteLine(rawString);
            using (var md5 = MD5.Create())
            {
                // Compute the MD5 hash
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(rawString));
                // Convert hash to a hexadecimal string
                var sb = new StringBuilder();
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        
    }

    public static class GlobalData
    {
        public static int UserId = Properties.Settings.Default.UserId; // 定义一个静态变量
        public static int PageIndex = 1;
        public static int ImageIndex = 0;
        public static int LoginFlag = 0;
    }
}
