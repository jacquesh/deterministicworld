using System;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;

namespace DeterministicWorld
{
    public abstract class dwWorld2D
    {
        public static int FPS = 20;

        public uint gameFrame
        {
            get { return currentFrame; }
        }

        public int playerCount
        {
            get { return players.Count; }
        }

        public event Action onWorldUpdate;

        private List<PlayerData> players;
        private List<dwObject2D> objects;

        private Dictionary<int, Type> orderRegister;
        private Dictionary<Type, int> reverseOrderRegister;

        private uint currentFrame;
        private bool running;
        private bool paused;

        private FrameInput currentFrameInput;

        private Thread simulationThread;

        private Dictionary<uint, FrameInput> inputData;

        private readonly FrameInput emptyInput = new FrameInput();

        //======================
        public dwWorld2D()
        {
            players = new List<PlayerData>();
            objects = new List<dwObject2D>();

            orderRegister = new Dictionary<int, Type>();
            reverseOrderRegister = new Dictionary<Type, int>();

            currentFrame = 0;
            running = false;
            paused = false;

            inputData = new Dictionary<uint, FrameInput>();
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

        public void addPlayer(PlayerData newPlayer)
        {
            players.Add(newPlayer);
        }

        public void addObject(dwObject2D obj)
        {
            if (obj != null)
            {
                objects.Add(obj);
            }
        }

        public void removeObject(dwObject2D obj)
        {
            if (obj != null)
            {
                objects.Remove(obj);
            }
        }

        public virtual void issueOrder(dwObject2D obj, Order issuedOrder)
        {
            //obj.issueOrder(issuedOrder);
            
            //By now the order has stored the given object as it's owner
            if (!inputData.ContainsKey(currentFrame))
            {
                inputData[currentFrame] = new FrameInput();
            }
            inputData[currentFrame].addOrder(issuedOrder);
        }

        public void scheduleOrder(dwObject2D obj, Order issuedOrder, uint targetFrame)
        {

        }

        public PlayerData[] getPlayers()
        {
            return players.ToArray();
        }

        public dwObject2D[] getObjects()
        {
            return objects.ToArray();
        }

        public void startSimulation()
        {
            if (simulationThread == null)
            {
                running = true;
                simulationThread = new Thread(threadStart);
                simulationThread.Start();
            }
        }

        public void stopSimulation()
        {
            if(simulationThread != null)
            {
                running = false;
                simulationThread.Join();
                simulationThread = null;
            }
        }

        private void threadStart()
        {
            running = true;

            initialize();
            while (running)
            {
                if (!paused)
                {
                    update();
                }
                Thread.Sleep(1000/FPS);
            }
        }

        private void initialize()
        {
            worldStart();
        }

        private void update()
        {
            //Call an update on all of the world's objects
            for (int i = 0; i < objects.Count; i++)
            {
                dwObject2D currentObject = objects[i];

                currentObject.update_internal();
            }

            worldUpdate();

            //Call any specified external update functions
            if (onWorldUpdate != null)
                onWorldUpdate();

            //Increment time
            currentFrame++;
            
            //Set up data for the next frame (thereby allowing user interaction to occur independant of game ticks,
            // because there is always something to notify of user input/actions
            currentFrameInput = new FrameInput();
        }

        internal FrameInput getInputData(uint frame)
        {
            if (inputData.ContainsKey(frame))
            {
                return inputData[frame];
            }

            return emptyInput;
        }

        protected abstract void worldStart();
        protected abstract void worldUpdate();
    }

    internal class FrameInput
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
    }

}
