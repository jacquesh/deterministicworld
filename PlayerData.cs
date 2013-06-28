using System;
using System.Collections.Generic;

using DeterministicWorld.Net;

using Lidgren.Network;

namespace DeterministicWorld
{
    public class PlayerData : dwISerializable
    {
        public string name;
        public int index;
        public long uid;

        public PlayerData() : this("")
        {
        }

        public PlayerData(string playerName)
        {
            index = -1;
            name = playerName;
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
