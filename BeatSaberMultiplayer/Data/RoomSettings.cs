﻿using Lidgren.Network;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BeatSaberMultiplayer.Data
{
    public class RoomSettings
    {
        public string Name;

        public bool UsePassword;
        public string Password;

        public SongSelectionType SelectionType;
        public int MaxPlayers;
        public bool NoFail;
        
        public RoomSettings()
        {

        }

        public RoomSettings(JSONNode node)
        {
            Name = node["Name"];
            UsePassword = node["UsePassword"];
            Password = node["Password"];
            SelectionType = (SongSelectionType)node["SelectionType"].AsInt;
            MaxPlayers = node["MaxPlayers"];
            NoFail = node["NoFail"];
        }
        
        public RoomSettings(NetIncomingMessage msg)
        {

            Name = msg.ReadString();

            UsePassword = msg.ReadBoolean();
            NoFail = msg.ReadBoolean();

            msg.SkipPadBits();

            if (UsePassword)
                Password = msg.ReadString();

            MaxPlayers = msg.ReadInt32();
            SelectionType = (SongSelectionType)msg.ReadByte();

        }
        
        public void AddToMessage(NetOutgoingMessage msg)
        {
            msg.Write(Name);

            msg.Write(UsePassword);
            msg.Write(NoFail);

            msg.WritePadBits();

            if (UsePassword)
                msg.Write(Password);

            msg.Write(MaxPlayers);
            msg.Write((byte)SelectionType);
        }

        public override bool Equals(object obj)
        {
            if (obj is RoomSettings)
            {
                return (Name == ((RoomSettings)obj).Name) && (UsePassword == ((RoomSettings)obj).UsePassword) && (Password == ((RoomSettings)obj).Password) && (MaxPlayers == ((RoomSettings)obj).MaxPlayers) && (NoFail == ((RoomSettings)obj).NoFail);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            var hashCode = -1123100830;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + UsePassword.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Password);
            hashCode = hashCode * -1521134295 + MaxPlayers.GetHashCode();
            hashCode = hashCode * -1521134295 + NoFail.GetHashCode();
            return hashCode;
        }
    }
}
