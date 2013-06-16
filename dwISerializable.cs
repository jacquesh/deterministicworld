using Lidgren.Network;

namespace DeterministicWorld
{
    interface dwISerializable
    {
        void serialize(NetOutgoingMessage outMsg);
        void deserialize(NetIncomingMessage inMsg);
    }
}
