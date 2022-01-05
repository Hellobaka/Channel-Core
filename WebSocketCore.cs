using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using WebSocket4Net;

namespace Channel_Core
{
    public class WebSocketCore
    {
        public static WebSocketCore Instance { get; private set; }
        public enum OpCode
        {
            Dispatch = 0,
            Heartbeat = 1,
            Identify = 2,
            Resume = 6,
            Reconnect = 7,
            InvalidSession = 9,
            Hello = 10,
            HeartbeatACK = 11
        }
        private string WebSocketURL { get; set; } = "";
        public WebSocket Websocket { get; set; }
        private int HeartBeat_TimeOut { get; set; } = 0;
        private int LastSeq { get; set; } = 0;
        private int LostConnectionCount { get; set; } = 0;
        private bool HeartBeatStop { get; set; } = false;
        private bool Connected { get; set; } = false;
        private bool Resuming { get; set; } = false;
        private string SessionID { get; set; } = string.Empty;
        private Thread HeartBeat_Thread { get; set; } = null;

        #region Event
        public delegate void DispatchHandler(JToken msg, string msgType, int seq);
        public event DispatchHandler Opcode_Dispatch;

        public delegate void HeartbeatHandler();
        public event HeartbeatHandler Opcode_Heartbeat;

        public delegate void ReconnectHandler();
        public event ReconnectHandler Opcode_Reconnect;

        public delegate void InvalidSessionHandler(JToken msg);
        public event InvalidSessionHandler Opcode_InvalidSession;

        public delegate void HelloHandler(JToken msg);
        public event HelloHandler Opcode_Hello;

        public delegate void HeartbeatACKHandler();
        public event HeartbeatACKHandler Opcode_HeartbeatACK;

        #endregion
        public WebSocketCore(string url)
        {
            WebSocketURL = url;
            WebSocketInit();
            Instance = this;
        }
        private void WebSocketInit()
        {
            Websocket = new WebSocket(WebSocketURL);
            Websocket.Opened += Websocket_Opened;
            Websocket.Error += Websocket_Error;
            Websocket.Closed += Websocket_Closed;
            Websocket.MessageReceived += Websocket_MessageReceived;
        }
        public void Connect()
        {
            Websocket.Open();
        }
        public void ReConnect()
        {
            Connected = false;
            HeartBeatStop = true;
            Helper.OutLog($"尝试重连...");
            SendResumeRequest();
        }
        private void SendResumeRequest()
        {
            Resuming = true;
            SendMsg(OpCode.Resume, new { token = Config.Token, session_id = SessionID, seq = LastSeq });            
        }

        private void Websocket_Opened(object sender, EventArgs e)
        {
            Helper.OutLog("WebSocket连接成功");
            if (Opcode_Hello != null) return;
            Opcode_Hello += WebSocketCore_Opcode_Hello;
            Opcode_HeartbeatACK += WebSocketCore_Opcode_HeartbeatACK;
            Opcode_Dispatch += WebSocketCore_Opcode_Dispatch;
            Opcode_InvalidSession += WebSocketCore_Opcode_InvalidSession;
            Opcode_Reconnect += WebSocketCore_Opcode_Reconnect;
        }

        private void WebSocketCore_Opcode_Reconnect()
        {
            ReConnect();
        }

        private void WebSocketCore_Opcode_InvalidSession(JToken msg)
        {
            Helper.OutError("鉴权或重连参数有误");
        }

