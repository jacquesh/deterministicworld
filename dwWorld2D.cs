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
            unindexedPlayers = new HashSet<PlayerData>();
            playerList = new PlayerData[dwWorldConstants.GAME_MAX_PLAYERS];

            playerCountCached = 0;
            playerCountDirty = false;

            objects = new List<dwObject2D>();

            currentFrame = 0;
            running = false;
            paused = false;

            inputData = new Dictionary<uint, FrameInput>();
        }

        public void addPlayer(PlayerData newPlayer)
        {
            unindexedPlayers.Add(newPlayer);
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
