﻿using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
            ArkMsg,
            PluginInfo,
            CallResult,
        }
        public enum CallResult
        {
            Pass,
            Block
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
        private static Dictionary<int, MessageStateMachine> OrderedMessage = new();
        private static int msgSeq = 0;
        public static void Broadcast(string type, object msg, bool orderful = false)
        {
            msgSeq++;
            if (orderful)
            {
                MessageStateMachine stateMachine = new(type, msg, Clients);
                Helper.OutLog("加入队列");
                OrderedMessage.Add(msgSeq, stateMachine);
                stateMachine.Next();
            }
            else
            {
                Clients.ForEach(client => client.Emit(type, msg));
            }
        }
        private static void RemoveStateMachine(int seq)
        {
            Helper.OutLog($"清除消息队列，序号: {seq}");
             OrderedMessage.Remove(seq);
        }
        public class MessageStateMachine
        {
            public int index = 0;

            readonly List<MsgHandler> clients;
            readonly string type;
            readonly object msg;
            public MessageStateMachine(string type, object msg, List<MsgHandler> clients)
            {
                this.clients = clients;
                this.type = type;
                this.msg = msg;
            }
            public void Next()
            {
                if(index < clients.Count)
                {
                    clients[index].Emit(type, msg);
                    index++;
                }
                else
                {
                    Helper.OutLog("溢出");
                    RemoveStateMachine(index);
                }
            }
            public void HandleResult(CallResult result)
            {
                switch (result)
                {
                    case CallResult.Pass:
                        Helper.OutLog($"投递消息，序号: {index}");
                        if(index == clients.Count)
                        {
                            RemoveStateMachine(index);
                        }
                        else
                        {
                            Next();
                        }
                        break;
                    case CallResult.Block:
                        Helper.OutLog($"阻止消息投递，序号: {index}");
                        RemoveStateMachine(index);
                        return;
                    default:
                        break;
                }
            }
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
                Helper.OutLog("Plugin Connected.");
            }
            protected override void OnClose(CloseEventArgs e)
            {
                Clients.Remove(this);
                Helper.OutLog("Plugin Disconnected.");
            }
            public void Emit(string type, object msg)
            {
                Send((new { type, seq = msgSeq, data = new { msg, timestamp = Helper.TimeStamp } }).ToJson());
            }
            public async static void HandleMessage(MsgHandler socket, string Data)
            {
                JObject json = JObject.Parse(Data);
                int msgSeq = ((int)json["seq"]);
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
                    case MessageType.CallResult:
                        OrderedMessage[msgSeq].HandleResult((CallResult)((int)json["data"]["result"]));
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
