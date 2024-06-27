using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _92CloudWallpaper
{
    
    public class InfoHelper
    {
        //public string currentVersion;
        public InfoHelper() { }
      

        public static class SoftwareInfo
        {
            public static string NameCN = "92云壁纸";
            public static string NameEN = "92CloudWallpaper";
            public static string CurrentVersion = "v0.4.2.1";
        }

        public static class Urls
        {
            public static string Login = "https://look.levect.com/web/login";
            public static string LoginSMS = "https://look.levect.com/web/verifylogin";
            public static string Store = "https://look.levect.com/web/";
            public static string Post = " https://look.levect.com/web/publishWorks";
            public static string MyAlbum = "https://look.levect.com/web/album";
            public static string MySub = "https://look.levect.com/web/subscription";
            //public static string  = "";
            //public static string  = "";
            //public static string  = "";
            public static string Stats = "https://hk-tracking-hz.log-global.aliyuncs.com/logstores/eframe/track?APIVersion=0.6.0";
        }

        public static class StatsBehavior
        {
            public static string SetWallpaper = "SetWallpaper";
            public static string StartApplication = "StartApplication";
        }
        public static class DistributeChannel
        {
            public static string Name = "Self";
        }
    }
    public static class GlobalData
    {
        public static int UserId = Properties.Settings.Default.UserId; // 定义一个静态变量
        //public static int UserId = 11583031;
        public static int PageIndex = 1;
        public const int PageSize = 20;
        public static int ImageIndex = 0;
        public static int LoginFlag = 0;
        public static string Token = Properties.Settings.Default.Token;
        public static string Did = Properties.Settings.Default.Uuid;
        public static DateTime LastUpdateTime;
    }
}
