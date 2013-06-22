using Lidgren.Network;

namespace DeterministicWorld
{
    internal interface dwISerializable
    {
        void serialize(NetOutgoingMessage outMsg);
        void deserialize(NetIncomingMessage inMsg);
    }
}
