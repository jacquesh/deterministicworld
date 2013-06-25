using System;
using System.Threading;
using System.Collections.Generic;

using Lidgren.Network;

using System.Reflection;

namespace DeterministicWorld.Net
{
    public enum NetDataType
    {
        None,

        PlayerConnect,
        PlayerDisconnect,
        PlayerIndexUpdate,

        FrameUpdate,

        StartGame,
    }

    class ServerPlayerData
    {
        public PlayerData playerData;
        public NetConnection connection;
    }

    public class dwServer
    {
        private Thread serverThread;

        private NetPeerConfiguration peerConfig;
        private NetServer netServer;

        private List<ServerPlayerData> playerList;

        private dwWorld2D serverWorld;

        private bool running;

        public dwServer(dwWorld2D world)
        {
            Console.WriteLine("Initializing netServer...");

            //Setup the connection between the server and the world
            serverWorld = world;

            //Create the player list
            playerList = new List<ServerPlayerData>();

            //Create the network configuration
            peerConfig = new NetPeerConfiguration(dwWorldConstants.GAME_ID);
            peerConfig.Port = dwWorldConstants.GAME_NET_PORT;
            peerConfig.MaximumConnections = 200;
            peerConfig.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            //Create the server instance
            netServer = new NetServer(peerConfig);
            running = false;

            //Create the server thread
            serverThread = new Thread(threadStart);
        }

        ~dwServer()
        {
            serverThread.Join();
        }

        //========================
        //Server Control Functions
        //========================
        public void start()
        {
            running = true;
            serverThread.Start();

            while (running)
            {
                //Handle server console commands?
                Console.ReadLine();
            }
        }

        public void shutdown()
        {
            running = false;
            netServer.Shutdown("Server shut down");
        }

        //======================
        //Server thread run/loop
        //======================
        private void threadStart()
        {
            //Console.WriteLine("Starting netServer...");
            dwLog.logger.Info("Starting NetServer...");
            netServer.Start();
            //Console.WriteLine("Server running...");
            dwLog.logger.Info("NetServer Running...");

            NetIncomingMessage inMsg;
            while (running && (netServer.Status == NetPeerStatus.Starting || netServer.Status == NetPeerStatus.Running))
            {
                inMsg = netServer.ReadMessage();

                if (inMsg != null)
                {
                    switch (inMsg.MessageType)
                    {
                        //Client attempting to create a connection
                        case (NetIncomingMessageType.ConnectionApproval):
                            clientConnectionRequest(inMsg);
                            break;

                        //App-specific data
                        case (NetIncomingMessageType.Data):
                            NetDataType dataType = (NetDataType)inMsg.ReadByte();

                            switch (dataType)
                            {
                                case (NetDataType.StartGame):
                                    startGame();
                                    break;

                                case (NetDataType.FrameUpdate):
                                    //Read in the packet
                                    FrameInput input = new FrameInput();
                                    input.deserialize(inMsg);

                                    //Write it to a new packet
                                    NetOutgoingMessage outMsg = netServer.CreateMessage();
                                    outMsg.Write((byte)NetDataType.FrameUpdate);
                                    input.serialize(outMsg);

                                    sendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);

                                    break;
                            }
                            break;

                        //A client's status changed (e.g connected/disconnected/connecting/disconnecting)
                        case (NetIncomingMessageType.StatusChanged):
                            updatePlayerStatus(inMsg);
                            break;

                        default:
                            Console.WriteLine(inMsg.MessageType+" Contents: " + inMsg.ReadString());
                            break;
                    }
                }
            }
        }

