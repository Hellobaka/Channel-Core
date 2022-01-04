using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Channel_Core
{
    public class WSocketServer
    {
        public enum MessageType
        {
            PlainMsg,
            EmbedMsg,
            ArkMsg
        }

        WebSocketServer Instance;
        ushort port;
        public static List<MsgHandler> Clients;
        public WSocketServer(ushort Port)
        {
            port = Port;
            Instance = new(port);
            Instance.AddWebSocketService<MsgHandler>("/main");
            Clients = new List<MsgHandler>();
            Instance.Start();
        }
        public static void Broadcast(string type, object msg)
        {
            Clients.ForEach(x => x.Emit(type, msg));
        }
        public class MsgHandler : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                HandleMessage(this, e.Data);
            }
            protected override void OnOpen()
            {
                Clients.Add(this);
            }
            protected override void OnClose(CloseEventArgs e)
            {
                Clients.Remove(this);
            }
            public void Emit(string type, object msg)
            {
                Send((new { type, data = new { msg, timestamp = Helper.TimeStamp } }).ToJson());
            }
            public async static void HandleMessage(MsgHandler socket, string Data)
            {
                JObject json = JObject.Parse(Data);
                using var http = Helper.GetTemplateHttpClient();
                switch ((MessageType)(int)json["type"])
                {
                    case MessageType.PlainMsg:
                        string channelID = json["data"]["channelID"].ToString();
                        string url = $"channels/{channelID}/messages";
                        var result = await http.PostAsync(url, new StringContent(json["data"]["content"].ToString(), Encoding.UTF8, "application/json"));
                        Helper.OutLog($"code: {result.StatusCode} content: {await result.Content.ReadAsStringAsync()}");
                        break;
                    case MessageType.EmbedMsg:
                        break;
                    case MessageType.ArkMsg:
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
