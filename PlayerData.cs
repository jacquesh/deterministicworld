using System;
using System.Collections.Generic;

using DeterministicWorld.Net;

using Lidgren.Network;

namespace DeterministicWorld
{
    public class PlayerData : dwISerializable
    {
        public static PlayerData getPlayer(int index)
        {
            return players[index];
        }
        private static PlayerData[] players;

        public string name;
        public int index;
        public long uid;

        static PlayerData()
        {
            players = new PlayerData[dwWorldConstants.GAME_MAX_PLAYERS];
        }

        public PlayerData() : this("")
        {
        }

        public PlayerData(string playerName)
        {
            index = -1;
            name = playerName;
        }

        internal void assignIndex(int newIndex)
        {
            dwLog.info("Attempt to assign index " + newIndex + " to " + name);
            if(this.index >= 0)
                PlayerData.players[this.index] = null;
            
            this.index = newIndex;

            if(newIndex >= 0)
                PlayerData.players[newIndex] = this;
        }

        public void serialize(NetOutgoingMessage outMsg)
        {
            outMsg.Write(uid);
            outMsg.Write(name);
        }

        public void deserialize(NetIncomingMessage inMsg)
        {
            uid = inMsg.ReadInt64();
            name = inMsg.ReadString();
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(PlayerData))
            {
                if (uid == ((PlayerData)obj).uid)
                    return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
