using System;

using DeterministicWorld.Net;

using Lidgren.Network;

namespace DeterministicWorld
{
    public class PlayerData : dwISerializable
    {
        public string name;

        public void serialize(NetOutgoingMessage outMsg)
        {
            outMsg.Write(name);
        }

        public void deserialize(NetIncomingMessage inMsg)
        {
            name = inMsg.ReadString();
        }
    }
}
