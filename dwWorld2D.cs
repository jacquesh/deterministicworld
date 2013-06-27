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

            currentFrame = 0;
            running = false;
            paused = false;

            inputData = new Dictionary<uint, FrameInput>();
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
                obj.destroy();
            }
        }

        public virtual void sendOrderInput(dwObject2D obj, Order issuedOrder)
        {
            issuedOrder.owner = obj;
            
            //By now the order has stored the given object as it's owner
            if (!inputData.ContainsKey(currentFrame))
            {
                inputData[currentFrame] = new FrameInput(currentFrame);
            }
            inputData[currentFrame].addOrder(issuedOrder);
        }

        protected internal void issueOrder(dwObject2D obj, Order issuedOrder)
        {
            obj.issueOrder(issuedOrder);
        }

        public PlayerData getPlayer(long uid)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].uid == uid)
                    return players[i];
            }

            return null;
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
            currentFrameInput = new FrameInput(currentFrame);
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
}
