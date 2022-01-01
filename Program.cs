using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Channel_Core
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var wsPoint = LoadGateWay();
            wsPoint.Wait();
            Helper.OutLog($"wss入口：{wsPoint.Result}");
            Helper.OutLog("尝试连接服务器...");
            WebSocketCore webSocket = new(wsPoint.Result);
            webSocket.Connect();
            while (true)
            {
                Helper.OutLog("进入消息循环，输入Ctrl+C中断...");
                Console.ReadLine();
            }
        }
        public static async Task<string> LoadGateWay()
        {
            using var client = Helper.GetTemplateHttpClient();
            var result = await client.GetAsync("gateway");
            string json = await result.Content.ReadAsStringAsync();
            return JObject.Parse(json)["url"].ToString();
        }
    }
}
