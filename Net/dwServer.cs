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

    public class dwServer
    {
        private Thread serverThread;

        private NetPeerConfiguration peerConfig;
        private NetServer netServer;

        private dwWorld2D serverWorld;

        private bool running;

        public dwServer(dwWorld2D world)
        {
            dwLog.info("Initializing NetServer...");

            //Setup the connection between the server and the world
            serverWorld = world;

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
            dwLog.info("Starting NetServer...");
            netServer.Start();
            dwLog.info("NetServer Running with UID "+netServer.UniqueIdentifier);

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
                                case(NetDataType.PlayerConnect):
                                    setupNewPlayer(inMsg);
                                    break;

                                case(NetDataType.PlayerIndexUpdate):
                                    playerIndexUpdateRequest(inMsg);
                                    break;

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
                                    
                                    netServer.SendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);

                                    break;
                            }
                            break;

                        //A client's status changed (e.g connected/disconnected/connecting/disconnecting)
                        case (NetIncomingMessageType.StatusChanged):
                            updatePlayerStatus(inMsg);
                            break;

                        default:
                            dwLog.info(inMsg.MessageType + " Contents: " + inMsg.ReadString());
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
                dwLog.info("Denied connection from " + inMsg.SenderConnection.RemoteEndPoint + ". REASON: Game started");
            }
            else if (gameId != dwWorldConstants.GAME_ID)
            {
                inMsg.SenderConnection.Deny("Invalid game ID, are you connecting to the right game?");
                int comp = gameId.CompareTo(dwWorldConstants.GAME_ID);
                dwLog.info("Denied connection from " + inMsg.SenderConnection.RemoteEndPoint + ". REASON: Incorrect Game ID - Is " + gameId + " should be " + dwWorldConstants.GAME_ID + " -> " + comp);
            }
            else if (gameVersion != dwWorldConstants.GAME_VERSION)
            {
                inMsg.SenderConnection.Deny("Game version mismatch, ensure that you have the same version as the server");
                dwLog.info("Denied connection from " + inMsg.SenderConnection.RemoteEndPoint + ". REASON: Incorrect Game Version");
            }
            else
            {
                //Accept request and set up new player
                inMsg.SenderConnection.Approve();
            }
        }

        private void updatePlayerStatus(NetIncomingMessage inMsg)
        {
            NetConnectionStatus newStatus = inMsg.SenderConnection.Status;

            if (newStatus == NetConnectionStatus.RespondedAwaitingApproval)
                return;

            switch (newStatus)
            {
                case (NetConnectionStatus.InitiatedConnect):
                    break;

                case(NetConnectionStatus.Connected):
                    break;

                case(NetConnectionStatus.Disconnecting):
                    break;

                case(NetConnectionStatus.Disconnected):
                    //playerList.RemoveAt(playerListIndex);
                    break;

                default:
                    break;
            }
            
            //PlayerData statusPlayer = serverWorld.getPlayer(inMsg.SenderConnection.RemoteUniqueIdentifier);
            //dwLog.info(statusPlayer.name + " " + newStatus);
        }

        private void playerIndexUpdateRequest(NetIncomingMessage inMsg)
        {
            long playerUID = inMsg.ReadInt64();
            int newIndex = inMsg.ReadInt32();

            if (serverWorld.getPlayers()[newIndex] == null)
            {
                updatePlayerIndex(playerUID, newIndex);
            }
        }

        //===================================
        //Server processing/outgoing messages
        //===================================
        private void setupNewPlayer(NetIncomingMessage connectionMsg)
        {
            NetOutgoingMessage outMsg;
            PlayerData[] playerList = serverWorld.getPlayers();

            //Create new player data
            PlayerData newPlayer = new PlayerData();
            newPlayer.deserialize(connectionMsg);
            
            //Tell the new player about all the players in this game
            for (int i = 0; i < playerList.Length; i++)
            {
                if (playerList[i] == null)
                    continue;

                outMsg = netServer.CreateMessage();
                outMsg.Write((byte)NetDataType.PlayerConnect);
                playerList[i].serialize(outMsg);

                netServer.SendMessage(outMsg, connectionMsg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
            }

            //Tell all players in this game about the new player
            outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.PlayerConnect);
            newPlayer.serialize(outMsg);
            netServer.SendToAll(outMsg, connectionMsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);

            //Update netServer player list
            serverWorld.addPlayer(newPlayer);

            //Assign an index to the new player
            for (int i = 0; i < dwWorldConstants.GAME_MAX_PLAYERS; i++)
            {
                if (PlayerData.getPlayer(i) == null)
                {
                    updatePlayerIndex(newPlayer.uid, i);
                    break;
                }
            }
        }

        private void updatePlayerIndex(long playerUID, int newIndex)
        {

            NetOutgoingMessage outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.PlayerIndexUpdate);
            outMsg.Write(playerUID);
            outMsg.Write(newIndex);

            netServer.SendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);
        }

        private void startGame()
        {
            NetOutgoingMessage outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.StartGame);

            netServer.SendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);

            serverWorld.startSimulation();
        }
    }
}