        //=================================
        //Server incoming message responses
        //=================================
        private void clientConnectionRequest(NetIncomingMessage inMsg)
        {
            string gameId = inMsg.ReadString();
            int gameVersion = inMsg.ReadInt32();

            if (serverWorld.gameFrame != 0)
            {
                inMsg.SenderConnection.Deny("The game has already started");
                Console.WriteLine("Denied connection from " + inMsg.SenderConnection.RemoteEndpoint + ". REASON: Game started");
            }
            else if (gameId != dwWorldConstants.GAME_ID)
            {
                inMsg.SenderConnection.Deny("Invalid game ID, are you connecting to the right game?");
                int comp = gameId.CompareTo(dwWorldConstants.GAME_ID);
                Console.WriteLine("Denied connection from " + inMsg.SenderConnection.RemoteEndpoint + ". REASON: Incorrect Game ID - Is " + gameId + " should be " + dwWorldConstants.GAME_ID + " -> " + comp);
            }
            else if (gameVersion != dwWorldConstants.GAME_VERSION)
            {
                inMsg.SenderConnection.Deny("Game version mismatch, ensure that you have the same version as the server");
                Console.WriteLine("Denied connection from " + inMsg.SenderConnection.RemoteEndpoint + ". REASON: Incorrect Game Version");
            }
            else
            {
                //Accept request and set up new player
                inMsg.SenderConnection.Approve();
                setupNewPlayer(inMsg);
            }
        }

        private void updatePlayerStatus(NetIncomingMessage inMsg)
        {
            NetConnectionStatus newStatus = inMsg.SenderConnection.Status;

            PlayerData statusPlayer = null;
            int playerListIndex = -1;

            for (int i = 0; i < playerList.Count; i++)
            {
                if (inMsg.SenderConnection == playerList[i].connection)
                {
                    statusPlayer = playerList[i].playerData;
                    playerListIndex = i;
                    break;
                }
            }

            Console.WriteLine(statusPlayer.name + " " + newStatus);

            switch (newStatus)
            {
                case (NetConnectionStatus.Connecting):
                    break;

                case(NetConnectionStatus.Connected):
                    break;

                case(NetConnectionStatus.Disconnecting):
                    break;

                case(NetConnectionStatus.Disconnected):
                    //playerList.RemoveAt(playerListIndex);
                    break;

                case(NetConnectionStatus.None):
                    break;
            }
        }

        //===================================
        //Server processing/outgoing messages
        //===================================
        private void setupNewPlayer(NetIncomingMessage connectionMsg)
        {
            NetOutgoingMessage outMsg;

            //Create new player data
            ServerPlayerData newPlayer = new ServerPlayerData();
            newPlayer.connection = connectionMsg.SenderConnection;

            newPlayer.playerData = new PlayerData();
            assignInitialSlotToPlayer(newPlayer.playerData);
            newPlayer.playerData.deserialize(connectionMsg);

            outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.PlayerIndexUpdate);
            outMsg.Write(-1); //Change player @ -1 (localplayer)
            outMsg.Write(newPlayer.playerData.index); //To the index we generated
            netServer.SendMessage(outMsg, newPlayer.connection, NetDeliveryMethod.ReliableOrdered);

            //Tell the new player about all the players in this game
            for (int i = 0; i < playerList.Count; i++)
            {
                outMsg = netServer.CreateMessage();
                outMsg.Write((byte)NetDataType.PlayerConnect);
                playerList[i].playerData.serialize(outMsg);

                netServer.SendMessage(outMsg, newPlayer.connection, NetDeliveryMethod.ReliableOrdered);
            }

            //Tell all players in this game about the new player
            outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.PlayerConnect);
            newPlayer.playerData.serialize(outMsg);
            sendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);

            //Update netServer player list
            serverWorld.addPlayer(newPlayer.playerData);
            playerList.Add(newPlayer);
        }

        private void assignInitialSlotToPlayer(PlayerData player)
        {
            for (int i = 0; i < dwWorldConstants.GAME_MAX_PLAYERS; i++)
            {
                if (PlayerData.getPlayer(i) == null)
                {
                    player.assignIndex(i);
                    break;
                }
            }
        }

        private void startGame()
        {
            NetOutgoingMessage outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.StartGame);

            sendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);

            serverWorld.startSimulation();
        }

        //=================
        //Utility Functions
        //=================

        /// <summary>
        /// Send a packet to all clients
        /// </summary>
        /// <param name="outMsg"></param>
        /// <param name="deliveryMethod"></param>
        private void sendToAll(NetOutgoingMessage outMsg, NetDeliveryMethod deliveryMethod)
        {
            for (int i = 0; i < playerList.Count; i++)
            {
                netServer.SendMessage(outMsg, playerList[i].connection, deliveryMethod);
            }
        }
    }
}
