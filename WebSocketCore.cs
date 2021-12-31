using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using WebSocket4Net;

namespace Channel_Core
{
    public class WebSocketCore
    {
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
        private string SessionID { get; set; } = string.Empty;

        #region Event
        public delegate void DispatchHandler(JToken msg, string msgType, string seq);
        public event DispatchHandler Opcode_Dispatch;

        public delegate void HeartbeatHandler();
        public event HeartbeatHandler Opcode_Heartbeat;

        public delegate void ReconnectHandler(JToken msg);
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
            int reTryCount = 0;
            while (!Connected)
            {
                reTryCount++;
                Websocket.Dispose();
                WebSocketInit();
                Connect();
                Helper.OutLog($"第 {reTryCount} 次尝试重连...");
                SendMsg(OpCode.Resume, new { token = Config.Token, session_id = SessionID, seq = LastSeq });
                Thread.Sleep(1000 * 3);
            }
        }
        private void Websocket_Opened(object sender, EventArgs e)
        {
            Helper.OutLog("WebSocket连接成功");
            Opcode_Hello += WebSocketCore_Opcode_Hello;
            Opcode_HeartbeatACK += WebSocketCore_Opcode_HeartbeatACK;
        }

        private void WebSocketCore_Opcode_HeartbeatACK()
        {
            LostConnectionCount = 0;
        }
        private void WebSocketCore_Opcode_Hello(JToken msg)
        {
            Connected = true;
            HeartBeat_TimeOut = msg["heartbeat_interval"].ToObject<int>();
            Helper.OutLog($"收到服务器Hello响应，心跳频率：{HeartBeat_TimeOut}ms");
            new Thread(() =>
            {
                Helper.OutLog($"心跳线程开始，频率 {HeartBeat_TimeOut} ms");
                while (!HeartBeatStop)
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
                        Thread.Sleep(HeartBeat_TimeOut);
                    }
                }
            }).Start();
        }

        private void Websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Helper.OutError($"WebSocket连接出错：{e.Exception.Message}");
        }

        private void Websocket_Closed(object sender, EventArgs e)
        {
            Helper.OutError("与服务器断开连接");
        }
        public void SendMsg(OpCode opCode, object msg)
        {
            JObject msgToSend = new()
            {
                { "op", (int)opCode },
            };
            if (msg != null)
                msgToSend.Add("d", JObject.Parse(JsonConvert.SerializeObject(msg)));
            else
                msgToSend.Add("d", null);
            Helper.OutLog($"推送消息：{msgToSend.ToString(Formatting.None)}");
            Websocket.Send(msgToSend.ToString(Formatting.None));
        }
        private void Websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Helper.OutLog($"来自服务器的消息：{e.Message}");
            JObject msg = JObject.Parse(e.Message);
            switch (msg["op"].ToString())
            {
                case "0":
                    if (msg["t"].ToString() == "READY" && ((bool)msg["d"]["user"]["bot"]))
                    {
                        SessionID = msg["d"]["session_id"].ToString();
                        Helper.OutLog($"收到Bot信息：{msg["d"]["user"]["username"]} v{msg["d"]["version"]} - {msg["d"]["user"]["id"]}");
                    }
                    Opcode_Dispatch(msg["d"], msg["t"].ToString(), msg["s"].ToString());
                    break;
                case "1":
                    Opcode_Heartbeat();
                    break;
                case "7":
                    Connected = true;
                    HeartBeatStop = false;
                    Opcode_Reconnect(msg["d"]);
                    break;
                case "9":
                    break;
                case "10":
                    Opcode_Hello(msg["d"]);
                    break;
                case "11":
                    Opcode_HeartbeatACK();
                    break;
                default:
                    break;
            }
        }
    }
}
