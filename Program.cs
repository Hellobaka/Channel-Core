using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Channel_Core
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Helper.OutLog("创建本地WebSocket服务器...预计端口号6235");
            WSocketServer server = new WSocketServer(6235);
            Helper.OutLog("ws://127.0.0.1:6235/main");
            var wsPoint = LoadGateWay();
            wsPoint.Wait();
            Helper.OutLog($"wss入口：{wsPoint.Result}");
            Helper.OutLog("尝试连接服务器...");
            WebSocketCore webSocket = new WebSocketCore(wsPoint.Result);
            webSocket.Connect();
            Helper.OutLog("进入消息循环，输入Ctrl+C中断...");
            while (true)
            {
                Console.ReadLine();
            }
        }
        public static async Task<string> LoadGateWay()
        {
            using (var client = Helper.GetTemplateHttpClient())
            {
                var result = await client.GetAsync("gateway");
                string json = await result.Content.ReadAsStringAsync();
                return JObject.Parse(json)["url"].ToString();
            }
        }
    }
}
