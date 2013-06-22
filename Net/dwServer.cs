using System;
using System.Collections.Generic;

using Lidgren.Network;

using System.Reflection;

namespace DeterministicWorld.Net
{
    public enum NetDataType
    {
        None,
        PlayerData,
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
        private NetPeerConfiguration peerConfig;
        private NetServer netServer;

        private List<ServerPlayerData> playerList;

        private dwWorld2D serverWorld;

        public dwServer(dwWorld2D world)
        {
            Console.WriteLine("Initializing netServer...");

            //Setup the connection between the server and the world
            serverWorld = world;
            serverWorld.onWorldUpdate += gameUpdate;

            //Create the player list
            playerList = new List<ServerPlayerData>();

            //Create the network configuration
            peerConfig = new NetPeerConfiguration(WorldConstants.GAME_ID);
            peerConfig.Port = WorldConstants.GAME_NET_PORT;
            peerConfig.MaximumConnections = 200;
            peerConfig.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            //Create the server instance
            netServer = new NetServer(peerConfig);
        }

        public void start()
        {
            Console.WriteLine("Starting netServer...");
            netServer.Start();

            Console.WriteLine("Server running...");
            NetIncomingMessage inMsg;
            while (netServer.Status == NetPeerStatus.Starting || netServer.Status == NetPeerStatus.Running)
            {
                inMsg = netServer.ReadMessage();

                if (inMsg != null)
                {
                    switch (inMsg.MessageType)
                    {
                        //Client attempting to create a connection
                        case (NetIncomingMessageType.ConnectionApproval):
                            string gameId = inMsg.ReadString();
                            int gameVersion = inMsg.ReadInt32();

                            if (serverWorld.gameFrame != 0)
                            {
                                inMsg.SenderConnection.Deny("The game has already started");
                                Console.WriteLine("Denied connection from " + inMsg.SenderConnection.RemoteEndpoint + ". REASON: Game started");
                            }
                            else if (gameId != WorldConstants.GAME_ID)
                            {
                                inMsg.SenderConnection.Deny("Invalid game ID, are you connecting to the right game?");
                                int comp = gameId.CompareTo(WorldConstants.GAME_ID);
                                Console.WriteLine("Denied connection from " + inMsg.SenderConnection.RemoteEndpoint + ". REASON: Incorrect Game ID - Is "+gameId+" should be " + WorldConstants.GAME_ID + " -> " + comp);
                            }
                            else if (gameVersion != WorldConstants.GAME_VERSION)
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
                            break;

                        //App-specific data
                        case (NetIncomingMessageType.Data):
                            NetDataType dataType = (NetDataType)inMsg.ReadByte();
                            
                            switch (dataType)
                            {
                                case(NetDataType.StartGame):
                                    startGame();
                                    break;

                                case(NetDataType.FrameUpdate):
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
                            Console.WriteLine("Status changed to: " + inMsg.SenderConnection.Status);
                            break;

                        default:
                            Console.WriteLine("Contents: " + inMsg.ReadString());
                            break;
                    }
                }
            }
        }

        private void gameUpdate()
        {
            //Broadcast input
        }

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
                outMsg.Write((byte)NetDataType.PlayerData);
                playerList[i].playerData.serialize(outMsg);

                netServer.SendMessage(outMsg, newPlayer.connection, NetDeliveryMethod.ReliableOrdered);
            }

            //Tell all players in this game about the new player
            outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.PlayerData);
            newPlayer.playerData.serialize(outMsg);
            sendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);

            //Update netServer player list
            serverWorld.addPlayer(newPlayer.playerData);
            playerList.Add(newPlayer);
            
            Console.WriteLine(newPlayer.playerData.name+" has connected");
        }

        private void assignInitialSlotToPlayer(PlayerData player)
        {
            for (int i = 0; i < WorldConstants.GAME_MAX_PLAYERS; i++)
            {
                if (PlayerData.getPlayer(i) == null)
                {
                    player.assignIndex(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Initiate the start of the game
        /// </summary>
        private void startGame()
        {
            NetOutgoingMessage outMsg = netServer.CreateMessage();
            outMsg.Write((byte)NetDataType.StartGame);

            sendToAll(outMsg, NetDeliveryMethod.ReliableOrdered);

            serverWorld.startSimulation();
        }

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
