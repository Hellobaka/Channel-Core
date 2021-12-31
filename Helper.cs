using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Channel_Core
{
    public static class Helper
    {
        private static readonly string baseURL = "https://api.sgroup.qq.com/";
        private static readonly string baseTestURL = "https://api.sgroup.qq.com/";
        public static bool IsDebugMode = false;
        public static HttpClient GetTemplateHttpClient()
        {
            var client = new HttpClient
            {
                BaseAddress = IsDebugMode ? new Uri(baseURL) : new Uri(baseTestURL),
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", $"{Config.AppID}.{Config.Token}");
            return client;
        }
        public static void OutError(string content)
        {
            Console.WriteLine($"[-][{DateTime.Now.ToLongTimeString()}] {content}");
        }
        public static void OutLog(string content)
        {
            Console.WriteLine($"[+][{DateTime.Now.ToLongTimeString()}] {content}");
        }
    }
}
