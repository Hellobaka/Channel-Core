using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Channel_Core
{
    internal class Program
    {
        private const string pluginBasePath = "Plugins";

        public static void Main(string[] args)
        {
            Helper.OutLog("创建本地WebSocket服务器...预计端口号6235");
            WSocketServer server = new(6235);
            Helper.OutLog("ws://127.0.0.1:6235/main");
            var wsPoint = LoadGateWay();
            wsPoint.Wait();
            Helper.OutLog($"wss入口：{wsPoint.Result}");
            Helper.OutLog("尝试连接服务器...");
            WebSocketCore webSocket = new(wsPoint.Result);
            webSocket.Connect();
            Helper.OutLog("载入插件...");
            LoadPlugin();
            Helper.OutLog("插件载入完成...");
            Helper.OutLog("进入消息循环，输入Ctrl+C中断...");
            while (true)
            {
                string cmd = Console.ReadLine();
                switch (cmd)
                {
                    case "reload":
                        ReloadPlugin();
                        break;
                    default:
                        break;
                }
            }
        }
        public static async Task<string> LoadGateWay()
        {
            using var client = Helper.GetTemplateHttpClient();
            var result = await client.GetAsync("gateway");
            string json = await result.Content.ReadAsStringAsync();
            return JObject.Parse(json)["url"].ToString();
        }
        public static void ReloadPlugin()
        {
            WSocketServer.Clients.ForEach(client => client.Emit("Disconnect", ""));
            Thread.Sleep(2000);
            LoadPlugin();
        }
        public static void LoadPlugin()
        {
            if (Directory.Exists(pluginBasePath))
            {
                DirectoryInfo dir = new(pluginBasePath);
                foreach (var item in dir.GetDirectories())
                {
                    string pluginName = Path.Combine(item.FullName, item.Name + ".exe");
                    string args = "";
                    if (File.Exists(pluginName))
                    {
                        ProcessStartInfo ps = new(pluginName)
                        {
                            UseShellExecute = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Normal,
                            Arguments = args
                        };
                        Process.Start(ps);
                    }
                    else
                    {
                        Helper.OutError($"找不到文件 {pluginName}");
                        //Environment.Exit(0);
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(pluginBasePath);
            }
        }
    }
}
