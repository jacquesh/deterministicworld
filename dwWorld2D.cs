using System;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;

using DeterministicWorld.Orders;

namespace DeterministicWorld
{
    public abstract partial class dwWorld2D
    {
        public uint gameFrame
        {
            get { return currentFrame; }
        }
        
        public event Action onWorldUpdate;

        private uint currentFrame;
        private bool running;
        private bool paused;
        
        private Thread simulationThread;

        //======================
        public dwWorld2D()
        {
            dwWorld2D._instance = this;
            
            unindexedPlayers = new HashSet<dwPlayerData>();
            playerList = new dwPlayerData[dwWorldConstants.GAME_MAX_PLAYERS];

            playerCountCached = 0;
            playerCountDirty = false;

            objects = new List<dwObject2D>();
            
            currentFrameInput = new dwFrameInput();
            currentFrame = 0;
            running = false;
            paused = false;

            inputData = new Dictionary<uint, dwFrameInput>();
        }

        //Simulation flow control
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

        //Internal functions
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
                Thread.Sleep(dwWorldConstants.GAME_TICK_MILLIS);
            }
        }

        private void initialize()
        {
            worldStart();
        }
        
        private void update()
        {
            //Issue all of the scheduled orders
            if (inputData.ContainsKey(currentFrame))
            {
                foreach (dwOrder o in inputData[currentFrame].orderList)
                {
                    issueOrder(o.owner, o);
                }

                //Clear the data from the input table
                inputData.Remove(currentFrame);
            }

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
            currentFrameInput = new dwFrameInput(currentFrame + dwWorldConstants.ORDER_DELAY_TICKS);
        }

        //Abstract events
        protected abstract void worldStart();
        protected abstract void worldUpdate();

        //Singleton data
        private static dwWorld2D _instance = null;
        public static dwWorld2D instance
        {
            get { return _instance; }
        }
    }
}
