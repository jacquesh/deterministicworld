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
            throw new NotImplementedException();
        }

        public void deserialize(NetIncomingMessage inMsg)
        {
            throw new NotImplementedException();
        }
    }
}
