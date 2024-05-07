using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace _92CloudWallpaper
{
    public class ApiRequestHandler
    {
        private HttpClient _client;
        private string _apiKey = "Z2oCDluRc0JlXUAmRO";
        private string ts = GenerateFormattedTimestamp();
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

            // 配置 JsonSerializer 排序属性
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new DefaultNamingStrategy()
                }
            };

            var jsonBody = JsonConvert.SerializeObject(body, settings);
            Console.WriteLine(jsonBody);

            //string jsonMessage = JsonConvert.SerializeObject(message, Newtonsoft.Json.Formatting.Indented);
            var sign = GenerateMd5Signature(messageID, timeStamp, _apiKey, jsonBody);
            //Console.WriteLine(sign);
            header["sign"] = sign;
            message["header"] = header;
            var jsonNew = JsonConvert.SerializeObject(message, settings);
            Console.WriteLine(jsonNew);

            var content = new StringContent(jsonNew, Encoding.UTF8, "application/json");
            //Console.WriteLine(content);
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
            Console.WriteLine(rawString);
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

        public class Response
        {
            public Header Header { get; set; }
            public Body Body { get; set; }
        }
        public class Header
        {
            public string MessageID { get; set; }
            public int ResCode { get; set; }
            public string ResMsg { get; set; }
            public string TimeStamp { get; set; }
            public string TransactionType { get; set; }
        }

        public class Body
        {
            public string Status { get; set; }
            public string Err { get; set; }
            public int UserId { get; set; }
        }
    }

    public static class GlobalData
    {
        public static int UserId = Properties.Settings.Default.UserId; // 定义一个静态变量
        public static int PageIndex = 1;
        public static int LoginFlag = 0;
    }
}