        private void WebSocketCore_Opcode_HeartbeatACK()
        {
            LostConnectionCount = 0;
        }
        private void WebSocketCore_Opcode_Dispatch(JToken msg, string msgType, int seq)
        {
            Connected = true;
            switch (msgType)
            {
                case "READY":
                    if (((bool)msg["user"]["bot"]))
                    {
                        SessionID = msg["session_id"].ToString();
                        Helper.OutLog($"收到Bot信息:{msg["user"]["username"]} v{msg["version"]} - {msg["user"]["id"]}");
                        Helper.OutLog("验证通过，开启心跳...");
                        if (HeartBeat_Thread != null) break;
                        HeartBeatStop = false;
                        HeartBeat_Thread = new Thread(() =>
                        {
                            Helper.OutLog($"心跳线程开始，频率 {HeartBeat_TimeOut} ms");
                            while (true)
                            {
                                if (LostConnectionCount >= 3)
                                {
                                    Helper.OutLog("似乎与服务器断开了连接，尝试发送重连请求");
                                    ReConnect();
                                }
                                else
                                {
                                    LostConnectionCount++;
                                    SendMsg(OpCode.Heartbeat, LastSeq == 0 ? null : LastSeq);
                                    int count = 0;
                                    while(count <= HeartBeat_TimeOut / 100)
                                    {
                                        if (HeartBeatStop) break;
                                        count++;
                                        Thread.Sleep(100);
                                    }
                                    if (HeartBeatStop)
                                    {
                                        Console.WriteLine("HeartBeat stop.");
                                        break;
                                    }
                                }
                            }
                        });
                        HeartBeat_Thread.Start();
                    }
                    break;
                case "RESUMED":
                    HeartBeatStop = false;
                    Resuming = false;
                    Helper.OutLog("重连完成");
                    break;
                default:
                    LastSeq = seq;
                    break;
            }
        }

        private void WebSocketCore_Opcode_Hello(JToken msg)
        {
            if (Connected) return;
            Connected = true;
            HeartBeat_TimeOut = msg["heartbeat_interval"].ToObject<int>();
            Helper.OutLog($"收到服务器Hello响应，心跳频率:{HeartBeat_TimeOut}ms");
            Helper.OutLog("发送Identity请求...");
            SendIdentityRequest();

            //if (!Resuming)
            //{
            //    Helper.OutLog("发送Identity请求...");
            //    SendIdentityRequest();
            //}
        }
        private void SendIdentityRequest()
        {
            SendMsg(OpCode.Identify, new
            {
                token = $"Bot {Config.AppID}.{Config.Token}",
                intents = 0 | 1 << 30 | 1 << 1 | 1 << 0,
            });
        }
        private void Websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Helper.OutError($"WebSocket连接出错:{e.Exception.Message}");
        }

        private void Websocket_Closed(object sender, EventArgs e)
        {
            Connected = false;
            HeartBeatStop = true;
            HeartBeat_Thread = null;
            Helper.OutError("与服务器断开连接，阻止重新连接请按Ctrl+C");
            Thread.Sleep(1000 * 3);
            Helper.OutLog("尝试重新连接...");
            Websocket.Dispose();
            WebSocketInit();
            Connect();
            //Helper.OutLog("发送Resume请求...");
            //SendResumeRequest();
        }
        public void SendMsg(OpCode opCode, object msg)
        {
            JObject msgToSend = new()
            {
                { "op", (int)opCode },
            };
            if (msg is null)
                msgToSend.Add("d", null);
            else
                msgToSend.Add("d", JToken.FromObject(msg));
            Helper.OutLog($"推送消息:{msgToSend.ToString(Formatting.None)}");
            Websocket.Send(msgToSend.ToString(Formatting.None));
        }
        private void Websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Helper.OutLog($"来自服务器的消息:{e.Message}");
            JObject msg = JObject.Parse(e.Message);
            switch (msg["op"].ToString())
            {
                case "0":
                    WSocketServer.Broadcast("Dispatch", new { t = msg["t"].ToString() , s = (int)msg["s"], d = msg["d"].ToObject<object>() });
                    Opcode_Dispatch?.Invoke(msg["d"], msg["t"].ToString(), (int)msg["s"]);
                    break;
                case "1":
                    WSocketServer.Broadcast("Heartbeat", "");
                    Opcode_Heartbeat?.Invoke();
                    break;
                case "7":
                    Connected = true;
                    HeartBeatStop = false;
                    WSocketServer.Broadcast("Reconnect", "");
                    Opcode_Reconnect?.Invoke();
                    break;
                case "9":
                    Connected = false;
                    HeartBeatStop = true;
                    WSocketServer.Broadcast("InvalidSession", new { d = msg["d"].ToObject<object>() });
                    Opcode_InvalidSession?.Invoke(msg["d"]);
                    break;
                case "10":
                    WSocketServer.Broadcast("Hello", new { d = msg["d"].ToObject<object>() });
                    Opcode_Hello?.Invoke(msg["d"]);
                    break;
                case "11":
                    WSocketServer.Broadcast("HeartbeatACK", "");
                    Opcode_HeartbeatACK?.Invoke();
                    break;
                default:
                    break;
            }
        }
    }
}
