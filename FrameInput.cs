﻿using System;
using System.Collections.Generic;

using Lidgren.Network;

namespace DeterministicWorld
{
    internal class FrameInput : dwISerializable
    {
        public List<Order> orderList;

        public FrameInput()
        {
            orderList = new List<Order>();
        }

        public void addOrder(Order issuedOrder)
        {
            orderList.Add(issuedOrder);
        }

        public void serialize(NetOutgoingMessage outMsg)
        {
            //outMsg.Write(targetFrame);
            outMsg.Write(orderList.Count);

            for (int i = 0; i < orderList.Count; i++)
            {
                orderList[i].serialize(outMsg);
            }
        }

        public void deserialize(NetIncomingMessage inMsg)
        {
            //targetFrame = inMsg.ReadUInt32();
            orderList.Capacity = inMsg.ReadInt32();

            for (int i = 0; i < orderList.Count; i++)
            {
                int orderID = inMsg.ReadInt32();
                Order newOrder = (Order)Activator.CreateInstance(OrderRegister.instance.idToOrder(orderID));
                newOrder.deserialize(inMsg);

                orderList.Add(newOrder);
            }
        }
    }
}
