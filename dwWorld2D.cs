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

        private int playerCountCached;
        private bool playerCountDirty;
        public int playerCount
        {
            get
            {
                if (!playerCountDirty)
                    return playerCountCached;

                int result = 0;

                for (int i = 0; i < dwWorldConstants.GAME_MAX_PLAYERS; i++)
                {
                    if (playerList[i] != null)
                        result += 1;
                }
                result += unindexedPlayers.Count;

                playerCountCached = result;
                return result;
            }
        }

        public event Action onWorldUpdate;

        private HashSet<PlayerData> unindexedPlayers;
        private PlayerData[] playerList;
        
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
            dwWorld2D._instance = this;

            unindexedPlayers = new HashSet<PlayerData>();
            playerList = new PlayerData[dwWorldConstants.GAME_MAX_PLAYERS];

            playerCountCached = 0;
            playerCountDirty = false;

            objects = new List<dwObject2D>();

            currentFrameInput = new FrameInput();
            currentFrame = 0;
            running = false;
            paused = false;

            inputData = new Dictionary<uint, FrameInput>();
        }
        
        //Player list modifications
        public void addPlayer(PlayerData newPlayer)
        {
            unindexedPlayers.Add(newPlayer);
        }

        public void removePlayer(PlayerData player)
        {
            if (player == null)
                return;

            if (unindexedPlayers.Contains(player))
                unindexedPlayers.Remove(player);

            else
                playerList[player.index] = null;

            playerCountDirty = true;
        }

        internal void assignPlayerIndex(long playerUID, int newIndex)
        {
            PlayerData player = null;

            //Get the  player either form unindexed players or from the player list
            HashSet<PlayerData>.Enumerator unindexedEnumerator = unindexedPlayers.GetEnumerator();
            while (unindexedEnumerator.MoveNext())
            {
                PlayerData tempPlayer = unindexedEnumerator.Current;

                if (tempPlayer.uid == playerUID)
                {
                    player = tempPlayer;
                    break;
                }
            }
            if (player != null)
            {
                unindexedPlayers.Remove(player);
                playerCountDirty = true;
            }
            else
            {
                player = getPlayerByUID(playerUID);
            }

            dwLog.info("Attempt to assign index " + newIndex + " to " + player.name);
            if (player.index >= 0)
                playerList[player.index] = null;

            player.index = newIndex;

            if (newIndex >= 0)
                playerList[newIndex] = player;
        }

        //Object interaction
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

        //Order handling
        public virtual void issueInputOrder(dwObject2D obj, Order issuedOrder)
        {
            issuedOrder.owner = obj;
            
            currentFrameInput.addOrder(issuedOrder);
        }

        protected void issueOrder(dwObject2D obj, Order issuedOrder)
        {
            sendOrderToObject(obj, issuedOrder);
        }

        private void sendOrderToObject(dwObject2D obj, Order issuedOrder)
        {
            obj.issueOrder(issuedOrder);
        }

        internal void addFrameInputData(FrameInput frameInput)
        {
            if(inputData.ContainsKey(frameInput.targetFrame))
            {
                inputData[frameInput.targetFrame].mergeFrom(frameInput);
            }
            else
                inputData[frameInput.targetFrame] = frameInput;
        }

        //Data accessors
        public PlayerData getPlayerByUID(long uid)
        {
            for (int i = 0; i < dwWorldConstants.GAME_MAX_PLAYERS; i++)
            {
                if (playerList[i] != null && playerList[i].uid == uid)
                    return playerList[i];
            }

            return null;
        }

        public PlayerData getPlayer(int index)
        {
            return playerList[index];
        }

        public dwObject2D[] getObjects()
        {
            return objects.ToArray();
        }

        internal FrameInput getInputData()
        {
            return currentFrameInput;
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
                Thread.Sleep(1000/FPS);
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
                foreach (Order o in inputData[currentFrame].orderList)
                {
                    sendOrderToObject(o.owner, o);
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
            currentFrameInput = new FrameInput(currentFrame + dwWorldConstants.ORDER_DELAY_TICKS);
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
