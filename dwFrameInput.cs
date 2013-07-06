using System;
using System.Collections.Generic;

using Lidgren.Network;

using DeterministicWorld.Network;
using DeterministicWorld.Orders;

namespace DeterministicWorld
{
    internal class dwFrameInput : dwISerializable
    {
        public List<dwOrder> orderList;
        internal uint targetFrame;

        public dwFrameInput()
        {
            orderList = new List<dwOrder>();
            targetFrame = 0;
        }

        public dwFrameInput(uint frameIndex)
        {
            orderList = new List<dwOrder>();
            targetFrame = frameIndex;
        }

        public void addOrder(dwOrder issuedOrder)
        {
            orderList.Add(issuedOrder);
        }

        public void mergeFrom(dwFrameInput other)
        {
            orderList.AddRange(other.orderList);
        }

        public void serialize(NetOutgoingMessage outMsg)
        {
            outMsg.Write(targetFrame);
            outMsg.Write(orderList.Count);

            for (int i = 0; i < orderList.Count; i++)
            {
                orderList[i].serialize(outMsg);
            }
        }

        public void deserialize(NetIncomingMessage inMsg)
        {
            targetFrame = inMsg.ReadUInt32();
            orderList.Capacity = inMsg.ReadInt32();

            for (int i = 0; i < orderList.Capacity; i++)
            {
                int orderID = inMsg.ReadInt32();
                dwOrder newOrder = (dwOrder)Activator.CreateInstance(dwOrderRegister.instance.idToOrder(orderID));
                newOrder.deserialize(inMsg);
                
                orderList.Add(newOrder);
            }
        }
    }
}
