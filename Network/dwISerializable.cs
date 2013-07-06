using Lidgren.Network;

namespace DeterministicWorld.Network
{
    internal interface dwISerializable
    {
        void serialize(NetOutgoingMessage outMsg);
        void deserialize(NetIncomingMessage inMsg);
    }
}
