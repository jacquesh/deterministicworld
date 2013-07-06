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
                                    relayFrameInput(inMsg);
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
            else if (serverWorld.playerCount >= dwWorldConstants.GAME_MAX_PLAYERS)
            {
                inMsg.SenderConnection.Deny("Server is full");
                dwLog.info("Denied connection from " + inMsg.SenderConnection.RemoteEndPoint + ". REASON: Server is full");
            }
            else
            {
                dwLog.info("Accept connection from " + inMsg.SenderConnection.RemoteEndPoint);
                //Accept request and set up new player
                inMsg.SenderConnection.Approve();
            }
        }

        private void updatePlayerStatus(NetIncomingMessage inMsg)
        {
            NetConnectionStatus newStatus = inMsg.SenderConnection.Status;

            PlayerData statusPlayer = serverWorld.getPlayerByUID(inMsg.SenderConnection.RemoteUniqueIdentifier);
            if (statusPlayer == null)
                return;

            dwLog.info(statusPlayer.name + " " + newStatus);

            switch (newStatus)
            {
                case (NetConnectionStatus.InitiatedConnect):
                    break;

                case(NetConnectionStatus.Connected):
                    break;

                case(NetConnectionStatus.Disconnecting):
                    break;

                case(NetConnectionStatus.Disconnected):
                    notifyPlayerDisconnect(statusPlayer.uid);
                    serverWorld.removePlayer(statusPlayer);
                    break;

                default:
                    break;
            }
        }

        private void playerIndexUpdateRequest(NetIncomingMessage inMsg)
        {
            long playerUID = inMsg.ReadInt64();
            int newIndex = inMsg.ReadInt32();

            if (serverWorld.getPlayer(newIndex) == null)
            {
                NetOutgoingMessage outMsg = getPlayerIndexUpdate(playerUID, newIndex);

                netServer.SendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);
            }
        }

        //===================================
        //Server processing/outgoing messages
        //===================================
        private void setupNewPlayer(NetIncomingMessage connectionMsg)
        {
            NetOutgoingMessage outMsg;

            //Create new player data
            PlayerData newPlayer = new PlayerData();
            newPlayer.deserialize(connectionMsg);

            //Update netServer player list
            serverWorld.addPlayer(newPlayer);

            //Tell all players in this game about the new player
            outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.PlayerConnect);
            newPlayer.serialize(outMsg);
            netServer.SendToAll(outMsg, connectionMsg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);

            //Assign an index to the new player
            for (int i = 0; i < dwWorldConstants.GAME_MAX_PLAYERS; i++)
            {
                if (serverWorld.getPlayer(i) == null)
                {
                    //Update the server world
                    serverWorld.assignPlayerIndex(newPlayer.uid, i);

                    //Send a message to all clients
                    outMsg = getPlayerIndexUpdate(newPlayer.uid, i);
                    netServer.SendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);
                    break;
                }
            }

            //Tell the new player about all the players in this game
            for (int i = 0; i < dwWorldConstants.GAME_MAX_PLAYERS; i++)
            {
                PlayerData player = serverWorld.getPlayer(i);
                if (player == null)
                    continue;

                if (player.Equals(newPlayer))
                    continue;

                //Send player data
                outMsg = netServer.CreateMessage();
                outMsg.Write((byte)NetDataType.PlayerConnect);
                player.serialize(outMsg);

                netServer.SendMessage(outMsg, connectionMsg.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                //Send a player index update (because this is not part of serialization)
                outMsg = getPlayerIndexUpdate(player.uid, player.index);
                netServer.SendMessage(outMsg, connectionMsg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
            }
        }

        private void notifyPlayerDisconnect(long playerUID)
        {
            NetOutgoingMessage outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.PlayerDisconnect);
            outMsg.Write(playerUID);

            netServer.SendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);
        }

        private NetOutgoingMessage getPlayerIndexUpdate(long playerUID, int newIndex)
        {
            NetOutgoingMessage outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.PlayerIndexUpdate);
            outMsg.Write(playerUID);
            outMsg.Write(newIndex);

            return outMsg;
        }

        private void startGame()
        {
            NetOutgoingMessage outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.StartGame);

            netServer.SendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);

            serverWorld.startSimulation();
        }

        private void relayFrameInput(NetIncomingMessage inMsg)
        {
            //Get frame input
            FrameInput input = new FrameInput();
            input.deserialize(inMsg);

            //Send it to the local server world
            foreach(Order o in input.orderList)
                serverWorld.issueOrder(o.owner, o);

            //Write it to a new packet
            NetOutgoingMessage outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.FrameUpdate);
            input.serialize(outMsg);

            netServer.SendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);
        }
    }
}
