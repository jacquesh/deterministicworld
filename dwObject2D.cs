using System.Collections.Generic;

namespace DeterministicWorld
{
    public abstract class dwObject2D
    {
        public dwVector2 position;

        private Order currentOrder;
        private Queue<Order> orderQueue;

        public dwObject2D()
        {
            orderQueue = new Queue<Order>();
        }

        public virtual void issueOrder(Order newOrder)
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
            currentOrder.OnUpdate();
            update();
        }

        /// <summary>
        /// Runs a single game state update tick
        /// </summary>
        public abstract void update();
    }
}
