using System.Collections.Generic;

using Lidgren.Network;

namespace DeterministicWorld
{
    public abstract class dwObject2D
    {
        public dwVector2 position;

        private Order currentOrder;
        private Queue<Order> orderQueue;

        private int id;
        
        public dwObject2D()
        {
            orderQueue = new Queue<Order>();
        }

        public int getID()
        {
            return id;
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
            outMsg.Write(getID());
        }

        public static dwObject2D deserialize(NetIncomingMessage inMsg)
        {
            return dwObject2D.getObject(inMsg.ReadInt32());
        }


        //Indexing
        //========
        private static dwObject2D[] indexedObjects;
        private static Queue<int> freeIds;
        private static int nextId;
        private static int maxId;

        static dwObject2D()
        {
            indexedObjects = new dwObject2D[10];
            freeIds = new Queue<int>();
            maxId = -1;
        }

        public static dwObject2D getObject(int obj_id)
        {
            if (obj_id < 0 || obj_id > maxId)
            {
                throw new System.IndexOutOfRangeException("Attempt to get the object for an invalid object ID - "+obj_id);
                return null;
            }

            if (indexedObjects[obj_id] == null)
            {
                throw new System.IndexOutOfRangeException("OMGWTFBQQ null indexed object at index " + obj_id + "? We have " + indexedObjects.Length+" objects currently");
            }

            return indexedObjects[obj_id];
        }

        internal static void indexObject(dwObject2D obj)
        {
            if (obj == null)
                throw new System.IndexOutOfRangeException("OMGWTFBQQ trying to index null object?");
                //return;

            if (freeIds.Count == 0)
            {
                obj.id = nextId;
                nextId++;

                maxId = obj.id;
            }
            else
            {
                obj.id = freeIds.Dequeue();
            }

            //TODO: Resize the array if necessary
            indexedObjects[obj.id] = obj;
        }

        internal static void deindexObject(dwObject2D obj)
        {
            if (obj.id == nextId - 1)
            {
                nextId--;
            }
            else
            {
                freeIds.Enqueue(obj.id);
            }

            indexedObjects[obj.id] = null;
            obj.id = -1;
        }
    }
}
