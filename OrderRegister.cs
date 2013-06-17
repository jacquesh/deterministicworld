using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeterministicWorld
{
    public class OrderRegister
    {

        public static OrderRegister instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new OrderRegister();
                }
                return _instance;
            }
        }
        private static OrderRegister _instance;

        private Dictionary<int, Type> orderRegister;
        private Dictionary<Type, int> reverseOrderRegister;

        private OrderRegister()
        {
            orderRegister = new Dictionary<int, Type>();
            reverseOrderRegister = new Dictionary<Type, int>();
        }

        public void registerOrderType(Type orderType)
        {
            if (orderType.BaseType != typeof(Order))
            {
                return;
            }

            int orderID = orderRegister.Count;
            orderRegister[orderID] = orderType;
            reverseOrderRegister[orderType] = orderID;
        }

        public Type idToOrder(int orderID)
        {
            if (orderRegister.ContainsKey(orderID))
            {
                return orderRegister[orderID];
            }

            return null;
        }

        public int orderToID(Type orderType)
        {
            if (reverseOrderRegister.ContainsKey(orderType))
            {
                return reverseOrderRegister[orderType];
            }

            return -1;
        }

    }
}
