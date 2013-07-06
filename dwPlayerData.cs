using System.Collections.Generic;

using Lidgren.Network;

using DeterministicWorld.Network;

namespace DeterministicWorld
{
    public class dwPlayerData : dwISerializable
    {
        public string name;
        public int index;
        public long uid;

        public dwPlayerData() : this("")
        {
        }

        public dwPlayerData(string playerName)
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
            if (obj.GetType() == typeof(dwPlayerData))
            {
                if (uid == ((dwPlayerData)obj).uid)
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
