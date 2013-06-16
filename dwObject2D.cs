using System.Collections.Generic;

using Lidgren.Network;

namespace DeterministicWorld
{
    public abstract class dwObject2D : dwISerializable
    {
        public dwVector2 position;

        private Order currentOrder;
        private Queue<Order> orderQueue;

        public dwObject2D()
        {
            orderQueue = new Queue<Order>();
        }

        internal virtual void issueOrder(Order newOrder)
        {
            if (orderQueue.Count == 0)
            {
                executeOrder(newOrder);
            }
            else
            {
                orderQueue.Enqueue(newOrder);
            }
        }

        public void clearOrders()
        {
            //TODO stop executing the current order? Maybe?
            orderQueue.Clear();
        }

        private void executeOrder(Order newOrder)
        {
            currentOrder = newOrder;
            newOrder.owner = this;
            newOrder.execute();
        }

        internal void orderComplete()
        {
            if (orderQueue.Count > 0)
            {
                executeOrder(orderQueue.Dequeue());
            }
        }

        internal void update_internal()
        {
            if(currentOrder != null)
                currentOrder.OnUpdate();

            update();
        }

        /// <summary>
        /// Runs a single game state update tick
        /// </summary>
        public abstract void update();


        public void serialize(NetOutgoingMessage outMsg)
        {
            //We never want to actually send an object over the network, we'd only need a reference to the object
            //So we dont need to send all of this object's data, only its id
            throw new System.NotImplementedException();
        }

        public void deserialize(NetIncomingMessage inMsg)
        {
            throw new System.NotImplementedException();
        }
    }
}
