﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using ServerHub.Rooms;
using ServerHub.Misc;
using Logger = ServerHub.Misc.Logger;
using static ServerHub.Hub.RCONStructs;
using System.Linq;
using WebSocketSharp.Server;
using System.Reflection;
using ServerHub.Data;

namespace ServerHub.Hub
{
    public struct WebSocketPacket
    {
        public string commandType;
        public object data;

        public WebSocketPacket(CommandType command, object d)
        {
            commandType = command.ToString();
            data = d;
        }
    }

    public struct SongWithDifficulty
    {
        public SongInfo song;
        public byte difficulty;

        public SongWithDifficulty(SongInfo songInfo, byte diff)
        {
            song = songInfo;
            difficulty = diff;
        }
    }

    public struct ServerHubInfo
    {
        public string Version;
        public int TotalClients;
        public List<WSRoomInfo> Rooms;
        public List<RCONChannelInfo> RadioChannels;
    }

    public struct WSRoomInfo
    {
        public string Path;
        public string RoomName;

        public WSRoomInfo(string _path)
        {
            Path = _path;

            string[] split = _path.Split('/');
            int _id = Convert.ToInt32(split[split.Length - 1]);

            BaseRoom _room = RoomsController.GetRoomsList().Find(room => room.roomId == _id);
            RoomName = _room.roomSettings.Name;
        }
    }

    public class ListServices : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            base.OnOpen();

            if (Settings.Instance.Server.EnableWebSocketRoomInfo)
            {
                try { 
                    List<string> paths = WebSocketListener.Server.WebSocketServices.Paths.ToList();
                    paths.Remove("/");
                    if (Settings.Instance.Server.EnableWebSocketRCON)
                    {
                        paths.Remove("/" + Settings.Instance.Server.RCONPassword);
                    }

                    List<WSRoomInfo> rooms = paths.Where(x => x.Contains("room")).Select(x => new WSRoomInfo(x)).ToList();
                    List<RCONChannelInfo> channels = paths.Where(x => x.Contains("channel")).Select(x => new RCONChannelInfo(x)).ToList();
                    ServerHubInfo info = new ServerHubInfo()
                    {
                        Version = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                        TotalClients = HubListener.hubClients.Count + RoomsController.GetRoomsList().Sum(x => x.roomClients.Count) + RadioController.radioChannels.Sum(x => x.radioClients.Count),
                        Rooms = rooms,
                        RadioChannels = channels
                    };
                    Send(JsonConvert.SerializeObject(info));
                }catch(Exception e)
                {
                    Logger.Instance.Warning("Unable to send ServerHub info to WebSocket client! Exception: " + e);
                }
            }
        }
    }

    public class Broadcast : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            base.OnOpen();
            Logger.Instance.Log($"WebSocket Client Connected! ");
            
            string[] split = Context.RequestUri.AbsolutePath.Split('/');
            int _id = Convert.ToInt32(split[split.Length - 1]);

            if (split[split.Length - 2] == "room")
            {
                BaseRoom _room = RoomsController.GetRoomsList().FirstOrDefault(room => room.roomId == _id);
                if (_room != null)
                    _room.OnOpenWebSocket();
            }else if(split[split.Length - 2] == "channel")
            {
                RadioChannel _channel = RadioController.radioChannels.FirstOrDefault(channel => channel.channelId == _id);
                if (_channel != null)
                    _channel.OnOpenWebSocket();
            }
        }
    }

    public class RCONBehaviour : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            base.OnOpen();
            Logger.Instance.Log($"RCON Client Connected!");
        }

        protected override void OnMessage(WebSocketSharp.MessageEventArgs e)
        {
            base.OnMessage(e);

            try
            {
                if (e.IsText)
                {
                    IncomingMessage message = JsonConvert.DeserializeObject<IncomingMessage>(e.Data);
#if DEBUG
                    Logger.Instance.Log($"Got message from RCON client: ID={message.Identifier}, Message={message.Message}");
#endif

                    List<string> args = Program.ParseLine(message.Message);
                    var response = Program.ProcessCommand(args[0], args.Skip(1).ToArray());

                    OutgoingMessage outMsg = new OutgoingMessage() { Identifier = message.Identifier, Message = response, Type = "Generic", Stacktrace = "" };

                    Send(JsonConvert.SerializeObject(outMsg));
                }
            }catch(Exception ex)
            {
                Logger.Instance.Warning("Unable to proccess a command from RCON client! Exception: "+ex);
            }
        }
    }

    static class WebSocketListener
    {
        public static WebSocketServer Server;

        public static void Start()
        {
            int webSocketPort = Settings.Instance.Server.WebSocketPort;
            Server = new WebSocketServer(webSocketPort);

            Logger.Instance.Log($"Hosting WebSocket Server @ {Program.GetPublicIPv4()}:{webSocketPort}");
            
            Server.AddWebSocketService<ListServices>("/");
            if (Settings.Instance.Server.EnableWebSocketRCON)
            {
                Server.AddWebSocketService<RCONBehaviour>("/"+Settings.Instance.Server.RCONPassword);
            }
            Server.Start();
        }

        public static void Stop()
        {
            if(Server != null)
                Server.Stop();
        }

        public static void BroadcastState()
        {
            if (Server != null && Settings.Instance.Server.EnableWebSocketRoomInfo)
            {
                List<string> paths = Server.WebSocketServices.Paths.ToList();
                paths.Remove("/");
                if (Settings.Instance.Server.EnableWebSocketRCON)
                {
                    paths.Remove("/" + Settings.Instance.Server.RCONPassword);
                }
                
                List<WSRoomInfo> rooms = paths.Where(x => x.Contains("room")).Select(x => new WSRoomInfo(x)).ToList();
                List<RCONChannelInfo> channels = paths.Where(x => x.Contains("channel")).Select(x => new RCONChannelInfo(x)).ToList();

                string data = JsonConvert.SerializeObject(new { rooms, channels });

                Server.WebSocketServices["/"].Sessions.BroadcastAsync(data, null);
            }
        }

        public static void AddChannel(RadioChannel channel)
        {
            if (Server != null && Settings.Instance.Server.EnableWebSocketRoomInfo)
            {
                Server.AddWebSocketService<Broadcast>($"/channel/{channel.channelId}");
                BroadcastState();
            }
        }

        public static void AddRoom(BaseRoom room)
        {
            if (Server != null && Settings.Instance.Server.EnableWebSocketRoomInfo)
            {
                Server.AddWebSocketService<Broadcast>($"/room/{room.roomId}");
                BroadcastState();
            }
        }

        public static void DestroyRoom(BaseRoom room)
        {
            if (Server != null && Settings.Instance.Server.EnableWebSocketRoomInfo)
            {
                Server.RemoveWebSocketService($"/room/{room.roomId}");
                BroadcastState();
            }
        }
    }
}
