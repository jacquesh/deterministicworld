using System.Collections.Generic;

using Lidgren.Network;

using DeterministicWorld.Network;
using DeterministicWorld.Orders;

namespace DeterministicWorld
{
    public abstract class dwObject2D : dwIIdentifiable
    {
        public dwPlayerData owner;

        public dwVector2 position;

        private int lifeticksRemaining;

        private dwOrder currentOrder;
        private Queue<dwOrder> orderQueue;

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

        public dwObject2D(dwPlayerData owningPlayer)
        {
            if (owningPlayer == null)
                throw new System.ArgumentNullException();

            indexer.indexObject(this);
            owner = owningPlayer;

            orderQueue = new Queue<dwOrder>();
            position = new dwVector2(0, 0);
            lifeticksRemaining = -1;
        }

        ~dwObject2D()
        {
            indexer.deindexObject(this);
        }

        public void AddTimedLife(int ticks)
        {
            lifeticksRemaining = ticks;
        }

        internal virtual void issueOrder(dwOrder newOrder)
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

        private void executeOrder(dwOrder newOrder)
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
            if (lifeticksRemaining > 0)
                lifeticksRemaining--;

            if (lifeticksRemaining == 0)
                dwWorld2D.instance.removeObject(this);

            if(currentOrder != null)
                currentOrder.OnUpdate();

            update();
        }

        /// <summary>
        /// Runs a single game state update tick
        /// </summary>
        protected abstract void update();

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
