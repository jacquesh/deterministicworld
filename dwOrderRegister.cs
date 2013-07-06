using System;
using System.Collections.Generic;

namespace DeterministicWorld
{
    public class dwOrderRegister
    {

        public static dwOrderRegister instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new dwOrderRegister();
                }
                return _instance;
            }
        }
        private static dwOrderRegister _instance;

        private Dictionary<int, Type> orderRegister;
        private Dictionary<Type, int> reverseOrderRegister;

        private dwOrderRegister()
        {
            orderRegister = new Dictionary<int, Type>();
            reverseOrderRegister = new Dictionary<Type, int>();
        }

        public void registerOrderType(Type orderType)
        {
            if (orderType.BaseType != typeof(dwOrder))
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
            Console.WriteLine("==========ERROR: Attempt to get the type of an invalid order ID==========");
            return null;
        }

        public int orderToID(Type orderType)
        {
            if (reverseOrderRegister.ContainsKey(orderType))
            {
                return reverseOrderRegister[orderType];
            }
            Console.WriteLine("==========ERROR: Attempt to get the ID of an invalid order type==========");
            return -1;
        }

    }
}
