using System.Collections.Generic;

using Lidgren.Network;

namespace DeterministicWorld
{
    public abstract class dwObject2D : dwIdentifiable
    {
        public PlayerData owner;

        public dwVector2 position;

        private Order currentOrder;
        private Queue<Order> orderQueue;

        public int id
        {
            get;
            set;
        }

        private static dwIndexer<dwObject2D> indexer;
        
        static dwObject2D()
        {
            indexer = new dwIndexer<dwObject2D>();
        }

        public dwObject2D(PlayerData owningPlayer)
        {
            if (owningPlayer == null)
                throw new System.ArgumentNullException();

            indexer.indexObject(this);

            orderQueue = new Queue<Order>();
            position = new dwVector2(0, 0);

            owner = owningPlayer;
        }

        internal void destroy()
        {
            indexer.deindexObject(this);
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

        //Serialization
        //=============
        public void serialize(NetOutgoingMessage outMsg)
        {
            //We never want to actually send an object over the network, we'd only need a reference to the object
            //So we dont need to send all of this object's data, only its id
            outMsg.Write(id);
        }

        public static dwObject2D deserialize(NetIncomingMessage inMsg)
        {
            return dwObject2D.indexer.getObject(inMsg.ReadInt32());
        }
    }
}
