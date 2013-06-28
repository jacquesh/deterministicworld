using System;
using System.Collections.Generic;

using Lidgren.Network;

namespace DeterministicWorld
{
    internal class FrameInput : dwISerializable
    {
        public List<Order> orderList;
        private uint targetFrame;

        public FrameInput()
        {
            orderList = new List<Order>();
            targetFrame = 0;
        }

        public FrameInput(uint frameIndex)
        {
            orderList = new List<Order>();
            targetFrame = frameIndex;
        }

        public void addOrder(Order issuedOrder)
        {
            orderList.Add(issuedOrder);
        }

        public void serialize(NetOutgoingMessage outMsg)
        {

            outMsg.Write(targetFrame);
            outMsg.Write(orderList.Count);

            for (int i = 0; i < orderList.Count; i++)
            {
                //Console.WriteLine("Serialize order owned by " + orderList[i].owner.owner.idString);
                Console.WriteLine("Serialize order owned by " + orderList[i].owner.owner.uid);
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
                Order newOrder = (Order)Activator.CreateInstance(OrderRegister.instance.idToOrder(orderID));
                newOrder.deserialize(inMsg);

                //Console.WriteLine("Deserialize order owned by " + newOrder.owner.owner.idString);
                Console.WriteLine("Deserialize order owned by " + newOrder.owner.owner.uid);
                orderList.Add(newOrder);
            }
        }
    }
}
